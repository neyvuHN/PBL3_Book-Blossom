using System;
using BookBlossom.Core.Enums;

namespace BookBlossom.Core.DTOs.Notification
{
    public class NotificationDTO
    {
        public long NotificationID { get; set; }
        public long UserID { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public NotificationType NotificationType { get; set; }
        public int? ReferenceID { get; set; }
        public bool IsRead { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
