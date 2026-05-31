using System;

namespace BookBlossom.Core.DTOs.Statistics
{
    public class TrendingBookDTO {
        public long BookId { get; set; }
        public string Title { get; set; }
        public int TotalSold { get; set; }
        public decimal TotalRevenue { get; set; } // Doanh thu từ sách này
    }
}
