// lấy các ID từ màn hình chọn sở thích
// IsOnboardingCompleted = true khi chọn xong sở thích + hoàn thành/skip hết tour hướng dẫn
using System.ComponentModel.DataAnnotations;

namespace BookBlossom.Core.DTOs.Onboarding
{
    public class SavePreferencesRequestDTO
    {
        [Required]
        public List<long> SelectedTagIds { get; set; } = new List<long>();

        [Required]
        public bool IsSkipped { get; set; }
    }
}