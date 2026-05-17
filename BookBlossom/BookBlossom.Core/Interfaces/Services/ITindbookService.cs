// Dựa vào những sở thích đã chọn (onboarding chọn sở thích), gợi ý sách cho  Tindbook
using BookBlossom.Core.DTOs.Tindbook; // Bạn tự tạo BookResponseDTO tương ứng với cấu trúc sách của bạn nhé

namespace BookBlossom.Core.Interfaces
{
    public interface ITindbookService
    {
        Task<IEnumerable<BookResponseDTO>> GetRecommendedBooksForTindbookAsync(long userId, int limit);
    }
}