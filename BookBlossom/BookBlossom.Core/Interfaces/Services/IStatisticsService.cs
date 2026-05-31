using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BookBlossom.Core.DTOs.Statistics;

namespace BookBlossom.Core.Interfaces.Services
{
    public interface IStatisticsService
    {
        // 1. Thống kê doanh thu theo khoảng thời gian và chu kỳ lọc (day, month, year)
        Task<RevenueStatsDTO> GetRevenueStatisticsAsync(DateTime from, DateTime to, string period);

        // 2. Lấy danh sách sách đang thịnh hành/bán chạy
        Task<List<TrendingBookDTO>> GetTrendingBooksAsync(int top);

        // 3. Tính toán hiệu suất voucher (ROI)
        Task<List<VoucherPerformanceDTO>> GetVoucherPerformanceAsync();

        // 4. Lấy các chỉ số tổng quan (Doanh thu, số đơn, tỉ lệ hoàn hàng)
        Task<OverviewStatsDTO> GetOverviewStatsAsync(DateTime from, DateTime to);
    }
}