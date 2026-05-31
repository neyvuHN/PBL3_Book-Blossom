using System;
using System.Collections.Generic;

namespace BookBlossom.Core.DTOs.Statistics
{
    public class RevenueStatsDTO
    {
        // Tổng doanh thu của khoảng thời gian được chọn
        public decimal TotalRevenue { get; set; }
        
        // Tổng số đơn hàng đã hoàn thành
        public int TotalOrders { get; set; }
        
        // Danh sách doanh thu chi tiết theo từng ngày (để FE vẽ biểu đồ đường/cột)
        public List<RevenueDataPointDTO> DailyRevenue { get; set; } = new List<RevenueDataPointDTO>();
    }
}