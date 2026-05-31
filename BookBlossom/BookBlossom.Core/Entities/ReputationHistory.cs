using System;

namespace BookBlossom.Core.Entities
{
    public class ReputationHistory
    {
        public long HistoryID { get; set; }
        public long CustomerID { get; set; }
        public int ChangeAmount { get; set; }
        public string? Reason { get; set; }
        public byte ReferenceType { get; set; }
        public DateTime? CreateAt { get; set; }

        // Navigation Property (Thuộc tính điều hướng liên kết quan hệ giữa các bảng)
        public virtual CustomerReputation? CustomerReputation { get; set; }
    }
}