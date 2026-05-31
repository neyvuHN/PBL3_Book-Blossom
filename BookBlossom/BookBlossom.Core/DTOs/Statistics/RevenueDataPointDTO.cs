using System;

namespace BookBlossom.Core.DTOs.Statistics
{
    public class RevenueDataPointDTO
    {
        // Trục X: Nhãn hiển thị trên biểu đồ (Ví dụ: "31/05/2026", "05/2026", hoặc "2026")
        // Dùng kiểu string giúp Frontend bê thẳng vào thư viện Chart mà không cần parse/format lại
        public string Label { get; set; } = string.Empty;

        // Trục Y: Tổng doanh thu tương ứng với mốc thời gian đó
        public decimal TotalAmount { get; set; }
    }
}