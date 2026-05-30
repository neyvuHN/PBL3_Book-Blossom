using System.ComponentModel.DataAnnotations;

namespace BookBlossom.Core.DTOs.Thread
{
    public class CreateThreadCommentDTO
    {
        [Required(ErrorMessage = "Nội dung bình luận không được để trống.")]
        [StringLength(1000, ErrorMessage = "Bình luận không được vượt quá 1000 ký tự.")]
        public string Content { get; set; } = string.Empty;
    }
}
