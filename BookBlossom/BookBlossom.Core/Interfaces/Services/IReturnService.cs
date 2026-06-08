using System.Collections.Generic;
using System.Threading.Tasks;
using BookBlossom.Core.DTOs;
using Microsoft.AspNetCore.Http;

namespace BookBlossom.Core.Interfaces
{
    public interface IReturnService
    {
        Task<ReturnRequestDetailDTO> CreateReturnRequestAsync(long customerId, long orderId, CreateReturnRequestDTO dto, IFormFile videoFile);
        Task<IEnumerable<ReturnRequestDetailDTO>> GetReturnRequestsAsync(byte? status);
        Task<ReturnRequestDetailDTO?> GetReturnRequestByIdAsync(long requestId);
        Task<bool> ReviewReturnRequestAsync(long staffId, long requestId, ReviewReturnRequestDTO dto);
        Task<bool> RestockReturnAsync(long staffId, long requestId);
    }
}
