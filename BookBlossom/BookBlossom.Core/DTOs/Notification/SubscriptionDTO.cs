using System;

namespace BookBlossom.Core.DTOs.Notification
{
    public class SubscriptionDTO
    {
        public long FollowID { get; set; }
        public long CustomerID { get; set; }
        public long TargetID { get; set; }
        public string TargetType { get; set; } = string.Empty;
        public string TargetName { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }
}
