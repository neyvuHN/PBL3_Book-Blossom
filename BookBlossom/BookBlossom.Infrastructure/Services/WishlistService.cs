using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using BookBlossom.Core.DTOs.Wishlist;
using BookBlossom.Core.Entities;
using BookBlossom.Core.Interfaces.Services;
using BookBlossom.Infrastructure.Data;

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
            var query = _context.Wishlists.Include(w => w.Book).AsQueryable();

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
                Title = w.Book?.Title ?? "Unknown Book",
                Price = w.Book?.Price ?? 0,
                AddedAt = w.AddedAt
            });
        }

        public async Task<WishlistItemDTO> AddToWishlistAsync(long? userId, Guid? guestId, AddWishlistRequestDTO request)
        {
            if (!userId.HasValue && !guestId.HasValue)
                throw new InvalidOperationException("Phải cung cấp UserID hoặc GuestID.");

            if (request.BookID == null && request.BlindBookID == null)
                throw new InvalidOperationException("Phải cung cấp BookID hoặc BlindBookID.");

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

            return new WishlistItemDTO
            {
                WishlistID = wishlistItem.WishlistID,
                BookID = wishlistItem.BookID,
                BlindBookID = wishlistItem.BlindBookID,
                Title = addedBook?.Title ?? "Unknown Book",
                Price = addedBook?.Price ?? 0,
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
    }
}
