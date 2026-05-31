using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using BookBlossom.Core.DTOs.Statistics;
using BookBlossom.Core.Interfaces.Services;
using BookBlossom.Infrastructure.Data;
using BookBlossom.Core.Enums;

namespace BookBlossom.Infrastructure.Services
{
    public class StatisticsService : IStatisticsService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<StatisticsService> _logger;

        public StatisticsService(ApplicationDbContext context, ILogger<StatisticsService> logger)
        {
            _context = context;
            _logger = logger;
        }

        // 1. Thống kê doanh thu chi tiết theo khoảng thời gian
        public async Task<RevenueStatsDTO> GetRevenueStatisticsAsync(DateTime from, DateTime to, string period)
        {
            try
            {
                _logger.LogInformation($"Bắt đầu thống kê doanh thu từ ngày {from:dd/MM/yyyy} đến {to:dd/MM/yyyy} theo chu kỳ: {period}");

                var validOrders = await _context.Orders
                    .Where(o => o.OrderDate >= from && o.OrderDate <= to 
                             && o.OrderStatus == OrderStatus.Completed 
                             && o.PaymentStatus == 1)
                    .Select(o => new { o.TotalAmount, o.OrderDate })
                    .ToListAsync();

                var totalRevenue = validOrders.Sum(o => o.TotalAmount);
                var totalOrdersCount = validOrders.Count;

                List<RevenueDataPointDTO> revenueDetailsList;

                switch (period.ToLower())
                {
                    case "month":
                        revenueDetailsList = validOrders
                            .Where(o => o.OrderDate.HasValue)
                            .GroupBy(o => new { o.OrderDate!.Value.Year, o.OrderDate!.Value.Month })
                            .Select(g => new RevenueDataPointDTO
                            {
                                Label = $"{g.Key.Month:D2}/{g.Key.Year}",
                                TotalAmount = g.Sum(o => o.TotalAmount)
                            })
                            .OrderBy(d => d.Label)
                            .ToList();
                        break;

                    case "year":
                        revenueDetailsList = validOrders
                            .Where(o => o.OrderDate.HasValue)
                            .GroupBy(o => o.OrderDate!.Value.Year)
                            .Select(g => new RevenueDataPointDTO
                            {
                                Label = g.Key.ToString(),
                                TotalAmount = g.Sum(o => o.TotalAmount)
                            })
                            .OrderBy(d => d.Label)
                            .ToList();
                        break;

                    case "day":
                    default:
                        revenueDetailsList = validOrders
                            .Where(o => o.OrderDate.HasValue)
                            .GroupBy(o => o.OrderDate!.Value.Date)
                            .Select(g => new RevenueDataPointDTO
                            {
                                Label = g.Key.ToString("dd/MM/yyyy"),
                                TotalAmount = g.Sum(o => o.TotalAmount)
                            })
                            .OrderBy(d => d.Label)
                            .ToList();
                        break;
                }

                return new RevenueStatsDTO
                {
                    TotalRevenue = totalRevenue,
                    TotalOrders = totalOrdersCount,
                    DailyRevenue = revenueDetailsList 
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi xảy ra khi tính toán thống kê doanh thu hệ thống.");
                throw;
            }
        }

        // 2. Lấy danh sách Top sách bán chạy (Quét cả RealBooks và BlindBooks)
        public async Task<List<TrendingBookDTO>> GetTrendingBooksAsync(int top)
        {
            try
            {
                _logger.LogInformation($"Bắt đầu truy vấn top {top} sản phẩm sách bán chạy nhất");

                var topBookSales = await _context.OrderDetails
                    .Include(od => od.Order)
                    .Where(od => od.Order != null && od.Order.OrderStatus == OrderStatus.Completed)
                    .GroupBy(od => od.BookID)
                    .Select(g => new
                    {
                        BookId = g.Key,
                        TotalSold = g.Sum(od => od.Quantity),
                        TotalRevenue = g.Sum(od => od.Quantity * od.UnitPrice)
                    })
                    .OrderByDescending(x => x.TotalSold)
                    .Take(top)
                    .ToListAsync();

                var trendingBooks = new List<TrendingBookDTO>();

                foreach (var sale in topBookSales)
                {
                    // Bước A: Thử tìm tên sách trong bảng RealBooks trước
                    var bookTitle = await _context.RealBooks
                        .Where(b => b.BookID == sale.BookId)
                        .Select(b => b.Title)
                        .FirstOrDefaultAsync();

                    // Bước B: Nếu không thấy ở bảng RealBooks (null), tiến hành quét sang bảng BlindBooks
                    if (string.IsNullOrEmpty(bookTitle))
                    {
                        // ĐÃ SỬA: Lấy Title của RealBook thông qua quan hệ điều hướng (Navigation Property) 
                        // Nếu RealBook đi kèm bị null, sẽ fallback về tên Thể loại của BlindBook đó.
                        bookTitle = await _context.BlindBooks
                            .Where(b => b.BlindBookID == sale.BookId)
                            .Select(b => b.RealBook != null 
                                ? "[Sách bí ẩn] " + b.RealBook.Title 
                                : "[Sách bí ẩn] Thể loại: " + b.Category)
                            .FirstOrDefaultAsync();
                    }

                    // Bước C: Gán nhãn mặc định nếu cả 2 bảng đều không tìm thấy ID này
                    bookTitle ??= "Sách không rõ tên (Blind/Real)";

                    trendingBooks.Add(new TrendingBookDTO
                    {
                        BookId = sale.BookId,
                        Title = bookTitle,
                        TotalSold = sale.TotalSold,
                        TotalRevenue = sale.TotalRevenue
                    });
                }

                return trendingBooks;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Lỗi xảy ra khi lấy danh sách top {top} sách trending.");
                throw;
            }
        }

        // 3. Tính toán hiệu suất sử dụng Voucher
        public async Task<List<VoucherPerformanceDTO>> GetVoucherPerformanceAsync()
        {
            try
            {
                _logger.LogInformation("Bắt đầu tính toán hiệu suất và chỉ số ROI của hệ thống Voucher");
                return await Task.FromResult(new List<VoucherPerformanceDTO>());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi xảy ra khi thống kê hiệu suất sử dụng mã voucher giảm giá.");
                throw;
            }
        }

        // 4. Tổng hợp các chỉ số tổng quan và tỷ lệ hoàn hàng
        public async Task<OverviewStatsDTO> GetOverviewStatsAsync(DateTime from, DateTime to)
        {
            try
            {
                _logger.LogInformation($"Bắt đầu tổng hợp số liệu tổng quan hệ thống từ {from:dd/MM/yyyy} đến {to:dd/MM/yyyy}");

                var ordersInRange = await _context.Orders
                    .Where(o => o.OrderDate >= from && o.OrderDate <= to)
                    .Select(o => new { o.OrderStatus, o.PaymentStatus, o.TotalAmount })
                    .ToListAsync();

                int totalOrdersCount = ordersInRange.Count;
                int returnedOrdersCount = ordersInRange.Count(o => o.OrderStatus == OrderStatus.Returning);

                decimal successfulRevenue = ordersInRange
                    .Where(o => o.OrderStatus == OrderStatus.Completed && o.PaymentStatus == 1)
                    .Sum(o => o.TotalAmount);

                double returnRatePercentage = 0.0;
                if (totalOrdersCount > 0)
                {
                    returnRatePercentage = Math.Round(((double)returnedOrdersCount / totalOrdersCount) * 100, 2);
                }

                return new OverviewStatsDTO
                {
                    Revenue = successfulRevenue,
                    TotalOrders = totalOrdersCount,
                    ReturnedOrders = returnedOrdersCount,
                    ReturnRate = returnRatePercentage
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi xảy ra khi xử lý số liệu tổng quan và tỷ lệ hoàn hàng.");
                throw;
            }
        }
    }
}