using System.Threading.Tasks;
using BookBlossom.Core.Entities;
using BookBlossom.Core.Enums;

namespace BookBlossom.Core.Interfaces.Services
{
    public interface IReputationService
    {
        Task<CustomerReputation> GetReputationWithRankAsync(long customerId);
        // Thêm tham số string reason vào đây
        Task HandleReputationChangeAsync(long customerId, ReputationAction action, string reason);
        Task UpdateCustomerRankAsync(long customerId);

        // Các hàm kiểm tra ngưỡng để Service khác gọi
        Task<bool> CanCommentAsync(long customerId);
        Task<bool> CanUseCodAsync(long customerId);
    }
}