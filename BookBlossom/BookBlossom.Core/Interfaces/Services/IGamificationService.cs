using System.Collections.Generic;
using System.Threading.Tasks;
using BookBlossom.Core.DTOs;

namespace BookBlossom.Core.Interfaces.Services
{
    public interface IGamificationService
    {
        Task<IEnumerable<BadgeCustomerDTO>> GetCustomerBadgesAsync(long customerId);
        Task<IEnumerable<BadgeDTO>> GetAllBadgesAsync();
        Task CheckAndGrantInteractionBadgesAsync(long customerId);
        Task CheckAndGrantShoppingBadgesAsync(long customerId);
        Task CheckAndGrantReputationBadgesAsync(long customerId);
        Task RecordShareActionAsync(long customerId);
    }
}
