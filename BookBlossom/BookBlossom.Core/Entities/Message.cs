using System;
using System.Collections.Generic;

namespace BookBlossom.Core.Entities
{
    public class Message
    {
        public long MessageID { get; set; }
        public long ConversationID { get; set; }
        
        public byte SenderType { get; set; } // e.g. 1 for Buyer, 2 for Shop/Admin
        public string Content { get; set; } = string.Empty;
        public byte MessageType { get; set; } // e.g. 1 for Text, 2 for Image, etc.
        
        public bool IsReadByBuyer { get; set; } = false;
        public bool IsReadByShop { get; set; } = false;
        public bool IsDeleted { get; set; } = false;
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public virtual Conversation Conversation { get; set; } = null!;
        public virtual ICollection<MessageAttachment> Attachments { get; set; } = new List<MessageAttachment>();
    }
}
