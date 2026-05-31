using System;
using System.Collections.Generic;

namespace BookBlossom.Core.DTOs
{
    public class KpiSummaryDto
    {
        public decimal TotalRevenue { get; set; }  
        public int TotalOrders { get; set; }       
        public double ReturnRate { get; set; }         
    }

    // Cập nhật: Khớp hoàn toàn với cấu trúc 7 trạng thái của OrderStatus Enum
    public class OrderPieChartDto
    {
        public int PendingCount { get; set; }
        public int AwaitingPickupCount { get; set; }
        public int ShippingCount { get; set; }
        public int DeliveringCount { get; set; }
        public int CompletedCount { get; set; }
        public int CancelledCount { get; set; }
        public int ReturningCount { get; set; }
    }

    public class RevenueChartPointDto
    {
        public string DateLabel { get; set; }      
        public decimal RealBookRevenue { get; set; } 
        public decimal BlindBookRevenue { get; set; } 
    }

    public class TopBookDto
    {
        public int Rank { get; set; }             
        public string BookName { get; set; }      
        public string ProductType { get; set; }   
        public int SoldCount { get; set; }        
        public decimal TotalRevenue { get; set; } 
        public int StockCount { get; set; }       
    }

    public class DashboardDataDto
    {
        public KpiSummaryDto Kpis { get; set; }
        public List<RevenueChartPointDto> RevenueChart { get; set; }
        public OrderPieChartDto OrderChart { get; set; }
        public List<TopBookDto> TopBooks { get; set; }
    }
}