using System;

namespace BookBlossom.Core.Entities
{
    public class CustomerReputation
    {
        // Vừa là Khóa chính, vừa là Khóa ngoại trỏ sang CustomerDetail
        public long CustomerID { get; set; }
        
        public long? RankID { get; set; }
        
        // Điểm uy tín của khách hàng (Mặc định là 100 dưới DB)
        public int? ReputationPoint { get; set; } = 100;

        public DateTime? ReputationMaxStreakStartDate { get; set; }

        // Navigation Properties
        public virtual MembershipRank? MembershipRank { get; set; }

        //  quan hệ 1-nhiều
        public virtual ICollection<ReputationHistory> Histories { get; set; } = new List<ReputationHistory>();
        public virtual CustomerDetail? CustomerDetail { get; set; } 
    }
}