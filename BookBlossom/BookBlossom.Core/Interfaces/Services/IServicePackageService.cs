using System.Collections.Generic;
using System.Threading.Tasks;
using BookBlossom.Core.Entities;

namespace BookBlossom.Core.Interfaces.Services
{
    public interface IServicePackageService
    {
        Task<IEnumerable<ServicePackage>> GetAllPackagesAsync();
        Task<CustomerService?> GetUserCurrentServiceAsync(long userId);
        Task<bool> SubscribeToPackageAsync(long userId, long packageID, byte paymentMethod);
        Task CheckAndDowngradeExpiredSubscriptionsAsync();
    }
}
