using System;
using System.Threading.Tasks;
using BookBlossom.Core.DTOs.Guest;

namespace BookBlossom.Core.Interfaces.Services
{
    public interface IGuestService
    {
        Task<GuestSessionResponseDTO> CreateGuestSessionAsync(string? ipAddress, string? deviceInfo);
        Task<bool> ValidateGuestSessionAsync(Guid guestId, string sessionToken);
        Task MigrateGuestDataToUserAsync(Guid guestId, long newUserId);
        Task<bool> IsGuestExistsAsync(Guid guestId);
    }
}
