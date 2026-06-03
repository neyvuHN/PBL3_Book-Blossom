using System;
using System.Collections.Generic;
using BookBlossom.Core.Enums;

namespace BookBlossom.Core.DTOs
{
    public class ConversationDto
    {
        public long ConversationID { get; set; }
        public long CustomerID { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public string CustomerAvatar { get; set; } = string.Empty;
        public DateTime UpdatedAt { get; set; }
        public string LastMessageSnippet { get; set; } = string.Empty;
        public bool HasUnreadMessages { get; set; }
    }

    public class MessageDto
    {
        public long MessageID { get; set; }
        public long SenderID { get; set; }
        public string SenderName { get; set; } = string.Empty;
        public string SenderAvatar { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public List<string>? AttachmentUrls { get; set; } = new List<string>();
        public long? AttachedBookID { get; set; }
        public string? AttachedBookTitle { get; set; }
        public string? AttachedBookAuthor { get; set; }
        public string? AttachedBookImage { get; set; }
        public DateTime SentAt { get; set; }
        public bool IsRead { get; set; }
    }

    public class CreateMessageDto
    {
        public long? ConversationID { get; set; }
        public long? ReceiverID { get; set; } // If admin sends to customer, or customer initializes
        public string Content { get; set; } = string.Empty;
        public List<string>? AttachmentUrls { get; set; } = new List<string>();
        public long? AttachedBookID { get; set; }
    }

    public class CallRequestDto
    {
        public long RequestID { get; set; }
        public long CustomerID { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public CallRequestCategory Category { get; set; }
        public string CategoryName => Category.ToString();
        public string PhoneNumber { get; set; } = string.Empty;
        public string Note { get; set; } = string.Empty;
        public CallRequestStatus Status { get; set; }
        public string StatusName => Status.ToString();
        public DateTime CreatedAt { get; set; }
        public DateTime? ResolvedAt { get; set; }
    }

    public class CreateCallRequestDto
    {
        public CallRequestCategory Category { get; set; }
        public string PhoneNumber { get; set; } = string.Empty;
        public string Note { get; set; } = string.Empty;
    }
}
