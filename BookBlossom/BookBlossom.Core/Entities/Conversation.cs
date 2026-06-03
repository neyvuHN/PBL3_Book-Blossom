using System;
using System.Collections.Generic;

namespace BookBlossom.Core.Entities
{
    public class Conversation
    {
        public long ConversationID { get; set; }
        public long BuyerID { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public bool IsClosed { get; set; } = false;

        // Navigation properties
        public virtual User User { get; set; } = null!;
        public virtual ICollection<Message> Messages { get; set; } = new List<Message>();
    }
}
