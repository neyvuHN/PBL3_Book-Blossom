using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using BookBlossom.Core.DTOs.Cart;
using BookBlossom.Core.Entities;
using BookBlossom.Core.Interfaces.Services;
using BookBlossom.Infrastructure.Data;
using BookBlossom.Core.Enums;

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
            var query = _context.Carts.Include(c => c.Book).Include(c => c.BlindBook).AsQueryable();

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
                Title = c.BookID.HasValue 
                    ? (c.Book?.Title ?? "Unknown Book") 
                    : (c.BlindBook != null ? $"Blind Book ({c.BlindBook.Category})" : "Unknown Blind Book"),
                Price = c.BookID.HasValue 
                    ? (c.Book?.Price ?? 0) 
                    : (c.BlindBook?.Price ?? 0),
                Quantity = c.Quantity,
                AddedAt = c.CreatedAt
            });
        }

        public async Task<CartItemDTO> AddToCartAsync(long? userId, Guid? guestId, AddCartRequestDTO request)
        {
            if (!userId.HasValue && !guestId.HasValue)
                throw new InvalidOperationException("Phải cung cấp UserID hoặc GuestID.");

            if (userId.HasValue)
            {
                await EnsureCustomerDetailExistsAsync(userId.Value);
            }

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

            // Kiểm tra tồn kho cho BlindBook
            if (request.BlindBookID.HasValue)
            {
                var blindBook = await _context.BlindBooks.FindAsync(request.BlindBookID.Value);
                if (blindBook == null)
                    throw new InvalidOperationException("Không tìm thấy sách ẩn danh.");

                if (blindBook.StockQuantity <= 0)
                    throw new InvalidOperationException("Sách ẩn danh đã hết hàng.");

                if (blindBook.StockQuantity < request.Quantity)
                    throw new InvalidOperationException("Sách ẩn danh không đủ số lượng trong kho.");
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

                if (request.BlindBookID.HasValue)
                {
                    var blindBook = await _context.BlindBooks.FindAsync(request.BlindBookID.Value);
                    if (blindBook != null && blindBook.StockQuantity < existingCartItem.Quantity)
                        throw new InvalidOperationException("Sách ẩn danh không đủ số lượng trong kho khi cộng dồn.");
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
            var addedBlindBook = request.BlindBookID.HasValue ? await _context.BlindBooks.FindAsync(request.BlindBookID.Value) : null;

            return new CartItemDTO
            {
                CartID = existingCartItem.CartID,
                BookID = existingCartItem.BookID,
                BlindBookID = existingCartItem.BlindBookID,
                Title = request.BookID.HasValue 
                    ? (addedBook?.Title ?? "Unknown Book") 
                    : (addedBlindBook != null ? $"Blind Book ({addedBlindBook.Category})" : "Unknown Blind Book"),
                Price = request.BookID.HasValue 
                    ? (addedBook?.Price ?? 0) 
                    : (addedBlindBook?.Price ?? 0),
                Quantity = existingCartItem.Quantity,
                AddedAt = existingCartItem.CreatedAt
            };
        }

        public async Task<CartItemDTO> UpdateCartItemQuantityAsync(long cartId, long? userId, Guid? guestId, int quantity)
        {
            if (quantity <= 0)
                throw new InvalidOperationException("Số lượng phải lớn hơn 0.");

            var cartItem = await _context.Carts.Include(c => c.Book).Include(c => c.BlindBook).FirstOrDefaultAsync(c => c.CartID == cartId);
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
            else if (cartItem.BlindBookID.HasValue)
            {
                if (cartItem.BlindBook != null && cartItem.BlindBook.StockQuantity < quantity)
                    throw new InvalidOperationException("Sách ẩn danh không đủ số lượng trong kho.");
            }

            cartItem.Quantity = quantity;
            cartItem.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return new CartItemDTO
            {
                CartID = cartItem.CartID,
                BookID = cartItem.BookID,
                BlindBookID = cartItem.BlindBookID,
                Title = cartItem.BookID.HasValue 
                    ? (cartItem.Book?.Title ?? "Unknown Book") 
                    : (cartItem.BlindBook != null ? $"Blind Book ({cartItem.BlindBook.Category})" : "Unknown Blind Book"),
                Price = cartItem.BookID.HasValue 
                    ? (cartItem.Book?.Price ?? 0) 
                    : (cartItem.BlindBook?.Price ?? 0),
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
            await EnsureCustomerDetailExistsAsync(userId);

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
