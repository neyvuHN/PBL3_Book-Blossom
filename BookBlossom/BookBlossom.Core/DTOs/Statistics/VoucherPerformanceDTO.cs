using System;

namespace BookBlossom.Core.DTOs.Statistics
{
    public class VoucherPerformanceDTO {
        public string VoucherCode { get; set; }
        public int UsageCount { get; set; } // Số lần được sử dụng
        public decimal TotalDiscountGiven { get; set; } // Tổng tiền đã giảm
        public decimal TotalOrderValue { get; set; } // Tổng giá trị đơn hàng dùng voucher này
    }
}