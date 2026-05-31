using System;

namespace BookBlossom.Core.DTOs.Statistics
{
    public class OverviewStatsDTO
    {
        public decimal Revenue { get; set; }
        public int TotalOrders { get; set; }
        public int ReturnedOrders { get; set; }
        public double ReturnRate { get; set; }
    }
}