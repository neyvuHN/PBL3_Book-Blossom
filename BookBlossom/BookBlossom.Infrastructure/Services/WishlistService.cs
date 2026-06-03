using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using BookBlossom.Core.DTOs.Wishlist;
using BookBlossom.Core.Entities;
using BookBlossom.Core.Interfaces.Services;
using BookBlossom.Infrastructure.Data;
using BookBlossom.Core.Enums;

namespace BookBlossom.Infrastructure.Services
{
    public class WishlistService : IWishlistService
    {
        private readonly ApplicationDbContext _context;

        public WishlistService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<WishlistItemDTO>> GetWishlistItemsAsync(long? userId, Guid? guestId)
        {
            var query = _context.Wishlists.Include(w => w.Book).Include(w => w.BlindBook).AsQueryable();

            if (userId.HasValue)
            {
                query = query.Where(w => w.UserID == userId.Value);
            }
            else if (guestId.HasValue)
            {
                query = query.Where(w => w.GuestID == guestId.Value);
            }
            else
            {
                return new List<WishlistItemDTO>();
            }

            var items = await query.ToListAsync();

            return items.Select(w => new WishlistItemDTO
            {
                WishlistID = w.WishlistID,
                BookID = w.BookID,
                BlindBookID = w.BlindBookID,
                Title = w.BookID.HasValue 
                    ? (w.Book?.Title ?? "Unknown Book") 
                    : (w.BlindBook != null ? $"Blind Book ({w.BlindBook.Category})" : "Unknown Blind Book"),
                Author = w.BookID.HasValue 
                    ? string.Join(", ", w.Book.BookAuthors.Select(ba => ba.Author.AuthorName))
                    : "Unknown",
                Price = w.BookID.HasValue 
                    ? (w.Book?.Price ?? 0) 
                    : (w.BlindBook?.Price ?? 0),
                ImageUrl = "/images/Book/book1.jpg", // Default image as RealBook doesn't map to BookImage
                AddedAt = w.AddedAt
            });
        }

        public async Task<WishlistItemDTO> AddToWishlistAsync(long? userId, Guid? guestId, AddWishlistRequestDTO request)
        {
            if (!userId.HasValue && !guestId.HasValue)
                throw new InvalidOperationException("Phải cung cấp UserID hoặc GuestID.");

            if (userId.HasValue)
            {
                await EnsureCustomerDetailExistsAsync(userId.Value);
            }

            if (request.BookID == null && request.BlindBookID == null)
                throw new InvalidOperationException("Phải cung cấp BookID hoặc BlindBookID.");

            // Kiểm tra tồn tại cho RealBook nếu được truyền
            if (request.BookID.HasValue)
            {
                var book = await _context.RealBooks.FindAsync(request.BookID.Value);
                if (book == null)
                    throw new InvalidOperationException("Không tìm thấy sách.");
            }

            // Kiểm tra tồn tại cho BlindBook nếu được truyền
            if (request.BlindBookID.HasValue)
            {
                var blindBook = await _context.BlindBooks.FindAsync(request.BlindBookID.Value);
                if (blindBook == null)
                    throw new InvalidOperationException("Không tìm thấy sách ẩn danh.");
            }

            // Kiểm tra xem đã có trong wishlist chưa
            var existingItem = await _context.Wishlists
                .FirstOrDefaultAsync(w =>
                    (userId.HasValue ? w.UserID == userId.Value : w.GuestID == guestId.Value) &&
                    (w.BookID == request.BookID && w.BlindBookID == request.BlindBookID));

            if (existingItem != null)
                throw new InvalidOperationException("Sản phẩm đã có trong danh sách yêu thích.");

            var wishlistItem = new Wishlist
            {
                UserID = userId,
                GuestID = guestId,
                BookID = request.BookID,
                BlindBookID = request.BlindBookID,
                AddedAt = DateTime.UtcNow
            };

            await _context.Wishlists.AddAsync(wishlistItem);
            await _context.SaveChangesAsync();

            var addedBook = request.BookID.HasValue ? await _context.RealBooks.FindAsync(request.BookID.Value) : null;
            var addedBlindBook = request.BlindBookID.HasValue ? await _context.BlindBooks.FindAsync(request.BlindBookID.Value) : null;

            return new WishlistItemDTO
            {
                WishlistID = wishlistItem.WishlistID,
                BookID = wishlistItem.BookID,
                BlindBookID = wishlistItem.BlindBookID,
                Title = request.BookID.HasValue 
                    ? (addedBook?.Title ?? "Unknown Book") 
                    : (addedBlindBook != null ? $"Blind Book ({addedBlindBook.Category})" : "Unknown Blind Book"),
                Price = request.BookID.HasValue 
                    ? (addedBook?.Price ?? 0) 
                    : (addedBlindBook?.Price ?? 0),
                AddedAt = wishlistItem.AddedAt
            };
        }

        public async Task<bool> RemoveFromWishlistAsync(long wishlistId, long? userId, Guid? guestId)
        {
            var wishlistItem = await _context.Wishlists.FirstOrDefaultAsync(w => w.WishlistID == wishlistId);
            if (wishlistItem == null) return false;

            if (userId.HasValue && wishlistItem.UserID != userId)
                throw new UnauthorizedAccessException("Bạn không có quyền xóa danh sách yêu thích này.");
            if (!userId.HasValue && guestId.HasValue && wishlistItem.GuestID != guestId)
                throw new UnauthorizedAccessException("Bạn không có quyền xóa danh sách yêu thích này.");

            _context.Wishlists.Remove(wishlistItem);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task MigrateGuestWishlistToUserAsync(Guid guestId, long userId)
        {
            await EnsureCustomerDetailExistsAsync(userId);

            var guestWishlists = await _context.Wishlists.Where(w => w.GuestID == guestId).ToListAsync();
            if (!guestWishlists.Any()) return;

            var userWishlists = await _context.Wishlists.Where(w => w.UserID == userId).ToListAsync();

            foreach (var guestWishlist in guestWishlists)
            {
                var existingUserWishlist = userWishlists.FirstOrDefault(w => w.BookID == guestWishlist.BookID && w.BlindBookID == guestWishlist.BlindBookID);
                if (existingUserWishlist != null)
                {
                    // Nếu đã có trong wishlist của user rồi thì bỏ qua bản ghi của guest (xoá)
                    _context.Wishlists.Remove(guestWishlist);
                }
                else
                {
                    // Nếu chưa có thì migrate sang user
                    guestWishlist.GuestID = null;
                    guestWishlist.UserID = userId;
                }
            }
            await _context.SaveChangesAsync();
        }

        private async Task EnsureCustomerDetailExistsAsync(long userId)
        {
            var exists = await _context.CustomerDetails.AnyAsync(cd => cd.CustomerID == userId);
            if (!exists)
            {
                var user = await _context.Users.FindAsync(userId);
                if (user != null)
                {
                    var customerDetail = new CustomerDetail
                    {
                        CustomerID = userId,
                        IsOnboardingCompleted = false,
                        TotalSpending = 0,
                        DailyUndoCount = 0,
                        CurrentMonthThreadCount = 0,
                        CurrentOrderStreak = 0
                    };
                    await _context.CustomerDetails.AddAsync(customerDetail);
                    await _context.SaveChangesAsync();
                }
            }
        }
    }
}
