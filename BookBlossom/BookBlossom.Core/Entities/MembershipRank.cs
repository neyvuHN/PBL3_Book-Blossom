using System;

namespace BookBlossom.Core.Entities
{
    public class MembershipRank
    {
        public long RankID { get; set; }
        
        // Kiểu tinyint trong SQL Server tương ứng với byte trong C# (Cho phép NULL)
        public byte? RankType { get; set; }
        
        // Mức chi tiêu tối thiểu để đạt rank (Mặc định là 0 dưới DB)
        public decimal? MinSpending { get; set; } = 0;
        
        // Tỷ lệ giảm giá tương ứng của Rank (Mặc định là 0 dưới DB)
        public decimal? DiscountRate { get; set; } = 0;
    }
}