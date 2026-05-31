using System.ComponentModel.DataAnnotations;

namespace BookBlossom.Core.DTOs.Notification
{
    public class SubscribeRequestDTO
    {
        [Required]
        public long TargetID { get; set; }

        [Required]
        [RegularExpression("^(Order|Book|Thread|Badge|Report|ReturnRequest|None)$", ErrorMessage = "TargetType must be one of: Order, Book, Thread, Badge, Report, ReturnRequest, None")]
        public string TargetType { get; set; } = string.Empty;
    }
}
