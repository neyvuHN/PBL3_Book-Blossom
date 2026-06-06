namespace BookBlossom.Core.Entities
{
    public class OrderDetail
    {
        public long OrderID { get; set; }
        public long BookID { get; set; } // Ánh xạ tới [BookID] của RealBook
        public decimal UnitPrice { get; set; }
        public int Quantity { get; set; }
        public decimal? Discount { get; set; } = 0; // DB cho phép NULL
        public long? BlindBookID { get; set; } // Khớp với DB (NULLABLE)
        public string? VoucherBreakdown { get; set; } // [NEW] Lưu trữ json chi tiết voucher áp dụng

        // Navigation Properties
        public virtual Order? Order { get; set; }
        public virtual RealBook? RealBook { get; set; }
        public virtual BlindBook? BlindBook { get; set; }
    }
}