using System.Threading.Tasks;
using BookBlossom.Core.Entities;
using BookBlossom.Core.Enums;

namespace BookBlossom.Core.Interfaces.Services
{
    public interface IReputationService
    {
        Task<CustomerReputation> GetReputationWithRankAsync(long customerId);
        Task HandleReputationChangeAsync(long customerId, ReputationAction action);
        Task UpdateCustomerRankAsync(long customerId);
    }
}