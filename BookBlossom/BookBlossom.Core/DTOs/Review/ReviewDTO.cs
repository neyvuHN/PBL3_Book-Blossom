using System;
using System.Collections.Generic;

namespace BookBlossom.Core.DTOs.Review
{
    public class ReviewDTO
    {
        public long ReviewID { get; set; }
        public long CustomerID { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public string? CustomerAvatar { get; set; }
        public long? BookID { get; set; }
        public long? BlindBookID { get; set; }
        public int Rating { get; set; }
        public string Content { get; set; } = string.Empty;
        public string? ImageVideoPath { get; set; }
        public List<string> MediaUrls { get; set; } = new List<string>();
        public List<string> VideoUrls { get; set; } = new List<string>();
        public int LikeCount { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsHidden { get; set; }
    }
}