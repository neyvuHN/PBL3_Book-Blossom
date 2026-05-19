using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using BookBlossom.Core.DTOs.Cart;
using BookBlossom.Core.Entities;
using BookBlossom.Core.Interfaces.Services;
using BookBlossom.Infrastructure.Data;

namespace BookBlossom.Infrastructure.Services
{
    public class CartService : ICartService
    {
        private readonly ApplicationDbContext _context;

        public CartService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<CartItemDTO>> GetCartItemsAsync(long? userId, Guid? guestId)
        {
            var query = _context.Carts.Include(c => c.Book).AsQueryable();

            if (userId.HasValue)
            {
                query = query.Where(c => c.UserID == userId.Value);
            }
            else if (guestId.HasValue)
            {
                query = query.Where(c => c.GuestID == guestId.Value);
            }
            else
            {
                return new List<CartItemDTO>();
            }

            var items = await query.ToListAsync();

            return items.Select(c => new CartItemDTO
            {
                CartID = c.CartID,
                BookID = c.BookID,
                BlindBookID = c.BlindBookID,
                Title = c.Book?.Title ?? "Unknown Book",
                Price = c.Book?.Price ?? 0,
                Quantity = c.Quantity,
                AddedAt = c.CreatedAt
            });
        }

        public async Task<CartItemDTO> AddToCartAsync(long? userId, Guid? guestId, AddCartRequestDTO request)
        {
            if (!userId.HasValue && !guestId.HasValue)
                throw new InvalidOperationException("Phải cung cấp UserID hoặc GuestID.");

            if (request.BookID == null && request.BlindBookID == null)
                throw new InvalidOperationException("Phải cung cấp BookID hoặc BlindBookID.");

            if (request.Quantity <= 0)
                throw new InvalidOperationException("Số lượng phải lớn hơn 0.");

            // Kiểm tra tồn kho cho RealBook
            if (request.BookID.HasValue)
            {
                var book = await _context.RealBooks.FindAsync(request.BookID.Value);
                if (book == null)
                    throw new InvalidOperationException("Không tìm thấy sách.");
                
                if (book.UnitsInStock < request.Quantity)
                    throw new InvalidOperationException("Sách không đủ số lượng trong kho.");
            }

            // Kiểm tra xem đã có trong giỏ chưa
            var existingCartItem = await _context.Carts
                .FirstOrDefaultAsync(c =>
                    (userId.HasValue ? c.UserID == userId.Value : c.GuestID == guestId.Value) &&
                    (c.BookID == request.BookID && c.BlindBookID == request.BlindBookID));

            if (existingCartItem != null)
            {
                existingCartItem.Quantity += request.Quantity;
                existingCartItem.UpdatedAt = DateTime.UtcNow;
                
                if (request.BookID.HasValue)
                {
                    var book = await _context.RealBooks.FindAsync(request.BookID.Value);
                    if (book != null && book.UnitsInStock < existingCartItem.Quantity)
                        throw new InvalidOperationException("Sách không đủ số lượng trong kho khi cộng dồn.");
                }
            }
            else
            {
                existingCartItem = new Cart
                {
                    UserID = userId,
                    GuestID = guestId,
                    BookID = request.BookID,
                    BlindBookID = request.BlindBookID,
                    Quantity = request.Quantity,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                await _context.Carts.AddAsync(existingCartItem);
            }

            await _context.SaveChangesAsync();

            var addedBook = request.BookID.HasValue ? await _context.RealBooks.FindAsync(request.BookID.Value) : null;

            return new CartItemDTO
            {
                CartID = existingCartItem.CartID,
                BookID = existingCartItem.BookID,
                BlindBookID = existingCartItem.BlindBookID,
                Title = addedBook?.Title ?? "Unknown Book",
                Price = addedBook?.Price ?? 0,
                Quantity = existingCartItem.Quantity,
                AddedAt = existingCartItem.CreatedAt
            };
        }

        public async Task<CartItemDTO> UpdateCartItemQuantityAsync(long cartId, long? userId, Guid? guestId, int quantity)
        {
            if (quantity <= 0)
                throw new InvalidOperationException("Số lượng phải lớn hơn 0.");

            var cartItem = await _context.Carts.Include(c => c.Book).FirstOrDefaultAsync(c => c.CartID == cartId);
            if (cartItem == null)
                throw new KeyNotFoundException("Không tìm thấy mục trong giỏ hàng.");

            if (userId.HasValue && cartItem.UserID != userId)
                throw new UnauthorizedAccessException("Bạn không có quyền sửa giỏ hàng này.");
            if (!userId.HasValue && guestId.HasValue && cartItem.GuestID != guestId)
                throw new UnauthorizedAccessException("Bạn không có quyền sửa giỏ hàng này.");

            if (cartItem.BookID.HasValue)
            {
                if (cartItem.Book != null && cartItem.Book.UnitsInStock < quantity)
                    throw new InvalidOperationException("Sách không đủ số lượng trong kho.");
            }

            cartItem.Quantity = quantity;
            cartItem.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return new CartItemDTO
            {
                CartID = cartItem.CartID,
                BookID = cartItem.BookID,
                BlindBookID = cartItem.BlindBookID,
                Title = cartItem.Book?.Title ?? "Unknown Book",
                Price = cartItem.Book?.Price ?? 0,
                Quantity = cartItem.Quantity,
                AddedAt = cartItem.CreatedAt
            };
        }

        public async Task<bool> RemoveFromCartAsync(long cartId, long? userId, Guid? guestId)
        {
            var cartItem = await _context.Carts.FirstOrDefaultAsync(c => c.CartID == cartId);
            if (cartItem == null) return false;

            if (userId.HasValue && cartItem.UserID != userId)
                throw new UnauthorizedAccessException("Bạn không có quyền xóa giỏ hàng này.");
            if (!userId.HasValue && guestId.HasValue && cartItem.GuestID != guestId)
                throw new UnauthorizedAccessException("Bạn không có quyền xóa giỏ hàng này.");

            _context.Carts.Remove(cartItem);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task MigrateGuestCartToUserAsync(Guid guestId, long userId)
        {
            var guestCarts = await _context.Carts.Where(c => c.GuestID == guestId).ToListAsync();
            if (!guestCarts.Any()) return;

            var userCarts = await _context.Carts.Where(c => c.UserID == userId).ToListAsync();

            foreach (var guestCart in guestCarts)
            {
                var existingUserCart = userCarts.FirstOrDefault(c => c.BookID == guestCart.BookID && c.BlindBookID == guestCart.BlindBookID);
                if (existingUserCart != null)
                {
                    existingUserCart.Quantity += guestCart.Quantity;
                    existingUserCart.UpdatedAt = DateTime.UtcNow;
                    _context.Carts.Remove(guestCart);
                }
                else
                {
                    guestCart.GuestID = null;
                    guestCart.UserID = userId;
                    guestCart.UpdatedAt = DateTime.UtcNow;
                }
            }
            await _context.SaveChangesAsync();
        }
    }
}
