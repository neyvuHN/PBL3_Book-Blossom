using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using BookBlossom.Core.Entities;
using BookBlossom.Core.Interfaces.Services;
using BookBlossom.Core.DTOs.Guest;
using BookBlossom.Infrastructure.Data;
using System.Security.Cryptography;

namespace BookBlossom.Infrastructure.Services
{
    public class GuestService : IGuestService
    {
        private readonly ApplicationDbContext _context;
        private readonly ICartService _cartService;
        private readonly IWishlistService _wishlistService;

        public GuestService(ApplicationDbContext context, ICartService cartService, IWishlistService wishlistService)
        {
            _context = context;
            _cartService = cartService;
            _wishlistService = wishlistService;
        }

        public async Task<GuestSessionResponseDTO> CreateGuestSessionAsync(string? ipAddress, string? deviceInfo)
        {
            var guestId = Guid.NewGuid();
            var sessionToken = GenerateSessionToken();

            var guest = new GuestDetail
            {
                GuestID = guestId,
                SessionToken = sessionToken,
                CreatedAt = DateTime.UtcNow,
                LastActiveAt = DateTime.UtcNow,
                IPAddress = ipAddress,
                DeviceInfo = deviceInfo
            };

            _context.GuestDetails.Add(guest);
            await _context.SaveChangesAsync();

            return new GuestSessionResponseDTO
            {
                GuestID = guestId,
                SessionToken = sessionToken,
                ExpireAt = DateTime.UtcNow.AddDays(7)
            };
        }

        public async Task<bool> ValidateGuestSessionAsync(Guid guestId, string sessionToken)
        {
            var guest = await _context.GuestDetails
                .FirstOrDefaultAsync(g => g.GuestID == guestId && g.SessionToken == sessionToken);

            if (guest == null || guest.ConvertedUserID != null || guest.CreatedAt < DateTime.UtcNow.AddDays(-7))
            {
                // Nếu không tồn tại hoặc đã convert sang User thì không hợp lệ
                return false;
            }

            // Cập nhật thời gian hoạt động cuối
            guest.LastActiveAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task MigrateGuestDataToUserAsync(Guid guestId, long newUserId)
        {
            var guest = await _context.GuestDetails.FindAsync(guestId);
            if (guest != null && guest.ConvertedUserID == null)
            {
                guest.ConvertedUserID = newUserId;
                await _context.SaveChangesAsync();
                
                // 1. Đồng bộ Giỏ hàng và Yêu thích (đã có sẵn)
                await _cartService.MigrateGuestCartToUserAsync(guestId, newUserId);
                await _wishlistService.MigrateGuestWishlistToUserAsync(guestId, newUserId);

                // 2. Đồng bộ Sở thích: GuestPreference -> CustomerPreference
                var guestPrefs = await _context.GuestPreferences
                    .Where(gp => gp.GuestID == guestId).ToListAsync();

                if (guestPrefs.Any())
                {
                    // Lấy danh sách CategoryID đã có trong CustomerPreference để tránh trùng
                    var existingCategoryIds = await _context.CustomerPreferences
                        .Where(cp => cp.CustomerID == newUserId)
                        .Select(cp => cp.CategoryID).ToListAsync();

                    var newCustomerPrefs = guestPrefs
                        .Where(gp => !existingCategoryIds.Contains(gp.CategoryID))
                        .Select(gp => new CustomerPreference
                        {
                            CustomerID = newUserId,
                            CategoryID = gp.CategoryID,
                            CreatedAt = gp.CreatedAt
                        }).ToList();

                    if (newCustomerPrefs.Any())
                        await _context.CustomerPreferences.AddRangeAsync(newCustomerPrefs);

                    // Xóa GuestPreference sau khi đã chuyển
                    _context.GuestPreferences.RemoveRange(guestPrefs);
                }

                // 3. Đồng bộ Lịch sử quẹt: SwipeLog(GuestID) -> SwipeLog(CustomerID)
                var guestSwipeLogs = await _context.SwipeLogs
                    .Where(l => l.GuestID == guestId).ToListAsync();

                foreach (var log in guestSwipeLogs)
                {
                    log.CustomerID = newUserId;
                    log.GuestID = null;
                }

                await _context.SaveChangesAsync();
            }
        }

        private string GenerateSessionToken()
        {
            var randomNumber = new byte[32];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomNumber);
            return Convert.ToBase64String(randomNumber);
        }
    }
}
