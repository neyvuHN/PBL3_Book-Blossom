using System;

namespace BookBlossom.Core.Entities
{
    public class MessageAttachment
    {
        public long AttachmentID { get; set; }
        public long MessageID { get; set; }
        
        public string FileUrl { get; set; } = string.Empty;
        public string? FileName { get; set; }
        public string? FileType { get; set; }
        public long? FileSize { get; set; }
        public byte AttachmentType { get; set; } // e.g. 1 for Image, 2 for Book Link
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public virtual Message Message { get; set; } = null!;
    }
}
