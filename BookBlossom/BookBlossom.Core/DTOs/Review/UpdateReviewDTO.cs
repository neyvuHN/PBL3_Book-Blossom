using System.ComponentModel.DataAnnotations;

namespace BookBlossom.Core.DTOs.Review
{
    public class UpdateReviewDTO
    {
        [Range(1, 5, ErrorMessage = "Điểm đánh giá phải từ 1 đến 5 sao.")]
        public int Rating { get; set; }

        [Required(ErrorMessage = "Nội dung đánh giá là bắt buộc.")]
        [StringLength(1000, ErrorMessage = "Nội dung đánh giá không quá 1000 ký tự.")]
        public string Content { get; set; } = string.Empty;
    }
}
