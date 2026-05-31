using System;
using BookBlossom.Core.Enums;

namespace BookBlossom.Core.Entities
{
    public class UserNotification
    {
        public long NotificationID { get; set; }
        public long UserID { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public NotificationType NotificationType { get; set; }
        public int? ReferenceID { get; set; }
        public SubscriptionTargetType? ReferenceType { get; set; }
        public bool IsRead { get; set; } = false;
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        // Navigation property
        public virtual User User { get; set; } = null!;
    }
}
