using BookBlossom.Core.DTOs.Onboarding;

namespace BookBlossom.Core.Interfaces
{
    public interface IOnboardingService
    {
        // Lấy danh sách sở thích để hiển thị khi onboarding (trạng thái Active)
        Task<IEnumerable<CategoryResponseDTO>> GetOnboardingTagsAsync();

        // Lưu sở thích + hoàn thành tour/skip
        Task<bool> SaveCustomerPreferencesAsync(long userId, SavePreferencesRequestDTO request);

        // Set flag đã hoàn thành tour
        Task<bool> CompleteOnboardingTourAsync(long userId);
    }
}