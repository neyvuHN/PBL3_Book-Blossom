// Dựa vào những sở thích đã chọn (onboarding chọn sở thích), gợi ý sách cho  Tindbook
using BookBlossom.Core.DTOs.Tindbook; 
using BookBlossom.Core.DTOs; 

namespace BookBlossom.Core.Interfaces
{
    public interface ITindbookService
    {
        // Gợi ý dành cho Customer đã đăng nhập
        Task<IEnumerable<BookResponseDTO>> GetRecommendedBooksForTindbookAsync(long userId, int limit);
        // Gợi ý dành cho Guest (dựa trên GuestPreference)
        Task<IEnumerable<BookResponseDTO>> GetRecommendedBooksForGuestAsync(Guid guestId, int limit);
        Task<IEnumerable<BookResponseDTO>> GetSwipeRecommendationsAsync(long? userId, List<long>? categoryIds, int count = 10);
        // Swipe cho Customer
        Task<bool> RecordSwipeActionAsync(long? userId, SwipeActionDTO dto);
        Task<bool> UndoLastSwipeAsync(long userId);
        Task<bool> CanUndoTindbookAsync(long userId);
        // Swipe cho Guest (trả về RequiresLogin=true nếu Guest cố AddToCart)
        Task<(bool Success, bool RequiresLogin)> RecordGuestSwipeActionAsync(Guid guestId, SwipeActionDTO dto);
        Task<bool> UndoLastGuestSwipeAsync(Guid guestId);
        Task<bool> CanUndoGuestAsync(Guid guestId);
    }
}