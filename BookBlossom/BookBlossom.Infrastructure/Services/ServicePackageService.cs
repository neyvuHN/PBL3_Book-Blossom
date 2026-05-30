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

        public async Task<bool> SubscribeToPackageAsync(long userId, long packageID, byte paymentMethod)
        {
            var package = await _context.ServicePackages.FindAsync(packageID);
            if (package == null) return false;

            // Logic thanh toán giả lập (Momo/VNPay Sandbox sẽ triển khai ở bước sau)
            // Giả định thanh toán thành công:

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
                var newService = new CustomerService
                {
                    CustomerID = userId,
                    CurrentPackageID = packageID,
                    StartDate = startDate,
                    EndDate = endDate
                };
                _context.CustomerServices.Add(newService);
            }

            // Ghi lại lịch sử
            var history = new ServiceHistory
            {
                CustomerID = userId,
                Price = package.Price,
                PaymentMethod = (PaymentMethod)paymentMethod,
                PaymentStatus = PaymentStatus.Completed,
                Description = $"Đăng ký gói {package.PackageName}",
                CreateAt = DateTime.UtcNow,
                IsAutoRenew = false
            };
            _context.ServiceHistories.Add(history);

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task CheckAndDowngradeExpiredSubscriptionsAsync()
        {
            var now = DateTime.UtcNow;
            var expiredServices = await _context.CustomerServices
                .Where(cs => cs.EndDate != null && cs.EndDate < now && cs.CurrentPackageID != 1)
                .ToListAsync();

            foreach (var service in expiredServices)
            {
                // Hạ cấp về gói Free (ID = 1)
                service.CurrentPackageID = 1;
                service.StartDate = now;
                service.EndDate = null;
            }

            if (expiredServices.Any())
            {
                await _context.SaveChangesAsync();
            }
        }
    }
}
