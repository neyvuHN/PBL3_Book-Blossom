using System;
using BookBlossom.Core.Enums;

namespace BookBlossom.Core.Entities
{
    public class ServiceHistory
    {
        public long HistoryID { get; set; }
        public long CustomerID { get; set; }
        public decimal Price { get; set; }
        public PaymentMethod PaymentMethod { get; set; }
        public PaymentStatus PaymentStatus { get; set; }
        public string? Description { get; set; }
        public bool? IsAutoRenew { get; set; }
        public DateTime? CreateAt { get; set; }

        public virtual User User { get; set; } = null!;
    }
}
