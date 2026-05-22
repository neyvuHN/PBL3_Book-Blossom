using System;
using BookBlossom.Core.Enums;

namespace BookBlossom.Core.DTOs.CheckoutAndCreateOrder
{
    public class OrderResponseDTO
    {
        public long OrderID { get; set; }
        public decimal TotalAmount { get; set; }
        public OrderStatus OrderStatus { get; set; }
        public DateTime OrderDate { get; set; }
        public string? PaymentUrl { get; set; } // Nếu thanh toán VNPAY thì trả về link này để FE chuyển hướng, COD thì để null
    }
}