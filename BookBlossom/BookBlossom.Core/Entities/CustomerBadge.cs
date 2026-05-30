using System;

namespace BookBlossom.Core.Entities
{
    public class CustomerBadge
    {
        public long CustomerID { get; set; }
        public long BadgeID { get; set; }
        public DateTime EarnedAt { get; set; } = DateTime.UtcNow;

        // Navigation Properties
        public virtual User? Customer { get; set; }
        public virtual Badge? Badge { get; set; }
    }
}
