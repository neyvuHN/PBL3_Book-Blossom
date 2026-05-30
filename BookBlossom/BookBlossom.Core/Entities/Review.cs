using System;

namespace BookBlossom.Core.Entities
{
    public class Review
    {
        public long ReviewID { get; set; }
        public long CustomerID { get; set; }
        public long? BookID { get; set; }
        public long? BlindBookID { get; set; }
        public int Rating { get; set; } // Điểm đánh giá (1-5)
        public string Content { get; set; } = string.Empty;
        public string? ImageVideoPath { get; set; } // Lưu trữ đường dẫn ảnh/video
        public int LikeCount { get; set; } = 0;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public bool IsHidden { get; set; } = false;
        public bool IsReputationAwarded { get; set; } = false; // Đánh dấu đã cộng điểm khi đạt 5 like

        // Navigation Properties
        public virtual User? Customer { get; set; }
        public virtual RealBook? RealBook { get; set; }
        public virtual BlindBook? BlindBook { get; set; }
    }
}
