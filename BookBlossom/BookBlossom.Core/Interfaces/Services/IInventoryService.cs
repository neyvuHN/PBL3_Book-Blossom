using System.Collections.Generic;
using System.Threading.Tasks;
using BookBlossom.Core.DTOs.Importing;

namespace BookBlossom.Core.Interfaces.Services
{
    public interface IInventoryService
    {
        // CRUD for Importing Header
        Task<ImportingDTO> CreateImportingAsync(long staffId, CreateImportingRequestDTO request);
        Task<ImportingDTO?> GetImportingByIdAsync(long id);
        Task<IEnumerable<ImportingDTO>> GetAllImportingsAsync();
        Task<ImportingDTO> UpdateImportingAsync(long id, UpdateImportingRequestDTO request);
        Task<bool> DeleteImportingAsync(long id);

        // CRUD for Importing Detail
        Task<ImportingDetailDTO> AddImportingDetailAsync(long importingId, CreateImportingDetailRequestDTO request);
        Task<ImportingDetailDTO> UpdateImportingDetailAsync(long importingId, long bookId, UpdateImportingDetailRequestDTO request);
        Task<bool> DeleteImportingDetailAsync(long importingId, long bookId);
    }
}
