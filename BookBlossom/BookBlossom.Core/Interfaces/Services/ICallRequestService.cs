using System.Collections.Generic;
using System.Threading.Tasks;
using BookBlossom.Core.DTOs;

namespace BookBlossom.Core.Interfaces.Services
{
    public interface ICallRequestService
    {
        Task<List<CallRequestDto>> GetAllCallRequestsAsync(); // For Admin
        Task<List<CallRequestDto>> GetCustomerCallRequestsAsync(long customerId);
        Task<CallRequestDto> CreateCallRequestAsync(long customerId, CreateCallRequestDto dto);
        Task ResolveCallRequestAsync(long requestId);
    }
}
