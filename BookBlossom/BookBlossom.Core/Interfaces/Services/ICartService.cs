using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BookBlossom.Core.DTOs.Cart;

namespace BookBlossom.Core.Interfaces.Services
{
    public interface ICartService
    {
        Task<IEnumerable<CartItemDTO>> GetCartItemsAsync(long? userId, Guid? guestId);
        Task<CartItemDTO> AddToCartAsync(long? userId, Guid? guestId, AddCartRequestDTO request);
        Task<CartItemDTO> UpdateCartItemQuantityAsync(long cartId, long? userId, Guid? guestId, int quantity);
        Task<bool> RemoveFromCartAsync(long cartId, long? userId, Guid? guestId);
        Task MigrateGuestCartToUserAsync(Guid guestId, long userId);
    }
}
