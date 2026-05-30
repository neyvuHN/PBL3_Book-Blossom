using System.ComponentModel.DataAnnotations;

namespace BookBlossom.Core.DTOs.Thread
{
    public class UpdateThreadPostDTO
    {
        [Required(ErrorMessage = "Tiêu đề bài viết không được để trống.")]
        [StringLength(255, ErrorMessage = "Tiêu đề không được vượt quá 255 ký tự.")]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "Nội dung bài viết không được để trống.")]
        public string Content { get; set; } = string.Empty;

        [StringLength(500, ErrorMessage = "Thẻ hashtag không được vượt quá 500 ký tự.")]
        public string? Hashtags { get; set; }
    }
}
