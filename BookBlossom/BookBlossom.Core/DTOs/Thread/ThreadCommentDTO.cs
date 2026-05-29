using System;

namespace BookBlossom.Core.DTOs.Thread
{
    public class ThreadCommentDTO
    {
        public long CommentID { get; set; }
        public long PostID { get; set; }
        public long CustomerID { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public string? CustomerAvatar { get; set; }
        public string Content { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }
}
