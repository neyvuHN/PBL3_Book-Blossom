using BookBlossom.Core.DTOs.Tindbook;
using BookBlossom.Core.Interfaces;
using BookBlossom.Core.Interfaces.Services;
using BookBlossom.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using BookBlossom.Core.Enums;
using BookBlossom.Core.Entities;

namespace BookBlossom.Infrastructure.Services
{
    public class TindbookService : ITindbookService
    {
        private readonly ApplicationDbContext _context;
        private readonly IServicePackageService _packageService;

        public TindbookService(ApplicationDbContext context, IServicePackageService packageService)
        {
            _context = context;
            _packageService = packageService;
        }

        // ======================== CUSTOMER ========================

        public async Task<IEnumerable<BookResponseDTO>> GetRecommendedBooksForTindbookAsync(long userId, int limit = 20)
        {
            // Lấy sách mà Customer đã quẹt (để loại ra khỏi gợi ý)
            var swipedBookIds = await _context.SwipeLogs
                .Where(l => l.CustomerID == userId)
                .Select(l => l.BookID)
                .Where(id => id.HasValue)
                .Select(id => id!.Value)
                .ToListAsync();

            var userPreferenceCategoryIds = await _context.CustomerPreferences
                .Where(cp => cp.CustomerID == userId).Select(cp => cp.CategoryID).ToListAsync();

            return await _context.RealBooks.Include(b => b.Category)
                .Where(b => !swipedBookIds.Contains(b.BookID)) // Loại sách đã quẹt
                .Select(b => new {
                    Book = b,
                    Priority = userPreferenceCategoryIds.Contains(b.CategoryID) ? 1 : 2
                })
                .OrderBy(x => x.Priority).ThenBy(x => Guid.NewGuid())
                .Take(limit)
                .Select(x => new BookResponseDTO {
                    BookID = x.Book.BookID, CategoryID = x.Book.CategoryID,
                    CategoryName = x.Book.Category.CategoryName, Title = x.Book.Title,
                    Publisher = x.Book.Publisher, Price = x.Book.Price
                }).ToListAsync();
        }

        public async Task<bool> RecordSwipeActionAsync(long? userId, SwipeActionDTO dto)
        {
            if (!userId.HasValue || (!dto.BookID.HasValue && !dto.BlindBookID.HasValue)) return false;

            // Lưu log quẹt
            _context.SwipeLogs.Add(new SwipeLog {
                CustomerID = userId.Value,
                BookID = dto.BookID,
                BlindBookID = dto.BlindBookID,
                ActionType = dto.Intent.ToString(),
                CreatedAt = DateTime.UtcNow
            });

            bool success = false;
            switch (dto.Intent)
            {
                // Phải -> Wishlist
                case SwipeIntent.Wishlist:
                    if (dto.BookID.HasValue)
                    {
                        bool exists = await _context.Wishlists.AnyAsync(w => w.UserID == userId.Value && w.BookID == dto.BookID.Value);
                        if (!exists)
                            _context.Wishlists.Add(new Wishlist { UserID = userId.Value, BookID = dto.BookID.Value, AddedAt = DateTime.UtcNow });
                        success = true;
                    }
                    break;

                // Trái -> Ẩn
                case SwipeIntent.Hide:
                    success = true;
                    break;

                // Lên -> Add to Cart
                case SwipeIntent.AddToCart:
                    var book = await _context.RealBooks.FirstOrDefaultAsync(b => b.BookID == dto.BookID && b.UnitsInStock > 0);
                    if (book != null) {
                        var cartExists = await _context.Carts.AnyAsync(c => c.UserID == userId.Value && c.BookID == dto.BookID.Value);
                        if (!cartExists)
                        {
                            _context.Carts.Add(new Cart { UserID = userId.Value, BookID = dto.BookID.Value, Quantity = 1, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow });
                            book.UnitsInStock -= 1;
                        }
                        success = true;
                    }
                    break;

                // Xuống -> Bỏ qua
                case SwipeIntent.Skip:
                    success = true;
                    break;
            }

            if (success) await _context.SaveChangesAsync();
            return success;
        }

        public async Task<bool> UndoLastSwipeAsync(long userId)
        {
            // Kiểm tra giới hạn undo theo gói dịch vụ
            bool canUndo = await _packageService.CanUndoTindbookAsync(userId);
            if (!canUndo) return false; // Hết lượt undo trong tháng

            var lastSwipe = await _context.SwipeLogs.Where(l => l.CustomerID == userId)
                .OrderByDescending(l => l.CreatedAt).FirstOrDefaultAsync();
            if (lastSwipe == null) return false;

            // Logic đảo ngược nếu là AddToCart
            if (lastSwipe.ActionType == SwipeIntent.AddToCart.ToString() && lastSwipe.BookID.HasValue)
            {
                var book = await _context.RealBooks.FindAsync(lastSwipe.BookID.Value);
                if (book != null) book.UnitsInStock += 1;

                var cart = await _context.Carts.FirstOrDefaultAsync(c => c.UserID == userId && c.BookID == lastSwipe.BookID.Value);
                if (cart != null) _context.Carts.Remove(cart);
            }
            // Logic đảo ngược nếu là Wishlist
            else if (lastSwipe.ActionType == SwipeIntent.Wishlist.ToString() && lastSwipe.BookID.HasValue)
            {
                var wishlist = await _context.Wishlists.FirstOrDefaultAsync(w => w.UserID == userId && w.BookID == lastSwipe.BookID.Value);
                if (wishlist != null) _context.Wishlists.Remove(wishlist);
            }

            _context.SwipeLogs.Remove(lastSwipe);

            // Ghi nhận lượt undo vào ServiceHistory để đếm giới hạn tháng
            _context.ServiceHistories.Add(new ServiceHistory
            {
                CustomerID = userId,
                Price = 0,
                PaymentMethod = PaymentMethod.VNPay,
                PaymentStatus = PaymentStatus.Completed,
                Description = "Undo Tindbook",
                CreateAt = DateTime.UtcNow,
                IsAutoRenew = false
            });

            await _context.SaveChangesAsync();
            return true;
        }

        // Tính toán số lượt undo của người dùng theo gói subscription + membership rank
        public async Task<bool> CanUndoTindbookAsync(long userId)
        {
            // Lấy thông tin customer bao gồm Subscription và Rank
            var customer = await _context.CustomerDetails
                .Include(c => c.MembershipRank)
                .Include(c => c.ServicePackage)
                .FirstOrDefaultAsync(c => c.CustomerID == userId);

            if (customer == null) return false;

            // 1. Xác định RankType
            int rankType = customer.MembershipRank?.RankType ?? 1; // Mặc định là 1 nếu null

            // 2. Logic kiểm tra Unlimited (Pro hoặc Rank Vàng/Kim Cương)
            bool isPro = customer.ServicePackage?.UndoLimit >= 999999;
            bool isHighRank = (rankType == 3 || rankType == 4);

            if (isPro || isHighRank) return true;

            // 3. Tính toán tổng lượt Undo cho phép: (Subscription Limit) + (Rank Bonus)
            // Subscription Limit: Free=2, Basic=5
            int packageLimit = customer.ServicePackage?.UndoLimit ?? 0;
            
            // Rank Bonus: Rank 2 (Bạc) = 3, Rank 1 (Đồng) = 0
            int rankBonus = (rankType == 2) ? 3 : 0;
            
            int totalLimit = packageLimit + rankBonus;

            // 4. Đếm số lượt đã Undo trong ngày (Sử dụng SwipeLogs với ActionType="Undo")
            var today = DateTime.UtcNow.Date;
            int usedUndoCount = await _context.SwipeLogs
                .CountAsync(log => log.CustomerID == userId 
                                && log.ActionType == "Undo" 
                                && log.CreatedAt.Date == today);

            return usedUndoCount < totalLimit;
        }

        // ======================== GUEST ========================

        public async Task<IEnumerable<BookResponseDTO>> GetRecommendedBooksForGuestAsync(Guid guestId, int limit = 20)
        {
            // Lấy sách mà Guest đã quẹt (để loại ra khỏi gợi ý)
            var swipedBookIds = await _context.SwipeLogs
                .Where(l => l.GuestID == guestId)
                .Select(l => l.BookID)
                .Where(id => id.HasValue)
                .Select(id => id!.Value)
                .ToListAsync();

            // Lấy sở thích Guest đã chọn lúc onboarding
            var guestPreferenceCategoryIds = await _context.GuestPreferences
                .Where(gp => gp.GuestID == guestId).Select(gp => gp.CategoryID).ToListAsync();

            return await _context.RealBooks.Include(b => b.Category)
                .Where(b => !swipedBookIds.Contains(b.BookID)) // Loại sách đã quẹt
                .Select(b => new {
                    Book = b,
                    Priority = guestPreferenceCategoryIds.Contains(b.CategoryID) ? 1 : 2
                })
                .OrderBy(x => x.Priority).ThenBy(x => Guid.NewGuid())
                .Take(limit)
                .Select(x => new BookResponseDTO {
                    BookID = x.Book.BookID, CategoryID = x.Book.CategoryID,
                    CategoryName = x.Book.Category.CategoryName, Title = x.Book.Title,
                    Publisher = x.Book.Publisher, Price = x.Book.Price
                }).ToListAsync();
        }

        public async Task<(bool Success, bool RequiresLogin)> RecordGuestSwipeActionAsync(Guid guestId, SwipeActionDTO dto)
        {
            if (!dto.BookID.HasValue && !dto.BlindBookID.HasValue) return (false, false);

            // NGHIỆP VỤ QUAN TRỌNG: Guest KHÔNG được phép AddToCart
            if (dto.Intent == SwipeIntent.AddToCart)
            {
                return (false, true); // RequiresLogin = true -> frontend hiện popup đăng nhập
            }

            // Lưu log quẹt theo GuestID
            _context.SwipeLogs.Add(new SwipeLog {
                GuestID = guestId,
                BookID = dto.BookID,
                BlindBookID = dto.BlindBookID,
                ActionType = dto.Intent.ToString(),
                CreatedAt = DateTime.UtcNow
            });

            bool success = false;
            switch (dto.Intent)
            {
                // Phải -> Wishlist (theo GuestID)
                case SwipeIntent.Wishlist:
                    if (dto.BookID.HasValue)
                    {
                        bool exists = await _context.Wishlists.AnyAsync(w => w.GuestID == guestId && w.BookID == dto.BookID.Value);
                        if (!exists)
                            _context.Wishlists.Add(new Wishlist { GuestID = guestId, BookID = dto.BookID.Value, AddedAt = DateTime.UtcNow });
                        success = true;
                    }
                    break;

                // Trái/Xuống -> Ẩn/Bỏ qua (chỉ log, không cần làm gì thêm)
                case SwipeIntent.Hide:
                case SwipeIntent.Skip:
                    success = true;
                    break;
            }

            if (success) await _context.SaveChangesAsync();
            return (success, false);
        }

        public async Task<bool> UndoLastGuestSwipeAsync(Guid guestId)
        {
            var lastSwipe = await _context.SwipeLogs.Where(l => l.GuestID == guestId)
                .OrderByDescending(l => l.CreatedAt).FirstOrDefaultAsync();
            if (lastSwipe == null) return false;

            // Đảo ngược Wishlist nếu cần
            if (lastSwipe.ActionType == SwipeIntent.Wishlist.ToString() && lastSwipe.BookID.HasValue)
            {
                var wishlist = await _context.Wishlists.FirstOrDefaultAsync(w => w.GuestID == guestId && w.BookID == lastSwipe.BookID.Value);
                if (wishlist != null) _context.Wishlists.Remove(wishlist);
            }

            _context.SwipeLogs.Remove(lastSwipe);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<IEnumerable<BookResponseDTO>> GetSwipeRecommendationsAsync(long? userId, List<long>? categoryIds, int count = 10)
        {
            if (!userId.HasValue && (categoryIds == null || !categoryIds.Any()))
                categoryIds = await _context.Categories.OrderBy(c => Guid.NewGuid()).Take(3).Select(c => c.CategoryID).ToListAsync();

            return await _context.RealBooks.Include(b => b.Category)
                .Where(b => categoryIds != null && categoryIds.Contains(b.CategoryID))
                .OrderBy(b => Guid.NewGuid()).Take(count)
                .Select(b => new BookResponseDTO {
                    BookID = b.BookID, CategoryID = b.CategoryID,
                    CategoryName = b.Category != null ? b.Category.CategoryName : "",
                    Title = b.Title, Description = b.Description, Price = b.Price
                }).ToListAsync();
        }
    }
}