using BookBlossom.Core.DTOs.Tindbook;
using BookBlossom.Core.Interfaces;
using BookBlossom.Core.Interfaces.Services;
using BookBlossom.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using BookBlossom.Core.Enums;
using BookBlossom.Core.Entities;
using Microsoft.Extensions.Caching.Memory;

namespace BookBlossom.Infrastructure.Services
{
    public class TindbookService : ITindbookService
    {
        private readonly ApplicationDbContext _context;
        private readonly IServicePackageService _packageService;
        private readonly IMemoryCache _cache;

        public TindbookService(ApplicationDbContext context, IServicePackageService packageService, IMemoryCache cache)
        {
            _context = context;
            _packageService = packageService;
            _cache = cache;
        }

        public async Task<IEnumerable<BookResponseDTO>> GetSwipeRecommendationsAsync(long? userId, List<long>? categoryIds, int count = 10)
        {
            if (!userId.HasValue && (categoryIds == null || !categoryIds.Any()))
                categoryIds = await _context.Categories.OrderBy(c => Guid.NewGuid()).Take(3).Select(c => c.CategoryID).ToListAsync();

            return await _context.RealBooks.Include(b => b.Category)
                .Where(b => categoryIds != null && categoryIds.Contains(b.CategoryID))
                .OrderBy(b => Guid.NewGuid()).Take(count)
                .Select(b => new BookResponseDTO {
                    BookID = b.BookID, 
                    CategoryID = b.CategoryID,
                    CategoryName = b.Category != null ? b.Category.CategoryName : "",
                    Title = b.Title, 
                    Description = b.Description, 
                    Price = b.Price
                }).ToListAsync();
        }

        // ======================== CUSTOMER ========================

        public async Task<IEnumerable<BookResponseDTO>> GetRecommendedBooksForTindbookAsync(long userId)
        {
            // 1. KIỂM TRA: Chặn nếu gói Free đạt giới hạn 50 lượt quẹt/ngày
            var today = DateTime.UtcNow.Date;
            var swipeCountToday = await _context.SwipeLogs
                .CountAsync(l => l.CustomerID == userId && l.CreatedAt.Date == today);

            var customerDetails = await _context.CustomerDetails
                .Include(c => c.ServicePackage)
                .FirstOrDefaultAsync(c => c.CustomerID == userId);

            if ((customerDetails?.ServicePackage == null || customerDetails.ServicePackage.PackageName == "Free") && swipeCountToday >= 50)
            {
                throw new InvalidOperationException("FREE_LIMIT_REACHED");
            }

            // 2. Lấy danh sách ID đã quẹt để lọc trùng (tách biệt RealBook và BlindBook)
            var swipedBookIds = await _context.SwipeLogs
                .Where(l => l.CustomerID == userId && l.BookID.HasValue)
                .Select(l => l.BookID!.Value).ToListAsync();

            var swipedBlindBookIds = await _context.SwipeLogs
                .Where(l => l.CustomerID == userId && l.BlindBookID.HasValue)
                .Select(l => l.BlindBookID!.Value).ToListAsync();

            // Lấy danh sách danh mục yêu thích
            var userPreferenceCategoryIds = await _context.CustomerPreferences
                .Where(cp => cp.CustomerID == userId).Select(cp => cp.CategoryID).ToListAsync();

            // 3. Luồng 1: Lấy RealBooks thường (loại trừ các RealBook đã được cấu hình làm BlindBook)
            var realBooksQuery = _context.RealBooks.Include(b => b.Category)
                .Where(b => !swipedBookIds.Contains(b.BookID) && b.BlindBook == null)
                .Select(b => new {
                    BookID = (long?)b.BookID,
                    BlindBookID = (long?)null,
                    CategoryID = b.CategoryID,
                    CategoryName = b.Category != null ? b.Category.CategoryName : "",
                    Title = b.Title,
                    Publisher = b.Publisher,
                    Price = b.Price,
                    Description = b.Description,
                    Priority = userPreferenceCategoryIds.Contains(b.CategoryID) ? 1 : 2
                });

            // 4. Luồng 2: Lấy BlindBooks (chỉ lấy những yêu cầu đã được DUYỆT - Approved)
            var blindBooksQuery = _context.BlindBooks.Include(b => b.RealBook).ThenInclude(rb => rb!.Category)
                .Where(b => !swipedBlindBookIds.Contains(b.BlindBookID) && b.BlindBookRequestStatus == BlindBookRequestStatus.Approved)
                .Select(b => new {
                    BookID = (long?)null, // Ẩn RealBookID gốc để chống lộ thông tin
                    BlindBookID = (long?)b.BlindBookID,
                    CategoryID = b.RealBook != null ? b.RealBook.CategoryID : 0,
                    CategoryName = (b.RealBook != null && b.RealBook.Category != null) ? b.RealBook.Category.CategoryName : "",
                    Title = "Sách Bí Ẩn (Blind Book)", // Tên giả lập bí ẩn
                    Publisher = "Nhà xuất bản Bí Ẩn",
                    Price = b.Price, // Lấy giá của chiến dịch BlindBook
                    Description = (string?)("💡 Gợi ý về sách: " + b.Keywords + "\n\n📖 Trích dẫn hay: \"" + b.Quotes + "\""), // Đưa Keywords và Quotes lên thay mô tả thực
                    Priority = (b.RealBook != null && userPreferenceCategoryIds.Contains(b.RealBook.CategoryID)) ? 1 : 2
                });

            // 5. Trộn (Concat), sắp xếp theo độ ưu tiên sở thích rồi xáo trộn ngẫu nhiên
            var combinedBooks = await realBooksQuery.Concat(blindBooksQuery)
                .OrderBy(x => x.Priority)
                .ThenBy(x => Guid.NewGuid())
                .Take(10)
                .ToListAsync();

            // 6. Trả về định dạng DTO mong muốn
            return combinedBooks.Select(x => new BookResponseDTO {
                BookID = x.BookID,
                BlindBookID = x.BlindBookID,
                CategoryID = x.CategoryID,
                CategoryName = x.CategoryName,
                Title = x.Title,
                Publisher = x.Publisher,
                Price = x.Price,
                Description = x.Description
            });
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
                    // Lưu Wishlist cho RealBook thường hoặc BlindBook
                    bool exists = await _context.Wishlists.AnyAsync(w => w.UserID == userId.Value 
                        && w.BookID == dto.BookID 
                        && w.BlindBookID == dto.BlindBookID); // Cần có trường BlindBookID trong thực thể Wishlist của bạn
                    
                    if (!exists)
                    {
                        _context.Wishlists.Add(new Wishlist { 
                            UserID = userId.Value, 
                            BookID = dto.BookID, 
                            BlindBookID = dto.BlindBookID, 
                            AddedAt = DateTime.UtcNow 
                        });
                    }
                    success = true;
                    break;

                // Trái -> Ẩn
                case SwipeIntent.Hide:
                    success = true;
                    break;

                // Lên -> Add to Cart
                case SwipeIntent.AddToCart:
                    if (dto.BlindBookID.HasValue)
                    {
                        // Xử lý AddToCart cho BlindBook
                        var blindBook = await _context.BlindBooks.FirstOrDefaultAsync(b => b.BlindBookID == dto.BlindBookID && b.StockQuantity > 0);
                        if (blindBook != null)
                        {
                            var cartExists = await _context.Carts.AnyAsync(c => c.UserID == userId.Value && c.BlindBookID == dto.BlindBookID.Value);
                            if (!cartExists)
                            {
                                _context.Carts.Add(new Cart { 
                                    UserID = userId.Value, 
                                    BlindBookID = dto.BlindBookID.Value, 
                                    Quantity = 1, 
                                    CreatedAt = DateTime.UtcNow, 
                                    UpdatedAt = DateTime.UtcNow 
                                });
                                blindBook.StockQuantity -= 1;
                            }
                            success = true;
                        }
                    }
                    else if (dto.BookID.HasValue)
                    {
                        // Xử lý AddToCart cho RealBook thường
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
            bool canUndo = await _packageService.CanUndoTindbookAsync(userId);
            if (!canUndo) return false;

            var lastSwipe = await _context.SwipeLogs.Where(l => l.CustomerID == userId)
                .OrderByDescending(l => l.CreatedAt).FirstOrDefaultAsync();
            if (lastSwipe == null) return false;

            // Xử lý hoàn tác cho AddToCart
            if (lastSwipe.ActionType == SwipeIntent.AddToCart.ToString())
            {
                if (lastSwipe.BlindBookID.HasValue)
                {
                    var blindBook = await _context.BlindBooks.FindAsync(lastSwipe.BlindBookID.Value);
                    if (blindBook != null) blindBook.StockQuantity += 1;

                    var cart = await _context.Carts.FirstOrDefaultAsync(c => c.UserID == userId && c.BlindBookID == lastSwipe.BlindBookID.Value);
                    if (cart != null) _context.Carts.Remove(cart);
                }
                else if (lastSwipe.BookID.HasValue)
                {
                    var book = await _context.RealBooks.FindAsync(lastSwipe.BookID.Value);
                    if (book != null) book.UnitsInStock += 1;

                    var cart = await _context.Carts.FirstOrDefaultAsync(c => c.UserID == userId && c.BookID == lastSwipe.BookID.Value);
                    if (cart != null) _context.Carts.Remove(cart);
                }
            }
            // Xử lý hoàn tác cho Wishlist
            else if (lastSwipe.ActionType == SwipeIntent.Wishlist.ToString())
            {
                var wishlist = await _context.Wishlists.FirstOrDefaultAsync(w => w.UserID == userId 
                    && w.BookID == lastSwipe.BookID 
                    && w.BlindBookID == lastSwipe.BlindBookID);
                if (wishlist != null) _context.Wishlists.Remove(wishlist);
            }

            _context.SwipeLogs.Remove(lastSwipe);

            // Ghi nhận lịch sử dịch vụ
            _context.ServiceHistories.Add(new ServiceHistory {
                CustomerID = userId, Price = 0,
                PaymentMethod = PaymentMethod.VNPay, PaymentStatus = PaymentStatus.Completed,
                Description = "Undo Tindbook", CreateAt = DateTime.UtcNow, IsAutoRenew = false
            });

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> CanUndoTindbookAsync(long userId)
        {
            var customer = await _context.CustomerDetails
                .Include(c => c.MembershipRank)
                .Include(c => c.ServicePackage)
                .FirstOrDefaultAsync(c => c.CustomerID == userId);

            if (customer == null) return false;

            int rankType = customer.MembershipRank?.RankType ?? 1;
            bool isPro = customer.ServicePackage?.UndoLimit >= 999999;
            bool isHighRank = (rankType == 3 || rankType == 4);

            if (isPro || isHighRank) return true;

            int packageLimit = customer.ServicePackage?.UndoLimit ?? 0;
            int rankBonus = (rankType == 2) ? 3 : 0;
            int totalLimit = packageLimit + rankBonus;

            var today = DateTime.UtcNow.Date;
            int usedUndoCount = await _context.SwipeLogs
                .CountAsync(log => log.CustomerID == userId 
                                && log.ActionType == "Undo" 
                                && log.CreatedAt.Date == today);

            return usedUndoCount < totalLimit;
        }

        // ======================== GUEST ========================

        public async Task<IEnumerable<BookResponseDTO>> GetRecommendedBooksForGuestAsync(Guid guestId)
        {
            // Kiểm tra số lượng đã quẹt của Guest
            var totalSwipedCount = await _context.SwipeLogs.CountAsync(l => l.GuestID == guestId);
            if (totalSwipedCount >= 10)
            {
                return Enumerable.Empty<BookResponseDTO>();
            }

            var swipedBookIds = await _context.SwipeLogs
                .Where(l => l.GuestID == guestId && l.BookID.HasValue)
                .Select(l => l.BookID!.Value).ToListAsync();

            var swipedBlindBookIds = await _context.SwipeLogs
                .Where(l => l.GuestID == guestId && l.BlindBookID.HasValue)
                .Select(l => l.BlindBookID!.Value).ToListAsync();

            var guestPreferenceCategoryIds = await _context.GuestPreferences
                .Where(gp => gp.GuestID == guestId).Select(gp => gp.CategoryID).ToListAsync();

            // Luồng RealBooks cho Guest
            var realBooksQuery = _context.RealBooks.Include(b => b.Category)
                .Where(b => !swipedBookIds.Contains(b.BookID) && b.BlindBook == null)
                .Select(b => new {
                    BookID = (long?)b.BookID,
                    BlindBookID = (long?)null,
                    CategoryID = b.CategoryID,
                    CategoryName = b.Category != null ? b.Category.CategoryName : "",
                    Title = b.Title,
                    Publisher = b.Publisher,
                    Price = b.Price,
                    Description = b.Description,
                    Priority = guestPreferenceCategoryIds.Contains(b.CategoryID) ? 1 : 2
                });

            // Luồng BlindBooks cho Guest
            var blindBooksQuery = _context.BlindBooks.Include(b => b.RealBook).ThenInclude(rb => rb!.Category)
                .Where(b => !swipedBlindBookIds.Contains(b.BlindBookID) && b.BlindBookRequestStatus == BlindBookRequestStatus.Approved)
                .Select(b => new {
                    BookID = (long?)null,
                    BlindBookID = (long?)b.BlindBookID,
                    CategoryID = b.RealBook != null ? b.RealBook.CategoryID : 0,
                    CategoryName = (b.RealBook != null && b.RealBook.Category != null) ? b.RealBook.Category.CategoryName : "",
                    Title = "Sách Bí Ẩn (Blind Book)",
                    Publisher = "Nhà xuất bản Bí Ẩn",
                    Price = b.Price,
                    Description = (string?)("💡 Gợi ý về sách: " + b.Keywords + "\n\n📖 Trích dẫn hay: \"" + b.Quotes + "\""),
                    Priority = (b.RealBook != null && guestPreferenceCategoryIds.Contains(b.RealBook.CategoryID)) ? 1 : 2
                });

            var combinedBooks = await realBooksQuery.Concat(blindBooksQuery)
                .OrderBy(x => x.Priority).ThenBy(x => Guid.NewGuid())
                .Take(10)
                .ToListAsync();

            return combinedBooks.Select(x => new BookResponseDTO {
                BookID = x.BookID,
                BlindBookID = x.BlindBookID,
                CategoryID = x.CategoryID,
                CategoryName = x.CategoryName,
                Title = x.Title,
                Publisher = x.Publisher,
                Price = x.Price,
                Description = x.Description
            });
        }

        public async Task<(bool Success, bool RequiresLogin)> RecordGuestSwipeActionAsync(Guid guestId, SwipeActionDTO dto)
        {
            if (!dto.BookID.HasValue && !dto.BlindBookID.HasValue) return (false, false);

            if (dto.Intent == SwipeIntent.AddToCart)
            {
                return (false, true); // Guest không được AddToCart -> Yêu cầu đăng nhập
            }

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
                case SwipeIntent.Wishlist:
                    bool exists = await _context.Wishlists.AnyAsync(w => w.GuestID == guestId 
                        && w.BookID == dto.BookID 
                        && w.BlindBookID == dto.BlindBookID);
                    if (!exists)
                    {
                        _context.Wishlists.Add(new Wishlist { 
                            GuestID = guestId, 
                            BookID = dto.BookID, 
                            BlindBookID = dto.BlindBookID, 
                            AddedAt = DateTime.UtcNow 
                        });
                    }
                    success = true;
                    break;

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
            var guest = await _context.GuestDetails.FirstOrDefaultAsync(g => g.GuestID == guestId);
            if (guest == null) return false;

            var today = DateTime.UtcNow.Date;
            if (guest.LastUndoDate?.Date != today)
            {
                guest.DailyUndoCount = 0;
                guest.LastUndoDate = today;
            }

            if (guest.DailyUndoCount >= 3) return false;

            var lastSwipe = await _context.SwipeLogs.Where(l => l.GuestID == guestId)
                .OrderByDescending(l => l.CreatedAt).FirstOrDefaultAsync();
            if (lastSwipe == null) return false;

            if (lastSwipe.ActionType == SwipeIntent.Wishlist.ToString())
            {
                var wishlist = await _context.Wishlists.FirstOrDefaultAsync(w => w.GuestID == guestId 
                    && w.BookID == lastSwipe.BookID 
                    && w.BlindBookID == lastSwipe.BlindBookID);
                if (wishlist != null) _context.Wishlists.Remove(wishlist);
            }

            _context.SwipeLogs.Remove(lastSwipe);
            guest.DailyUndoCount += 1;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> CanUndoGuestAsync(Guid guestId)
        {
            var guest = await _context.GuestDetails.FirstOrDefaultAsync(g => g.GuestID == guestId);
            if (guest == null) return false;

            var today = DateTime.UtcNow.Date;
            if (guest.LastUndoDate?.Date != today) return true;

            return guest.DailyUndoCount < 3;
        }
    }
}