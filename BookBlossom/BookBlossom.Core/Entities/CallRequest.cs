using System;
using BookBlossom.Core.Enums;

namespace BookBlossom.Core.Entities
{
    public class CallRequest
    {
        public long RequestID { get; set; }
        public long CustomerID { get; set; }
        
        public CallRequestCategory Category { get; set; }
        public string PhoneNumber { get; set; } = string.Empty;
        public string Note { get; set; } = string.Empty;
        public CallRequestStatus Status { get; set; } = CallRequestStatus.Pending;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ResolvedAt { get; set; }

        // Navigation property
        public virtual User User { get; set; } = null!;
    }
}
