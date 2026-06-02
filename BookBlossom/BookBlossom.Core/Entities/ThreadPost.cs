using System;
using System.Collections.Generic;

namespace BookBlossom.Core.Entities
{
    public class ThreadPost
    {
        public long PostID { get; set; }
        public long CustomerID { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public string? Hashtags { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public bool IsHidden { get; set; } = false;
        public int ReportCount { get; set; } = 0;
        public int LikeCount { get; set; } = 0;
        public int ShareCount { get; set; } = 0;

        // Navigation properties
        public virtual User User { get; set; } = null!;
        public virtual ICollection<ThreadComment> Comments { get; set; } = new List<ThreadComment>();
        public virtual ICollection<ThreadImage> Images { get; set; } = new List<ThreadImage>();
        public virtual ICollection<ThreadLike> Likes { get; set; } = new List<ThreadLike>();
        public virtual ICollection<ThreadShare> Shares { get; set; } = new List<ThreadShare>();
    }
}
