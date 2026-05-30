using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using BookBlossom.Core.Entities;
using BookBlossom.Core.Enums;
using BookBlossom.Core.Interfaces.Services;
using BookBlossom.Infrastructure.Data;

namespace BookBlossom.Infrastructure.Services
{
    public class ServicePackageService : IServicePackageService
    {
        private readonly ApplicationDbContext _context;

        public ServicePackageService(ApplicationDbContext context)
        {
            _context = context;
        }

        // ── Đọc ──────────────────────────────────────────────────────────────

        public async Task<IEnumerable<ServicePackage>> GetAllPackagesAsync()
        {
            return await _context.ServicePackages.ToListAsync();
        }

        public async Task<CustomerService?> GetUserCurrentServiceAsync(long userId)
        {
            return await _context.CustomerServices
                .Include(cs => cs.ServicePackage)
                .FirstOrDefaultAsync(cs => cs.CustomerID == userId);
        }

        // ── Đăng ký / Mua gói ────────────────────────────────────────────────

        public async Task<bool> SubscribeToPackageAsync(long userId, long packageID, byte paymentMethod)
        {
            var package = await _context.ServicePackages.FindAsync(packageID);
            if (package == null) return false;

            var userRequest = await _context.Users.FindAsync(userId);
            if (userRequest == null) return false;

            var existingService = await _context.CustomerServices.FindAsync(userId);

            var startDate = DateTime.UtcNow;
            var endDate = package.DurationDay > 0 ? startDate.AddDays(package.DurationDay) : (DateTime?)null;

            if (existingService != null)
            {
                existingService.CurrentPackageID = packageID;
                existingService.StartDate = startDate;
                existingService.EndDate = endDate;
            }
            else
            {
                _context.CustomerServices.Add(new CustomerService
                {
                    CustomerID = userId,
                    CurrentPackageID = packageID,
                    StartDate = startDate,
                    EndDate = endDate
                });
            }

            // Ghi lịch sử thanh toán
            _context.ServiceHistories.Add(new ServiceHistory
            {
                CustomerID = userId,
                Price = package.Price,
                PaymentMethod = (PaymentMethod)paymentMethod,
                PaymentStatus = PaymentStatus.Completed,
                Description = $"Đăng ký gói {package.PackageName}",
                CreateAt = DateTime.UtcNow,
                IsAutoRenew = false
            });

            await _context.SaveChangesAsync();
            return true;
        }

        // ── Staff CRUD ────────────────────────────────────────────────────────

        public async Task<bool> CreatePackageAsync(ServicePackage package)
        {
            _context.ServicePackages.Add(package);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> UpdatePackageAsync(ServicePackage package)
        {
            var existing = await _context.ServicePackages.FindAsync(package.PackageID);
            if (existing == null) return false;

            existing.PackageName  = package.PackageName;
            existing.Price        = package.Price;
            existing.DurationDay  = package.DurationDay;
            existing.ThreadLimit  = package.ThreadLimit;
            existing.UndoLimit    = package.UndoLimit;
            existing.Description  = package.Description;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeletePackageAsync(long packageId)
        {
            // Không cho xóa nếu đang có customer đang dùng gói này
            bool inUse = await _context.CustomerServices.AnyAsync(cs => cs.CurrentPackageID == packageId);
            if (inUse) return false;

            var package = await _context.ServicePackages.FindAsync(packageId);
            if (package == null) return false;

            _context.ServicePackages.Remove(package);
            await _context.SaveChangesAsync();
            return true;
        }

        // ── Guard: kiểm tra giới hạn tính năng ───────────────────────────────

        /// <summary>
        /// Trả về true nếu user còn đủ lượt undo Tindbook trong tháng hiện tại.
        /// Đếm số lần undo (SwipeLogs bị xóa → dùng ServiceHistory hoặc SwipeLogs riêng).
        /// Cách đơn giản: đếm số UndoTindbook action từ đầu tháng.
        /// </summary>
        public async Task<bool> CanUndoTindbookAsync(long userId)
        {
            var customerService = await _context.CustomerServices
                .Include(cs => cs.ServicePackage)
                .FirstOrDefaultAsync(cs => cs.CustomerID == userId);

            // Nếu chưa có gói → mặc định Free (UndoLimit = 2)
            int undoLimit = customerService?.ServicePackage?.UndoLimit ?? 2;

            // Pro: 999999 → không giới hạn
            if (undoLimit >= 999999) return true;

            // Đếm số lần undo trong tháng hiện tại qua ServiceHistory có Description chứa "Undo"
            var now = DateTime.UtcNow;
            var startOfMonth = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);

            int undoCount = await _context.ServiceHistories
                .CountAsync(h => h.CustomerID == userId
                              && h.CreateAt >= startOfMonth
                              && h.Description != null
                              && h.Description.Contains("Undo Tindbook"));

            return undoCount < undoLimit;
        }

        // ── Background Job ────────────────────────────────────────────────────

        public async Task CheckAndDowngradeExpiredSubscriptionsAsync()
        {
            var now = DateTime.UtcNow;
            var expiredServices = await _context.CustomerServices
                .Where(cs => cs.EndDate != null && cs.EndDate < now && cs.CurrentPackageID != 1)
                .ToListAsync();

            foreach (var service in expiredServices)
            {
                service.CurrentPackageID = 1; // Hạ cấp về Free
                service.StartDate = now;
                service.EndDate = null;
            }

            if (expiredServices.Any())
                await _context.SaveChangesAsync();
        }
    }
}
