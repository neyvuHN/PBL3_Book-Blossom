using System;
using System.Collections.Generic;
using BookBlossom.Core.Enums;

namespace BookBlossom.Core.Entities
{
    public class Order
    {
        public long OrderID { get; set; }
        public long CustomerID { get; set; }
        public long? AddressID { get; set; } // Khớp với DB (NULLABLE)
        public decimal TotalAmount { get; set; }
        public DateTime? OrderDate { get; set; } = DateTime.UtcNow; // DB cho phép NULL, mặc định GETDATE()
        public DateTime? ShippedDate { get; set; }
        public OrderStatus OrderStatus { get; set; } = OrderStatus.Pending; // Kiểu enum byte
        public PaymentMethod PaymentMethod { get; set; } // Sẽ ép kiểu về tinyint trong config
        public byte PaymentStatus { get; set; } = 0; // Khớp với tinyint trong DB (0: Chưa thanh toán, 1: Đã thanh toán)
        public decimal? ShippingFee { get; set; } = 0;
        public decimal? DiscountAmount { get; set; } = 0;
        public string? ShipReceiverName { get; set; } = string.Empty;
        public string? ShipPhoneNumber { get; set; } = string.Empty;
        public string? ShipDetailAddress { get; set; } = string.Empty;
        public string? Note { get; set; }
        public DateTime? DeliveredDate { get; set; }
        public DateTime? CompletedDate { get; set; }

        // Navigation Properties
        public virtual ICollection<OrderVoucher> OrderVouchers { get; set; } = new List<OrderVoucher>();
        public virtual ICollection<OrderDetail> OrderDetails { get; set; } = new List<OrderDetail>();
        public virtual ICollection<Review> Reviews { get; set; } = new List<Review>();
    }
}