using System.Collections.Generic;
using System.Threading.Tasks;
using BookBlossom.Core.Entities;

namespace BookBlossom.Core.Interfaces.Services
{
    public interface IServicePackageService
    {
        // ── Đọc ──────────────────────────────────────────────
        Task<IEnumerable<ServicePackage>> GetAllPackagesAsync();
        Task<CustomerService?> GetUserCurrentServiceAsync(long userId);

        // ── Đăng ký / Mua gói ────────────────────────────────
        Task<bool> SubscribeToPackageAsync(long userId, long packageID, byte paymentMethod);

        // ── Staff CRUD ────────────────────────────────────────
        Task<bool> CreatePackageAsync(ServicePackage package);
        Task<bool> UpdatePackageAsync(ServicePackage package);
        Task<bool> DeletePackageAsync(long packageId);

        // ── Guard: kiểm tra giới hạn tính năng ───────────────
        /// <summary>Kiểm tra user còn đủ lượt undo Tindbook trong tháng không.</summary>
        Task<bool> CanUndoTindbookAsync(long userId);

        // ── Background Job ────────────────────────────────────
        Task CheckAndDowngradeExpiredSubscriptionsAsync();
    }
}
