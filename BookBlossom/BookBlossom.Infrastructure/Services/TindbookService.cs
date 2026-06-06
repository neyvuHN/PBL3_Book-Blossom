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

            return await _context.RealBooks.Include(b => b.Category).Include(b => b.BookImages)
                .Where(b => categoryIds != null && categoryIds.Contains(b.CategoryID))
                .OrderBy(b => Guid.NewGuid()).Take(count)
                .Select(b => new BookResponseDTO {
                    BookID = b.BookID, 
                    CategoryID = b.CategoryID,
                    CategoryName = b.Category != null ? b.Category.CategoryName : "",
                    Title = b.Title, 
                    Description = b.Description, 
                    Price = b.Price,
                    ImageUrl = b.BookImages.OrderByDescending(i => i.IsMain).Select(i => i.ImagePath).FirstOrDefault()
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

            // 3. TÍNH TOÁN DANH SÁCH THỂ LOẠI BỊ PHẠT (SKIP >= 5 LẦN TRONG 10 NGÀY)
            var tenDaysAgo = DateTime.UtcNow.AddDays(-10);
            
            var recentSkips = await _context.SwipeLogs
                .Where(l => l.CustomerID == userId 
                         && l.ActionType == SwipeIntent.Skip.ToString() 
                         && l.CreatedAt >= tenDaysAgo)
                .Select(l => new {
                    l.BookID,
                    l.BlindBookID
                })
                .ToListAsync();

            var skippedBookIds = recentSkips.Where(s => s.BookID.HasValue).Select(s => s.BookID!.Value).Distinct().ToList();
            var skippedBlindBookIds = recentSkips.Where(s => s.BlindBookID.HasValue).Select(s => s.BlindBookID!.Value).Distinct().ToList();

            var bookCategories = await _context.RealBooks
                .Where(b => skippedBookIds.Contains(b.BookID))
                .Select(b => new { b.BookID, b.CategoryID })
                .ToListAsync();

            var blindBookCategories = await _context.BlindBooks
                .Include(bb => bb.RealBook)
                .Where(bb => skippedBlindBookIds.Contains(bb.BlindBookID) && bb.RealBook != null)
                .Select(bb => new { bb.BlindBookID, bb.RealBook!.CategoryID })
                .ToListAsync();

            var skipCategoryCounts = new Dictionary<long, int>();
            foreach (var skip in recentSkips)
            {
                long catId = 0;
                if (skip.BookID.HasValue)
                {
                    var match = bookCategories.FirstOrDefault(bc => bc.BookID == skip.BookID.Value);
                    if (match != null) catId = match.CategoryID;
                }
                else if (skip.BlindBookID.HasValue)
                {
                    var match = blindBookCategories.FirstOrDefault(bbc => bbc.BlindBookID == skip.BlindBookID.Value);
                    if (match != null) catId = match.CategoryID;
                }

                if (catId > 0)
                {
                    if (skipCategoryCounts.ContainsKey(catId))
                        skipCategoryCounts[catId]++;
                    else
                        skipCategoryCounts[catId] = 1;
                }
            }

            var blockedCategoryIds = skipCategoryCounts
                .Where(kvp => kvp.Value >= 5)
                .Select(kvp => kvp.Key)
                .ToList();

            // 4. KIỂM TRA PHIÊN LẦN ĐẦU TIÊN (COLD START) VS TIẾP THEO
            var userPreferenceCategoryIds = await _context.CustomerPreferences
                .Where(cp => cp.CustomerID == userId).Select(cp => cp.CategoryID).ToListAsync();

            var totalSwipes = await _context.SwipeLogs.CountAsync(l => l.CustomerID == userId);
            bool isFirstTime = totalSwipes == 0 && userPreferenceCategoryIds.Any();

            // 5. Luồng 1: Lấy RealBooks thường
            var realBooksQuery = _context.RealBooks.Include(b => b.Category).Include(b => b.BookImages)
                .Where(b => !swipedBookIds.Contains(b.BookID) 
                         && b.BlindBook == null
                         && !blockedCategoryIds.Contains(b.CategoryID));

            if (isFirstTime)
            {
                realBooksQuery = realBooksQuery.Where(b => userPreferenceCategoryIds.Contains(b.CategoryID));
            }

            var realBooksProjected = realBooksQuery.Select(b => new {
                BookID = (long?)b.BookID,
                BlindBookID = (long?)null,
                CategoryID = b.CategoryID,
                CategoryName = b.Category != null ? b.Category.CategoryName : "",
                Title = b.Title,
                Publisher = b.Publisher,
                Price = b.Price,
                Description = b.Description,
                ImageUrl = b.BookImages.OrderByDescending(i => i.IsMain).Select(i => i.ImagePath).FirstOrDefault(),
                Priority = userPreferenceCategoryIds.Contains(b.CategoryID) ? 1 : 2
            });

            // 6. Luồng 2: Lấy BlindBooks
            var blindBooksQuery = _context.BlindBooks.Include(b => b.RealBook).ThenInclude(rb => rb!.Category)
                .Where(b => !swipedBlindBookIds.Contains(b.BlindBookID) 
                         && b.BlindBookRequestStatus == BlindBookRequestStatus.Approved
                         && (b.RealBook == null || !blockedCategoryIds.Contains(b.RealBook.CategoryID)));

            if (isFirstTime)
            {
                blindBooksQuery = blindBooksQuery.Where(b => b.RealBook != null && userPreferenceCategoryIds.Contains(b.RealBook.CategoryID));
            }

            var blindBooksProjected = blindBooksQuery.Select(b => new {
                BookID = (long?)null,
                BlindBookID = (long?)b.BlindBookID,
                CategoryID = b.RealBook != null ? b.RealBook.CategoryID : 0,
                CategoryName = (b.RealBook != null && b.RealBook.Category != null) ? b.RealBook.Category.CategoryName : "",
                Title = "Sách Bí Ẩn (Blind Book)",
                Publisher = "Nhà xuất bản Bí Ẩn",
                Price = b.Price,
                Description = (string?)("💡 Gợi ý về sách: " + b.Keywords + "\n\n📖 Trích dẫn hay: \"" + b.Quotes + "\""),
                ImageUrl = (string?)null,
                Priority = (b.RealBook != null && userPreferenceCategoryIds.Contains(b.RealBook.CategoryID)) ? 1 : 2
            });

            // 7. Trộn, sắp xếp theo ưu tiên sở thích rồi xáo trộn ngẫu nhiên
            var combinedBooks = await realBooksProjected.Concat(blindBooksProjected)
                .OrderBy(x => x.Priority)
                .ThenBy(x => Guid.NewGuid())
                .Take(10)
                .ToListAsync();

            // 8. Trả về DTO
            return combinedBooks.Select(x => new BookResponseDTO {
                BookID = x.BookID,
                BlindBookID = x.BlindBookID,
                CategoryID = x.CategoryID,
                CategoryName = x.CategoryName,
                Title = x.Title,
                Publisher = x.Publisher,
                Price = x.Price,
                Description = x.Description,
                ImageUrl = x.ImageUrl
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
            bool canUndo = await CanUndoTindbookAsync(userId);
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

            // Update CustomerDetail DailyUndoCount
            var customer = await _context.CustomerDetails.FirstOrDefaultAsync(c => c.CustomerID == userId);
            if (customer != null)
            {
                var todayDate = DateTime.UtcNow.Date;
                if (customer.LastUndoDate?.Date != todayDate)
                {
                    customer.DailyUndoCount = 0;
                    customer.LastUndoDate = todayDate;
                }
                customer.DailyUndoCount += 1;
            }

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
                .FirstOrDefaultAsync(c => c.CustomerID == userId);

            if (customer == null) return false;

            // Lấy gói dịch vụ đang hoạt động thực sự từ CustomerServices
            var customerService = await _context.CustomerServices
                .Include(cs => cs.ServicePackage)
                .FirstOrDefaultAsync(cs => cs.CustomerID == userId);

            var activePackage = customerService?.ServicePackage;

            int rankType = customer.MembershipRank?.RankType ?? 1;
            bool isPro = activePackage?.UndoLimit >= 999999;
            bool isHighRank = (rankType == 3 || rankType == 4);

            if (isPro || isHighRank) return true;

            int packageLimit = activePackage?.UndoLimit ?? 0;
            int rankBonus = (rankType == 2) ? 3 : 0;

            // Mặc định cho Free User (chưa có gói dịch vụ) là 2 lượt
            if (customerService == null)
            {
                packageLimit = 2;
            }

            int totalLimit = packageLimit + rankBonus;

            // Đếm số lần đã Undo thực tế trong ngày hôm nay từ ServiceHistories (nơi lưu các dòng "Undo Tindbook")
            var today = DateTime.UtcNow.Date;
            int usedUndoCount = await _context.ServiceHistories
                .CountAsync(h => h.CustomerID == userId 
                              && h.Description != null 
                              && h.Description.Contains("Undo Tindbook") 
                              && h.CreateAt >= today);

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

            // 3. TÍNH TOÁN DANH SÁCH THỂ LOẠI BỊ PHẠT CHO GUEST (SKIP >= 5 LẦN TRONG 10 NGÀY)
            var tenDaysAgo = DateTime.UtcNow.AddDays(-10);
            
            var recentSkips = await _context.SwipeLogs
                .Where(l => l.GuestID == guestId 
                         && l.ActionType == SwipeIntent.Skip.ToString() 
                         && l.CreatedAt >= tenDaysAgo)
                .Select(l => new {
                    l.BookID,
                    l.BlindBookID
                })
                .ToListAsync();

            var skippedBookIds = recentSkips.Where(s => s.BookID.HasValue).Select(s => s.BookID!.Value).Distinct().ToList();
            var skippedBlindBookIds = recentSkips.Where(s => s.BlindBookID.HasValue).Select(s => s.BlindBookID!.Value).Distinct().ToList();

            var bookCategories = await _context.RealBooks
                .Where(b => skippedBookIds.Contains(b.BookID))
                .Select(b => new { b.BookID, b.CategoryID })
                .ToListAsync();

            var blindBookCategories = await _context.BlindBooks
                .Include(bb => bb.RealBook)
                .Where(bb => skippedBlindBookIds.Contains(bb.BlindBookID) && bb.RealBook != null)
                .Select(bb => new { bb.BlindBookID, bb.RealBook!.CategoryID })
                .ToListAsync();

            var skipCategoryCounts = new Dictionary<long, int>();
            foreach (var skip in recentSkips)
            {
                long catId = 0;
                if (skip.BookID.HasValue)
                {
                    var match = bookCategories.FirstOrDefault(bc => bc.BookID == skip.BookID.Value);
                    if (match != null) catId = match.CategoryID;
                }
                else if (skip.BlindBookID.HasValue)
                {
                    var match = blindBookCategories.FirstOrDefault(bbc => bbc.BlindBookID == skip.BlindBookID.Value);
                    if (match != null) catId = match.CategoryID;
                }

                if (catId > 0)
                {
                    if (skipCategoryCounts.ContainsKey(catId))
                        skipCategoryCounts[catId]++;
                    else
                        skipCategoryCounts[catId] = 1;
                }
            }

            var blockedCategoryIds = skipCategoryCounts
                .Where(kvp => kvp.Value >= 5)
                .Select(kvp => kvp.Key)
                .ToList();

            // 4. KIỂM TRA PHIÊN LẦN ĐẦU TIÊN (COLD START) VS TIẾP THEO
            var guestPreferenceCategoryIds = await _context.GuestPreferences
                .Where(gp => gp.GuestID == guestId).Select(gp => gp.CategoryID).ToListAsync();

            var totalGuestSwipes = await _context.SwipeLogs.CountAsync(l => l.GuestID == guestId);
            bool isFirstTime = totalGuestSwipes == 0 && guestPreferenceCategoryIds.Any();

            // 5. Luồng RealBooks cho Guest
            var realBooksQuery = _context.RealBooks.Include(b => b.Category).Include(b => b.BookImages)
                .Where(b => !swipedBookIds.Contains(b.BookID) 
                         && b.BlindBook == null
                         && !blockedCategoryIds.Contains(b.CategoryID));

            if (isFirstTime)
            {
                realBooksQuery = realBooksQuery.Where(b => guestPreferenceCategoryIds.Contains(b.CategoryID));
            }

            var realBooksProjected = realBooksQuery.Select(b => new {
                BookID = (long?)b.BookID,
                BlindBookID = (long?)null,
                CategoryID = b.CategoryID,
                CategoryName = b.Category != null ? b.Category.CategoryName : "",
                Title = b.Title,
                Publisher = b.Publisher,
                Price = b.Price,
                Description = b.Description,
                ImageUrl = b.BookImages.OrderByDescending(i => i.IsMain).Select(i => i.ImagePath).FirstOrDefault(),
                Priority = guestPreferenceCategoryIds.Contains(b.CategoryID) ? 1 : 2
            });

            // 6. Luồng BlindBooks cho Guest
            var blindBooksQuery = _context.BlindBooks.Include(b => b.RealBook).ThenInclude(rb => rb!.Category)
                .Where(b => !swipedBlindBookIds.Contains(b.BlindBookID) 
                         && b.BlindBookRequestStatus == BlindBookRequestStatus.Approved
                         && (b.RealBook == null || !blockedCategoryIds.Contains(b.RealBook.CategoryID)));

            if (isFirstTime)
            {
                blindBooksQuery = blindBooksQuery.Where(b => b.RealBook != null && guestPreferenceCategoryIds.Contains(b.RealBook.CategoryID));
            }

            var blindBooksProjected = blindBooksQuery.Select(b => new {
                BookID = (long?)null,
                BlindBookID = (long?)b.BlindBookID,
                CategoryID = b.RealBook != null ? b.RealBook.CategoryID : 0,
                CategoryName = (b.RealBook != null && b.RealBook.Category != null) ? b.RealBook.Category.CategoryName : "",
                Title = "Sách Bí Ẩn (Blind Book)",
                Publisher = "Nhà xuất bản Bí Ẩn",
                Price = b.Price,
                Description = (string?)("💡 Gợi ý về sách: " + b.Keywords + "\n\n📖 Trích dẫn hay: \"" + b.Quotes + "\""),
                ImageUrl = (string?)null,
                Priority = (b.RealBook != null && guestPreferenceCategoryIds.Contains(b.RealBook.CategoryID)) ? 1 : 2
            });

            // 7. Trộn, sắp xếp theo ưu tiên sở thích rồi xáo trộn ngẫu nhiên
            var combinedBooks = await realBooksProjected.Concat(blindBooksProjected)
                .OrderBy(x => x.Priority)
                .ThenBy(x => Guid.NewGuid())
                .Take(10)
                .ToListAsync();

            // 8. Trả về DTO
            return combinedBooks.Select(x => new BookResponseDTO {
                BookID = x.BookID,
                BlindBookID = x.BlindBookID,
                CategoryID = x.CategoryID,
                CategoryName = x.CategoryName,
                Title = x.Title,
                Publisher = x.Publisher,
                Price = x.Price,
                Description = x.Description,
                ImageUrl = x.ImageUrl
            });
        }

        public async Task<(bool Success, bool RequiresLogin)> RecordGuestSwipeActionAsync(Guid guestId, SwipeActionDTO dto)
        {
            if (!dto.BookID.HasValue && !dto.BlindBookID.HasValue) return (false, false);

            if (dto.Intent == SwipeIntent.AddToCart)
            {
                bool cartSaved = false;
                if (dto.BlindBookID.HasValue)
                {
                    var blindBook = await _context.BlindBooks.FirstOrDefaultAsync(b => b.BlindBookID == dto.BlindBookID && b.StockQuantity > 0);
                    if (blindBook != null)
                    {
                        var cartExists = await _context.Carts.AnyAsync(c => c.GuestID == guestId && c.BlindBookID == dto.BlindBookID.Value);
                        if (!cartExists)
                        {
                            _context.Carts.Add(new Cart { GuestID = guestId, BlindBookID = dto.BlindBookID.Value, Quantity = 1, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow });
                            blindBook.StockQuantity -= 1;
                        }
                        cartSaved = true;
                    }
                }
                else if (dto.BookID.HasValue)
                {
                    var book = await _context.RealBooks.FirstOrDefaultAsync(b => b.BookID == dto.BookID && b.UnitsInStock > 0);
                    if (book != null)
                    {
                        var cartExists = await _context.Carts.AnyAsync(c => c.GuestID == guestId && c.BookID == dto.BookID.Value);
                        if (!cartExists)
                        {
                            _context.Carts.Add(new Cart { GuestID = guestId, BookID = dto.BookID.Value, Quantity = 1, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow });
                            book.UnitsInStock -= 1;
                        }
                        cartSaved = true;
                    }
                }

                _context.SwipeLogs.Add(new SwipeLog {
                    GuestID = guestId,
                    BookID = dto.BookID,
                    BlindBookID = dto.BlindBookID,
                    ActionType = dto.Intent.ToString(),
                    CreatedAt = DateTime.UtcNow
                });

                if (cartSaved)
                {
                    await _context.SaveChangesAsync();
                }

                return (cartSaved, true); // Lưu giỏ hàng tạm thời thành công và yêu cầu đăng ký/đăng nhập
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