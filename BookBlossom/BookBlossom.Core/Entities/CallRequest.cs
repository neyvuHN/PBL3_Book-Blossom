using System;
using BookBlossom.Core.Enums;

namespace BookBlossom.Core.Entities
{
    public class CallRequest
    {
        public long CallRequestID { get; set; }
        public long BuyerID { get; set; }
        public long? ConversationID { get; set; }
        public string PhoneNumber { get; set; } = string.Empty;
        public CallRequestCategory Category { get; set; }
        public string? Note { get; set; }
        public CallRequestStatus Status { get; set; } = CallRequestStatus.Pending;
        public long? ResolvedBy { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ResolvedAt { get; set; }

        // Navigation properties
        public virtual CustomerDetail Buyer { get; set; } = null!;
        public virtual Conversation? Conversation { get; set; }
        public virtual User? Resolver { get; set; }
    }
}
