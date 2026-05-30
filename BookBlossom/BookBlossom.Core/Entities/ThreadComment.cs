using System;

namespace BookBlossom.Core.Entities
{
    public class ThreadComment
    {
        public long CommentID { get; set; }
        public long PostID { get; set; }
        public long CustomerID { get; set; }
        public string Content { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public virtual ThreadPost Post { get; set; } = null!;
        public virtual User User { get; set; } = null!;
    }
}
