using BookBlossom.Core.DTOs.Book;
using BookBlossom.Core.Enums;

namespace BookBlossom.Core.Interfaces
{
    public interface IRealBookService
    {
        Task<bool> CreateRealBookAsync(CreateRealBookDTO request);
        Task<List<RealBookDTO>> GetAllRealBooksAsync(string searchTerm = "", string category = "", SortOrder sortOrder = SortOrder.Ascending);
        Task<List<RealBookDTO>> GetRealBooksByCategoryIdAsync(long categoryId, SortOrder sortOrder = SortOrder.Ascending);
        Task<RealBookDTO?> GetRealBookByIdAsync(long id);
        Task<bool> UpdateRealBookAsync(long id, UpdateRealBookDTO request);
        Task<bool> DeleteRealBookAsync(long id);

        // Hàm phục vụ giữ kho khi Buyer tạo đơn hàng ở Phase 3
        Task<bool> ReserveStockAsync(long bookId, int quantity);
    }
}