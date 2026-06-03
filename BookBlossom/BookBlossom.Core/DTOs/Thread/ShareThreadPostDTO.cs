using System.ComponentModel.DataAnnotations;

namespace BookBlossom.Core.DTOs.Thread
{
    public class ShareThreadPostDTO
    {
        [StringLength(1000, ErrorMessage = "Đường dẫn chia sẻ không được vượt quá 1000 ký tự.")]
        public string? ShareUrl { get; set; }
    }
}
