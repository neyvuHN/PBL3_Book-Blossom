using System;

namespace BookBlossom.Core.Entities
{
    public class Message
    {
        public long MessageID { get; set; }
        public long ConversationID { get; set; }
        public long SenderID { get; set; }
        
        public string Content { get; set; } = string.Empty;
        public string? AttachmentUrl { get; set; }
        public long? AttachedBookID { get; set; }

        public DateTime SentAt { get; set; } = DateTime.UtcNow;
        public bool IsRead { get; set; } = false;

        // Navigation properties
        public virtual Conversation Conversation { get; set; } = null!;
        public virtual User Sender { get; set; } = null!;
        public virtual RealBook? AttachedBook { get; set; }
    }
}
