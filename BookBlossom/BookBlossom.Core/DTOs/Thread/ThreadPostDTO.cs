using System;
using System.Collections.Generic;

namespace BookBlossom.Core.DTOs.Thread
{
    public class ThreadPostDTO
    {
        public long PostID { get; set; }
        public long CustomerID { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public string? CustomerAvatar { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public string? Hashtags { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsHidden { get; set; }
        public int ReportCount { get; set; }
        public int LikeCount { get; set; }
        public int ShareCount { get; set; }
        public int CommentsCount { get; set; }
        public bool IsLikedByCurrentUser { get; set; }

        public List<ThreadImageDTO> Images { get; set; } = new List<ThreadImageDTO>();
        public List<ThreadCommentDTO> Comments { get; set; } = new List<ThreadCommentDTO>();

        public long? BookID { get; set; }
        public string? BookTitle { get; set; }
        public string? BookAuthor { get; set; }
        public string? BookImage { get; set; }
    }
}
