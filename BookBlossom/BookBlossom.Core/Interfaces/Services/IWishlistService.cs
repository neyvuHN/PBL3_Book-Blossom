using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BookBlossom.Core.DTOs.Wishlist;

namespace BookBlossom.Core.Interfaces.Services
{
    public interface IWishlistService
    {
        Task<IEnumerable<WishlistItemDTO>> GetWishlistItemsAsync(long? userId, Guid? guestId);
        Task<WishlistItemDTO> AddToWishlistAsync(long? userId, Guid? guestId, AddWishlistRequestDTO request);
        Task<bool> RemoveFromWishlistAsync(long wishlistId, long? userId, Guid? guestId);
        Task MigrateGuestWishlistToUserAsync(Guid guestId, long userId);
    }
}
