using System.ComponentModel.DataAnnotations;

namespace BookBlossom.Core.DTOs.Review
{
    public class CreateReviewDTO
    {
        public long OrderID { get; set; } 
        public long? BookID { get; set; }
        public long? BlindBookID { get; set; }

        [Required(ErrorMessage = "Điểm đánh giá sao không được để trống.")]
        [Range(1, 5, ErrorMessage = "Điểm đánh giá phải từ 1 đến 5 sao.")]
        public int Rating { get; set; }

        [Required(ErrorMessage = "Nội dung đánh giá không được để trống.")]
        [StringLength(1000, ErrorMessage = "Nội dung review không được vượt quá 1000 ký tự.")]
        public string Content { get; set; } = string.Empty;
    }
}