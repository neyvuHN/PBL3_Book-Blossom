using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using BookBlossom.Core.Enums; 
using BookBlossom.Core.DTOs;
using BookBlossom.Core.Interfaces.Services;
using BookBlossom.Infrastructure.Data;

namespace BookBlossom.Infrastructure.Services
{
    public class StatisticsService : IStatisticsService
    {
        private readonly ApplicationDbContext _context;

        public StatisticsService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<DashboardDataDto> GetDashboardDataAsync(DateTime from, DateTime to)
        {
            var endDate = to.Date.AddDays(1).AddTicks(-1);

            // --- 1. LẤY DỮ LIỆU KPI TỔNG QUAN ---
            var totalRevenue = await _context.Orders
                .Where(o => o.OrderDate >= from && o.OrderDate <= endDate && o.OrderStatus == OrderStatus.Completed)
                .SumAsync(o => o.TotalAmount);

            var totalOrders = await _context.Orders
                .CountAsync(o => o.OrderDate >= from && o.OrderDate <= endDate);

            var returnedOrders = await _context.Orders
                .CountAsync(o => o.OrderDate >= from && o.OrderDate <= endDate && o.OrderStatus == OrderStatus.Returning);
            
            double returnRate = totalOrders > 0 ? (double)returnedOrders / totalOrders * 100 : 0;

            // --- 2. LẤY DỮ LIỆU BIỂU ĐỒ TRÒN ---
            var orderStatusCounts = await _context.Orders
                .Where(o => o.OrderDate >= from && o.OrderDate <= endDate)
                .GroupBy(o => o.OrderStatus)
                .Select(g => new { Status = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.Status, x => x.Count);

            var orderChart = new OrderPieChartDto
            {
                PendingCount = orderStatusCounts.GetValueOrDefault(OrderStatus.Pending, 0),
                AwaitingPickupCount = orderStatusCounts.GetValueOrDefault(OrderStatus.AwaitingPickup, 0),
                ShippingCount = orderStatusCounts.GetValueOrDefault(OrderStatus.Shipping, 0),
                DeliveringCount = orderStatusCounts.GetValueOrDefault(OrderStatus.Delivering, 0),
                CompletedCount = orderStatusCounts.GetValueOrDefault(OrderStatus.Completed, 0),
                CancelledCount = orderStatusCounts.GetValueOrDefault(OrderStatus.Cancelled, 0),
                ReturningCount = orderStatusCounts.GetValueOrDefault(OrderStatus.Returning, 0)
            };

            // --- 3. ĐẾM CÁC THÔNG SỐ KHÁC ---
            var lowStockCount = await _context.RealBooks.CountAsync(b => b.UnitsInStock > 0 && b.UnitsInStock < 10);
            var outOfStockCount = await _context.RealBooks.CountAsync(b => b.UnitsInStock == 0);
            var lifetimeBuyersCount = await _context.Users.CountAsync(u => u.RoleID == UserRole.Customer);
            var pendingReportsCount = await _context.Reports.CountAsync(r => r.IsAccurate == null);

            var kpis = new KpiSummaryDto
            {
                TotalRevenue = totalRevenue,
                TotalOrders = totalOrders,
                ReturnRate = Math.Round(returnRate, 2),
                PendingOrdersCount = orderChart.PendingCount,
                CompletedOrdersCount = orderChart.CompletedCount,
                LowStockCount = lowStockCount,
                OutOfStockCount = outOfStockCount,
                LifetimeBuyersCount = lifetimeBuyersCount,
                PendingReportsCount = pendingReportsCount
            };

            // --- 4. LẤY DỮ LIỆU BIỂU ĐỒ ĐƯỜNG ---
            var revenueData = await _context.OrderDetails
                .Where(od => od.Order.OrderDate >= from && od.Order.OrderDate <= endDate && od.Order.OrderStatus == OrderStatus.Completed)
                .GroupBy(od => new
                {
                    Date = od.Order.OrderDate.Value.Date,
                    ProductType = od.BlindBookID != null ? "Blind Book" : "Real Book"
                })
                .Select(g => new
                {
                    Date = g.Key.Date,
                    Type = g.Key.ProductType,
                    Amount = g.Sum(od => od.Quantity * od.UnitPrice) 
                })
                .ToListAsync();

            var revenueChart = revenueData
                .GroupBy(r => r.Date)
                .Select(g => new RevenueChartPointDto
                {
                    DateLabel = g.Key.ToString("dd/MM"),
                    RealBookRevenue = g.Where(x => x.Type == "Real Book").Sum(x => x.Amount),
                    BlindBookRevenue = g.Where(x => x.Type == "Blind Book").Sum(x => x.Amount)
                })
                .OrderBy(c => c.DateLabel)
                .ToList();

            // --- 5. LẤY DỮ LIỆU TOP 5 SÁCH BÁN CHẠY ---
            var topBooksRaw = await _context.OrderDetails
                .Where(od => od.Order.OrderDate >= from && od.Order.OrderDate <= endDate && od.Order.OrderStatus == OrderStatus.Completed)
                .GroupBy(od => new 
                { 
                    Id = od.BlindBookID ?? od.BookID,
                    Title = od.RealBook.Title,
                    ProductType = od.BlindBookID != null ? "Blind Book" : "Real Book",
                    Stock = od.BlindBookID != null ? od.BlindBook.StockQuantity : od.RealBook.UnitsInStock
                })
                .Select(g => new TopBookDto
                {
                    BookName = g.Key.Title,
                    ProductType = g.Key.ProductType,
                    SoldCount = g.Sum(od => od.Quantity),
                    TotalRevenue = g.Sum(od => od.Quantity * od.UnitPrice), 
                    StockCount = g.Key.Stock
                })
                .OrderByDescending(b => b.SoldCount)
                .Take(5)
                .ToListAsync();

            int rank = 1;
            foreach (var book in topBooksRaw)
            {
                book.Rank = rank++;
            }

            // --- 6. CÁC CẢNH BÁO VÀ THÔNG TIN CHI TIẾT ---
            var lowStockBooks = await _context.RealBooks
                .Where(b => b.UnitsInStock >= 0 && b.UnitsInStock < 10)
                .OrderBy(b => b.UnitsInStock)
                .Take(5)
                .Select(b => new LowStockBookDto {
                    BookId = b.BookID,
                    Title = b.Title,
                    UnitsInStock = b.UnitsInStock
                })
                .ToListAsync();

            var pendingReportsList = await _context.Reports
                .Where(r => r.IsAccurate == null)
                .OrderByDescending(r => r.CreatedAt)
                .Take(5)
                .Select(r => new PendingReportDto {
                    ReportId = r.ReportID,
                    Reason = r.Reason.ToString(),
                    Description = r.Description,
                    CreatedAt = r.CreatedAt
                })
                .ToListAsync();

            var thresholdConfig = await _context.SystemConfigurations
                .FirstOrDefaultAsync(c => c.ConfigName == "CommunityReportThreshold");
            int threshold = thresholdConfig != null && int.TryParse(thresholdConfig.ConfigValue, out var val) ? val : 5;

            var highReportPosts = await _context.ThreadPosts
                .Where(p => p.ReportCount >= threshold)
                .OrderByDescending(p => p.ReportCount)
                .Take(5)
                .Select(p => new HighReportPostDto {
                    PostId = p.PostID,
                    Title = p.Title,
                    ReportCount = p.ReportCount,
                    Threshold = threshold
                })
                .ToListAsync();

            var timeoutConfig = await _context.SystemConfigurations
                .FirstOrDefaultAsync(c => c.ConfigName == "OrderConfirmTimeoutHours");
            int timeoutHours = timeoutConfig != null && int.TryParse(timeoutConfig.ConfigValue, out var val2) ? val2 : 48;

            var pendingTimeoutDate = DateTime.UtcNow.AddHours(-timeoutHours);
            var delayedOrders = await _context.Orders
                .Where(o => o.OrderStatus == OrderStatus.Pending && o.OrderDate != null && o.OrderDate.Value <= pendingTimeoutDate)
                .OrderBy(o => o.OrderDate)
                .Take(5)
                .Select(o => new DelayedPendingOrderDto {
                    OrderId = o.OrderID,
                    OrderDate = o.OrderDate.Value,
                    TotalAmount = o.TotalAmount,
                    ShipReceiverName = o.ShipReceiverName
                })
                .ToListAsync();

            var configs = await _context.SystemConfigurations
                .Select(c => new SystemConfigDto {
                    ConfigName = c.ConfigName,
                    ConfigValue = c.ConfigValue,
                    Description = c.Description ?? string.Empty
                })
                .ToListAsync();

            return new DashboardDataDto
            {
                Kpis = kpis,
                OrderChart = orderChart,
                RevenueChart = revenueChart,
                TopBooks = topBooksRaw,
                LowStockBooks = lowStockBooks,
                PendingReportsList = pendingReportsList,
                HighReportPosts = highReportPosts,
                DelayedPendingOrders = delayedOrders,
                SystemConfigs = configs
            };
        }

        public async Task<byte[]> GenerateDashboardPdfAsync(DateTime from, DateTime to)
        {
            var data = await GetDashboardDataAsync(from, to);

            int totalOrdersCount = data.OrderChart.PendingCount + data.OrderChart.AwaitingPickupCount + 
                                   data.OrderChart.ShippingCount + data.OrderChart.DeliveringCount + 
                                   data.OrderChart.CompletedCount + data.OrderChart.CancelledCount + 
                                   data.OrderChart.ReturningCount;

            var htmlBuilder = new StringBuilder();

            htmlBuilder.Append($@"
            <html>
            <head>
                <style>
                    body {{ font-family: 'Arial', sans-serif; color: #334155; margin: 20px; }}
                    .header {{ border-bottom: 3px solid #ec4899; padding-bottom: 15px; margin-bottom: 25px; }}
                    .logo-area {{ font-size: 24px; font-weight: bold; color: #db2777; font-family: 'Courier New', sans-serif; }}
                    .report-title {{ font-size: 20px; font-weight: bold; text-transform: uppercase; color: #1e293b; margin-top: 5px; }}
                    .meta-info {{ font-size: 12px; color: #64748b; margin-top: 5px; }}
                    
                    .kpi-container {{ display: table; width: 100%; margin-bottom: 30px; border-spacing: 10px 0px; }}
                    .kpi-card {{ display: table-cell; background: #fff; border: 1px solid #e2e8f0; border-radius: 8px; padding: 15px; text-align: left; }}
                    .kpi-card.border-amber {{ border-left: 4px solid #f59e0b; }}
                    .kpi-title {{ font-size: 11px; text-transform: uppercase; color: #94a3b8; font-weight: bold; }}
                    .kpi-value {{ font-size: 18px; font-weight: bold; color: #0f172a; margin-top: 5px; }}

                    .section-title {{ font-size: 14px; font-weight: bold; color: #0f172a; border-left: 3px solid #db2777; padding-left: 8px; margin-bottom: 12px; margin-top: 25px; text-transform: uppercase; }}
                    
                    table {{ width: 100%; border-collapse: collapse; margin-top: 10px; font-size: 13px; }}
                    th {{ background-color: #f8fafc; color: #475569; font-weight: bold; text-transform: uppercase; font-size: 11px; padding: 10px; border-bottom: 2px solid #e2e8f0; text-align: left; }}
                    td {{ padding: 10px; border-bottom: 1px solid #e2e8f0; color: #334155; }}
                    tr:nth-child(even) {{ background-color: #f8fafc; }}
                    .text-right {{ text-align: right; }}
                    .text-center {{ text-align: center; }}
                    .badge {{ background: #f1f5f9; border: 1px solid #cbd5e1; padding: 2px 6px; border-radius: 4px; font-size: 10px; font-weight: bold; }}
                    .badge-pink {{ background: #fce7f3; border-color: #fbcfe8; color: #9d174d; }}
                </style>
            </head>
            <body>
                <div class='header'>
                    <div class='logo-area'>🌸 BOOK BLOSSOM</div>
                    <div class='report-title'>Báo Cáo Thống Kê Hoạt Động Kinh Doanh</div>
                    <div class='meta-info'>
                        Khoảng thời gian báo cáo: {from:dd/MM/yyyy} - {to:dd/MM/yyyy} <br/>
                        Hệ thống kết xuất ngày: {DateTime.Now:dd/MM/yyyy HH:mm} | Quyền hạn: Tổng hệ thống (Admin Panel)
                    </div>
                </div>

                <div class='kpi-container'>
                    <div class='kpi-card'>
                        <div class='kpi-title'>Tổng doanh thu</div>
                        <div class='kpi-value'>{data.Kpis.TotalRevenue.ToString("N0")}đ</div>
                    </div>
                    <div class='kpi-card'>
                        <div class='kpi-title'>Tổng đơn hàng</div>
                        <div class='kpi-value'>{data.Kpis.TotalOrders.ToString("N0")} Đơn</div>
                    </div>
                    <div class='kpi-card border-amber'>
                        <div class='kpi-title'>Tỉ lệ hoàn trả</div>
                        <div class='kpi-value'>{data.Kpis.ReturnRate}%</div>
                    </div>
                </div>

                <div class='section-title'>1. Tỉ trọng chi tiết trạng thái đơn hàng (Pie Chart Data)</div>
                <table>
                    <thead>
                        <tr>
                            <th>Trạng thái hệ thống</th>
                            <th class='text-center'>Số lượng đơn hàng</th>
                            <th class='text-right'>Tỉ lệ phần trăm</th>
                        </tr>
                    </thead>
                    <tbody>
                        <tr><td>⏳ Chờ xử lý (Pending)</td><td class='text-center'>{data.OrderChart.PendingCount}</td><td class='text-right'>{(totalOrdersCount > 0 ? Math.Round((double)data.OrderChart.PendingCount / totalOrdersCount * 100, 1) : 0)}%</td></tr>
                        <tr><td>📦 Chờ lấy hàng (Awaiting Pickup)</td><td class='text-center'>{data.OrderChart.AwaitingPickupCount}</td><td class='text-right'>{(totalOrdersCount > 0 ? Math.Round((double)data.OrderChart.AwaitingPickupCount / totalOrdersCount * 100, 1) : 0)}%</td></tr>
                        <tr><td>🚚 Đang vận chuyển (Shipping)</td><td class='text-center'>{data.OrderChart.ShippingCount}</td><td class='text-right'>{(totalOrdersCount > 0 ? Math.Round((double)data.OrderChart.ShippingCount / totalOrdersCount * 100, 1) : 0)}%</td></tr>
                        <tr><td>🛵 Đang giao hàng (Delivering)</td><td class='text-center'>{data.OrderChart.DeliveringCount}</td><td class='text-right'>{(totalOrdersCount > 0 ? Math.Round((double)data.OrderChart.DeliveringCount / totalOrdersCount * 100, 1) : 0)}%</td></tr>
                        <tr><td>🟢 Hoàn thành xuất sắc (Completed)</td><td class='text-center'>{data.OrderChart.CompletedCount}</td><td class='text-right'>{(totalOrdersCount > 0 ? Math.Round((double)data.OrderChart.CompletedCount / totalOrdersCount * 100, 1) : 0)}%</td></tr>
                        <tr><td>🔴 Đã hủy đơn (Cancelled)</td><td class='text-center'>{data.OrderChart.CancelledCount}</td><td class='text-right'>{(totalOrdersCount > 0 ? Math.Round((double)data.OrderChart.CancelledCount / totalOrdersCount * 100, 1) : 0)}%</td></tr>
                        <tr><td>🔄 Đang hoàn trả hàng (Returning)</td><td class='text-center'>{data.OrderChart.ReturningCount}</td><td class='text-right'>{(totalOrdersCount > 0 ? Math.Round((double)data.OrderChart.ReturningCount / totalOrdersCount * 100, 1) : 0)}%</td></tr>
                    </tbody>
                </table>

                <div class='section-title'>2. Nhật ký biến động doanh thu theo mô hình sản phẩm (Line Chart Data)</div>
                <table>
                    <thead>
                        <tr>
                            <th>Ngày phát sinh</th>
                            <th class='text-right'>Doanh thu Sách Truyền Thống (Real Book)</th>
                            <th class='text-right'>Doanh thu Sách Bí Ẩn (Blind Book)</th>
                            <th class='text-right'>Tổng cộng trong ngày</th>
                        </tr>
                    </thead>
                    <tbody>");

            foreach (var point in data.RevenueChart)
            {
                htmlBuilder.Append($@"
                        <tr>
                            <td>📅 Ngày {point.DateLabel}</td>
                            <td class='text-right'>{point.RealBookRevenue.ToString("N0")}đ</td>
                            <td class='text-right'>{point.BlindBookRevenue.ToString("N0")}đ</td>
                            <td class='text-right' style='font-weight:bold; color:#1e293b;'>{(point.RealBookRevenue + point.BlindBookRevenue).ToString("N0")}đ</td>
                        </tr>");
            }

            htmlBuilder.Append($@"
                    </tbody>
                </table>

                <div class='section-title'>3. Danh sách Top 5 sản phẩm bán chạy nhất hệ thống</div>
                <table>
                    <thead>
                        <tr>
                            <th class='text-center' style='width: 50px;'>Hạng</th>
                            <th>Tên sản phẩm sách</th>
                            <th>Phân loại mô hình</th>
                            <th class='text-center'>Số lượng đã bán</th>
                            <th class='text-center'>Tồn kho khả dụng</th>
                            <th class='text-right'>Doanh thu mang lại</th>
                        </tr>
                    </thead>
                    <tbody>");

            foreach (var book in data.TopBooks)
            {
                string typeBadge = book.ProductType == "Blind Book" 
                    ? "<span class='badge badge-pink'>Blind Book</span>" 
                    : "<span class='badge'>Real Book</span>";

                htmlBuilder.Append($@"
                        <tr>
                            <td class='text-center' style='font-weight:bold; color:#64748b;'>{book.Rank}</td>
                            <td style='font-weight:600; color:#0f172a;'>{book.BookName}</td>
                            <td>{typeBadge}</td>
                            <td class='text-center' style='font-weight:500;'>{book.SoldCount.ToString("N0")}</td>
                            <td class='text-center' style='color:#64748b;'>{book.StockCount.ToString("N0")}</td>
                            <td class='text-right' style='font-weight:bold; color:#db2777;'>{book.TotalRevenue.ToString("N0")}đ</td>
                        </tr>");
            }

            htmlBuilder.Append($@"
                    </tbody>
                </table>

                <div style='margin-top: 45px; border-top: 2px dashed #e2e8f0; padding-top: 15px;'>
                    <div style='float: right; width: 45%; text-align: right; font-size: 14px;'>
                        <p style='margin: 4px 0;'><strong>Tổng doanh thu thuần thực nhận:</strong> <span style='color:#db2777; font-size:20px; font-weight:bold;'>{data.Kpis.TotalRevenue.ToString("N0")}đ</span></p>
                        <p style='font-size:11px; color:#94a3b8; font-style:italic; margin-top:10px;'>Báo cáo số liệu kinh doanh được ký duyệt điện tử và phát hành tự động.</p>
                    </div>
                </div>
            </body>
            </html>");

            return await Task.FromResult(Encoding.UTF8.GetBytes(htmlBuilder.ToString())); 
        }
    }
}