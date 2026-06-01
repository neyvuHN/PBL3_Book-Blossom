using BookBlossom.Core.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;
using BookBlossom.DTOs.BlindBook;

namespace BookBlossom.Core.Interfaces
{
    public interface IBlindBookService
    {
        // --- Hệ thống / Client ---
        Task<IEnumerable<BlindBook>> GetAllBlindBooksAsync();
        Task<BlindBook?> GetByIdAsync(long blindBookId);

        // --- Dành cho Marketing Manager ---
        Task<BlindBook> CreateRequestAsync(BlindBook blindBook, string marketingId);
        Task<IEnumerable<BlindBook>> GetMarketingRequestsAsync(); 
        Task<bool> CreateRestockRequestAsync(long blindBookId, int quantity, string marketingId);

        // --- Dành cho Store Manager ---
        Task<IEnumerable<BlindBook>> GetPendingRequestsForStoreAsync();
        Task<bool> ConfirmBlindBookRequestAsync(long blindBookId, string storeManagerId);
        Task<bool> RejectBlindBookRequestAsync(long blindBookId, string reason, string storeManagerId);
        Task<bool> ConfirmRestockRequestAsync(long blindBookId, int approvedQuantity, string storeManagerId);

        // --- Dành cho Admin (Chủ cửa hàng) ---
        Task<bool> ToggleLockStatusAsync(long blindBookId);
        Task<bool> UpdateBlindBookAsync(long id, UpdateBlindBookDTO dto);
        Task<bool> HasOrdersAsync(long blindBookId);
        Task<HashSet<long>> GetBlindBookIdsWithOrdersAsync();
    }
}