using System;
using System.Collections.Generic;

namespace BookBlossom.Core.DTOs
{
    public class KpiSummaryDto
    {
        public decimal TotalRevenue { get; set; }  
        public int TotalOrders { get; set; }       
        public double ReturnRate { get; set; }         
        public int PendingOrdersCount { get; set; }
        public int CompletedOrdersCount { get; set; }
        public int LowStockCount { get; set; }
        public int OutOfStockCount { get; set; }
        public int LifetimeBuyersCount { get; set; }
        public int PendingReportsCount { get; set; }
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
        public long BookID { get; set; }
    }

    public class LowStockBookDto
    {
        public long BookId { get; set; }
        public string Title { get; set; } = string.Empty;
        public int UnitsInStock { get; set; }
    }

    public class PendingReportDto
    {
        public long ReportId { get; set; }
        public string Reason { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }

    public class HighReportPostDto
    {
        public long PostId { get; set; }
        public string Title { get; set; } = string.Empty;
        public int ReportCount { get; set; }
        public int Threshold { get; set; }
    }

    public class DelayedPendingOrderDto
    {
        public long OrderId { get; set; }
        public DateTime OrderDate { get; set; }
        public decimal TotalAmount { get; set; }
        public string ShipReceiverName { get; set; } = string.Empty;
    }

    public class SystemConfigDto
    {
        public string ConfigName { get; set; } = string.Empty;
        public string ConfigValue { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }

    public class DashboardDataDto
    {
        public KpiSummaryDto Kpis { get; set; }
        public List<RevenueChartPointDto> RevenueChart { get; set; }
        public OrderPieChartDto OrderChart { get; set; }
        public List<TopBookDto> TopBooks { get; set; }
        public List<LowStockBookDto> LowStockBooks { get; set; } = new();
        public List<PendingReportDto> PendingReportsList { get; set; } = new();
        public List<HighReportPostDto> HighReportPosts { get; set; } = new();
        public List<DelayedPendingOrderDto> DelayedPendingOrders { get; set; } = new();
        public List<SystemConfigDto> SystemConfigs { get; set; } = new();
    }
}