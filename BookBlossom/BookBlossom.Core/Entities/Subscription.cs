using System;
using BookBlossom.Core.Enums;

namespace BookBlossom.Core.Entities
{
    public class Subscription
    {
        public long FollowID { get; set; }
        public long CustomerID { get; set; }
        public long TargetID { get; set; }
        public SubscriptionTargetType TargetType { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        // Navigation property
        public virtual User Customer { get; set; } = null!;
    }
}
