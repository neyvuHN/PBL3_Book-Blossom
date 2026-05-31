using System;
using System.Collections.Generic;

namespace BookBlossom.Core.Entities
{
    public class Badge
    {
        public long BadgeID { get; set; }
        public string BadgeName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;

        // Navigation property
        public virtual ICollection<BadgeCustomer> BadgeCustomers { get; set; } = new List<BadgeCustomer>();
    }

    public class BadgeCustomer
    {
        public long CustomerID { get; set; }
        public long BadgeID { get; set; }
        public DateTime EarnedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public virtual CustomerDetail CustomerDetail { get; set; } = null!;
        public virtual Badge Badge { get; set; } = null!;
    }
}
