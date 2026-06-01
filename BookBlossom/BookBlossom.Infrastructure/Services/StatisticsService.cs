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
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;


namespace BookBlossom.Infrastructure.Services
{
    public class StatisticsService : IStatisticsService
    {
        private readonly ApplicationDbContext _context;

        public StatisticsService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<DashboardDataDto> GetDashboardDataAsync(DateTime from, DateTime to, string model = "All")
        {
            var endDate = to.Date.AddDays(1).AddTicks(-1);

            // --- 1. LẤY DỮ LIỆU KPI TỔNG QUAN ---
            decimal totalRevenue = 0;
            int totalOrders = 0;
            int returnedOrders = 0;

            if (model == "RealBook")
            {
                totalRevenue = await _context.OrderDetails
                    .Where(od => od.Order.OrderDate >= from && od.Order.OrderDate <= endDate && od.Order.OrderStatus == OrderStatus.Completed && od.BlindBookID == null)
                    .SumAsync(od => od.Quantity * od.UnitPrice);

                totalOrders = await _context.Orders
                    .CountAsync(o => o.OrderDate >= from && o.OrderDate <= endDate && o.OrderDetails.Any(od => od.BlindBookID == null));

                returnedOrders = await _context.Orders
                    .CountAsync(o => o.OrderDate >= from && o.OrderDate <= endDate && o.OrderStatus == OrderStatus.Returning && o.OrderDetails.Any(od => od.BlindBookID == null));
            }
            else if (model == "BlindDate")
            {
                totalRevenue = await _context.OrderDetails
                    .Where(od => od.Order.OrderDate >= from && od.Order.OrderDate <= endDate && od.Order.OrderStatus == OrderStatus.Completed && od.BlindBookID != null)
                    .SumAsync(od => od.Quantity * od.UnitPrice);

                totalOrders = await _context.Orders
                    .CountAsync(o => o.OrderDate >= from && o.OrderDate <= endDate && o.OrderDetails.Any(od => od.BlindBookID != null));

                returnedOrders = await _context.Orders
                    .CountAsync(o => o.OrderDate >= from && o.OrderDate <= endDate && o.OrderStatus == OrderStatus.Returning && o.OrderDetails.Any(od => od.BlindBookID != null));
            }
            else // All Models
            {
                totalRevenue = await _context.Orders
                    .Where(o => o.OrderDate >= from && o.OrderDate <= endDate && o.OrderStatus == OrderStatus.Completed)
                    .SumAsync(o => o.TotalAmount);

                totalOrders = await _context.Orders
                    .CountAsync(o => o.OrderDate >= from && o.OrderDate <= endDate);

                returnedOrders = await _context.Orders
                    .CountAsync(o => o.OrderDate >= from && o.OrderDate <= endDate && o.OrderStatus == OrderStatus.Returning);
            }
            
            double returnRate = totalOrders > 0 ? (double)returnedOrders / totalOrders * 100 : 0;

            // --- 2. LẤY DỮ LIỆU BIỂU ĐỒ TRÒN ---
            Dictionary<OrderStatus, int> orderStatusCounts;
            if (model == "RealBook")
            {
                orderStatusCounts = await _context.Orders
                    .Where(o => o.OrderDate >= from && o.OrderDate <= endDate && o.OrderDetails.Any(od => od.BlindBookID == null))
                    .GroupBy(o => o.OrderStatus)
                    .Select(g => new { Status = g.Key, Count = g.Count() })
                    .ToDictionaryAsync(x => x.Status, x => x.Count);
            }
            else if (model == "BlindDate")
            {
                orderStatusCounts = await _context.Orders
                    .Where(o => o.OrderDate >= from && o.OrderDate <= endDate && o.OrderDetails.Any(od => od.BlindBookID != null))
                    .GroupBy(o => o.OrderStatus)
                    .Select(g => new { Status = g.Key, Count = g.Count() })
                    .ToDictionaryAsync(x => x.Status, x => x.Count);
            }
            else
            {
                orderStatusCounts = await _context.Orders
                    .Where(o => o.OrderDate >= from && o.OrderDate <= endDate)
                    .GroupBy(o => o.OrderStatus)
                    .Select(g => new { Status = g.Key, Count = g.Count() })
                    .ToDictionaryAsync(x => x.Status, x => x.Count);
            }

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
            var orderDetailQuery = _context.OrderDetails
                .Where(od => od.Order.OrderDate >= from && od.Order.OrderDate <= endDate && od.Order.OrderStatus == OrderStatus.Completed);
            
            if (model == "RealBook")
            {
                orderDetailQuery = orderDetailQuery.Where(od => od.BlindBookID == null);
            }
            else if (model == "BlindDate")
            {
                orderDetailQuery = orderDetailQuery.Where(od => od.BlindBookID != null);
            }

            var revenueData = await orderDetailQuery
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
            var topBooksQuery = _context.OrderDetails
                .Where(od => od.Order.OrderDate >= from && od.Order.OrderDate <= endDate && od.Order.OrderStatus == OrderStatus.Completed);

            if (model == "RealBook")
            {
                topBooksQuery = topBooksQuery.Where(od => od.BlindBookID == null);
            }
            else if (model == "BlindDate")
            {
                topBooksQuery = topBooksQuery.Where(od => od.BlindBookID != null);
            }

            var topBooksRaw = await topBooksQuery
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
                    StockCount = g.Key.Stock,
                    BookID = g.Key.Id
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

        public async Task<byte[]> GenerateDashboardPdfAsync(DateTime from, DateTime to, string model = "All")
        {
            QuestPDF.Settings.License = LicenseType.Community;
            var data = await GetDashboardDataAsync(from, to, model);
            var document = new DashboardReportDocument(data, from, to, model);
            return document.GeneratePdf();
        }
    }

    internal class DashboardReportDocument : IDocument
    {
        public DashboardDataDto Data { get; }
        public DateTime From { get; }
        public DateTime To { get; }
        public string Model { get; }

        public DashboardReportDocument(DashboardDataDto data, DateTime from, DateTime to, string model)
        {
            Data = data;
            From = from;
            To = to;
            Model = model;
        }

        public DocumentMetadata GetMetadata() => DocumentMetadata.Default;

        public void Compose(IDocumentContainer container)
        {
            container.Page(page =>
            {
                page.Margin(40);
                page.Size(PageSizes.A4);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x.FontSize(10).FontFamily("Arial"));

                page.Header().Element(ComposeHeader);
                page.Content().Element(ComposeContent);
                page.Footer().Element(ComposeFooter);
            });
        }

        private void ComposeHeader(IContainer container)
        {
            container.BorderBottom(2).BorderColor(Color.FromHex("#db2777")).PaddingBottom(10).Row(row =>
            {
                row.RelativeItem().Column(column =>
                {
                    column.Item().Text("🌸 BOOK BLOSSOM").FontSize(18).Bold().FontColor(Color.FromHex("#db2777"));
                    column.Item().Text("BÁO CÁO THỐNG KÊ HOẠT ĐỘNG KINH DOANH").FontSize(14).Bold().FontColor(Colors.Grey.Darken3);
                    column.Item().Text($"Khoảng thời gian: {From:dd/MM/yyyy} - {To:dd/MM/yyyy}").FontSize(9).FontColor(Colors.Grey.Medium);
                    column.Item().Text($"Mô hình sản phẩm: {(Model == "RealBook" ? "Real Book" : Model == "BlindDate" ? "Blind Date" : "Tất cả mô hình")}").FontSize(9).FontColor(Colors.Grey.Medium);
                });

                row.ConstantItem(150).AlignRight().Column(column =>
                {
                    column.Item().Text($"Ngày xuất: {DateTime.Now:dd/MM/yyyy HH:mm}").FontSize(8).FontColor(Colors.Grey.Medium);
                    column.Item().Text("Phân hệ: Admin Panel").FontSize(8).FontColor(Colors.Grey.Medium);
                });
            });
        }

        private void ComposeContent(IContainer container)
        {
            var pinkColor = Color.FromHex("#db2777");
            var greyColor = Color.FromHex("#64748b");
            var amberColor = Color.FromHex("#d97706");
            var borderLightColor = Color.FromHex("#cbd5e1");
            var bgLightColor = Color.FromHex("#f8fafc");

            container.PaddingTop(15).Column(column =>
            {
                column.Spacing(15);

                // Section 1: KPI Cards
                column.Item().Row(row =>
                {
                    row.Spacing(15);
                    
                    // Card 1: Doanh thu
                    row.RelativeItem().Border(1).BorderColor(borderLightColor).Background(bgLightColor).Padding(10).Column(c =>
                    {
                        c.Item().Text("TỔNG DOANH THU").FontSize(8).Bold().FontColor(greyColor);
                        c.Item().Text($"{Data.Kpis.TotalRevenue:N0}đ").FontSize(15).Bold().FontColor(pinkColor);
                    });

                    // Card 2: Đơn hàng
                    row.RelativeItem().Border(1).BorderColor(borderLightColor).Background(bgLightColor).Padding(10).Column(c =>
                    {
                        c.Item().Text("TỔNG ĐƠN HÀNG").FontSize(8).Bold().FontColor(greyColor);
                        c.Item().Text($"{Data.Kpis.TotalOrders:N0} Đơn").FontSize(15).Bold().FontColor(Colors.Grey.Darken3);
                    });

                    // Card 3: Tỉ lệ hoàn
                    row.RelativeItem().Border(1).BorderColor(Colors.Amber.Lighten3).BorderLeft(4).BorderColor(Colors.Amber.Medium).Background(bgLightColor).Padding(10).Column(c =>
                    {
                        c.Item().Text("TỈ LỆ HOÀN TRẢ").FontSize(8).Bold().FontColor(greyColor);
                        c.Item().Text($"{Data.Kpis.ReturnRate}%").FontSize(15).Bold().FontColor(amberColor);
                    });
                });

                // Section 2: Order Status Table
                column.Item().Text("1. Tỉ trọng chi tiết trạng thái đơn hàng").FontSize(11).Bold().FontColor(pinkColor);
                
                int totalOrdersCount = Data.OrderChart.PendingCount + Data.OrderChart.AwaitingPickupCount + 
                                       Data.OrderChart.ShippingCount + Data.OrderChart.DeliveringCount + 
                                       Data.OrderChart.CompletedCount + Data.OrderChart.CancelledCount + 
                                       Data.OrderChart.ReturningCount;

                column.Item().Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.RelativeColumn();
                        columns.ConstantColumn(120);
                        columns.ConstantColumn(100);
                    });

                    table.Header(header =>
                    {
                        header.Cell().Background(Colors.Grey.Lighten3).Padding(5).Text("Trạng thái").Bold();
                        header.Cell().Background(Colors.Grey.Lighten3).Padding(5).AlignRight().Text("Số lượng đơn hàng").Bold();
                        header.Cell().Background(Colors.Grey.Lighten3).Padding(5).AlignRight().Text("Tỉ lệ").Bold();
                    });

                    // Hàng 1
                    double percent = totalOrdersCount > 0 ? Math.Round((double)Data.OrderChart.PendingCount / totalOrdersCount * 100, 1) : 0;
                    table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(5).Text("⏳ Chờ xử lý (Pending)");
                    table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(5).AlignRight().Text(Data.OrderChart.PendingCount.ToString());
                    table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(5).AlignRight().Text($"{percent}%");

                    // Hàng 2
                    percent = totalOrdersCount > 0 ? Math.Round((double)Data.OrderChart.AwaitingPickupCount / totalOrdersCount * 100, 1) : 0;
                    table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(5).Text("📦 Chờ lấy hàng (Awaiting Pickup)");
                    table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(5).AlignRight().Text(Data.OrderChart.AwaitingPickupCount.ToString());
                    table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(5).AlignRight().Text($"{percent}%");

                    // Hàng 3
                    percent = totalOrdersCount > 0 ? Math.Round((double)Data.OrderChart.ShippingCount / totalOrdersCount * 100, 1) : 0;
                    table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(5).Text("🚚 Đang vận chuyển (Shipping)");
                    table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(5).AlignRight().Text(Data.OrderChart.ShippingCount.ToString());
                    table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(5).AlignRight().Text($"{percent}%");

                    // Hàng 4
                    percent = totalOrdersCount > 0 ? Math.Round((double)Data.OrderChart.DeliveringCount / totalOrdersCount * 100, 1) : 0;
                    table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(5).Text("🛵 Đang giao hàng (Delivering)");
                    table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(5).AlignRight().Text(Data.OrderChart.DeliveringCount.ToString());
                    table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(5).AlignRight().Text($"{percent}%");

                    // Hàng 5
                    percent = totalOrdersCount > 0 ? Math.Round((double)Data.OrderChart.CompletedCount / totalOrdersCount * 100, 1) : 0;
                    table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(5).Text("🟢 Hoàn thành (Completed)");
                    table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(5).AlignRight().Text(Data.OrderChart.CompletedCount.ToString());
                    table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(5).AlignRight().Text($"{percent}%");

                    // Hàng 6
                    percent = totalOrdersCount > 0 ? Math.Round((double)Data.OrderChart.CancelledCount / totalOrdersCount * 100, 1) : 0;
                    table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(5).Text("🔴 Đã hủy đơn (Cancelled)");
                    table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(5).AlignRight().Text(Data.OrderChart.CancelledCount.ToString());
                    table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(5).AlignRight().Text($"{percent}%");

                    // Hàng 7
                    percent = totalOrdersCount > 0 ? Math.Round((double)Data.OrderChart.ReturningCount / totalOrdersCount * 100, 1) : 0;
                    table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(5).Text("🔄 Đang hoàn trả hàng (Returning)");
                    table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(5).AlignRight().Text(Data.OrderChart.ReturningCount.ToString());
                    table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(5).AlignRight().Text($"{percent}%");
                });

                // Section 3: Revenue Chart Data
                column.Item().Text("2. Nhật ký biến động doanh thu theo ngày").FontSize(11).Bold().FontColor(pinkColor);
                
                column.Item().Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.RelativeColumn();
                        columns.ConstantColumn(120);
                        columns.ConstantColumn(120);
                        columns.ConstantColumn(120);
                    });

                    table.Header(header =>
                    {
                        header.Cell().Background(Colors.Grey.Lighten3).Padding(5).Text("Ngày phát sinh").Bold();
                        header.Cell().Background(Colors.Grey.Lighten3).Padding(5).AlignRight().Text("Real Book").Bold();
                        header.Cell().Background(Colors.Grey.Lighten3).Padding(5).AlignRight().Text("Blind Book").Bold();
                        header.Cell().Background(Colors.Grey.Lighten3).Padding(5).AlignRight().Text("Tổng cộng").Bold();
                    });

                    foreach (var point in Data.RevenueChart)
                    {
                        table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(5).Text($"Ngày {point.DateLabel}");
                        table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(5).AlignRight().Text($"{point.RealBookRevenue:N0}đ");
                        table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(5).AlignRight().Text($"{point.BlindBookRevenue:N0}đ");
                        table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(5).AlignRight().Text($"{(point.RealBookRevenue + point.BlindBookRevenue):N0}đ").Bold();
                    }
                });

                // Section 4: Top 5 Books
                column.Item().Text("3. Danh sách Top 5 sản phẩm bán chạy nhất").FontSize(11).Bold().FontColor(pinkColor);

                column.Item().Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.ConstantColumn(40);
                        columns.RelativeColumn();
                        columns.ConstantColumn(90);
                        columns.ConstantColumn(60);
                        columns.ConstantColumn(60);
                        columns.ConstantColumn(100);
                    });

                    table.Header(header =>
                    {
                        header.Cell().Background(Colors.Grey.Lighten3).Padding(5).AlignCenter().Text("Hạng").Bold();
                        header.Cell().Background(Colors.Grey.Lighten3).Padding(5).Text("Tên sản phẩm").Bold();
                        header.Cell().Background(Colors.Grey.Lighten3).Padding(5).Text("Phân loại").Bold();
                        header.Cell().Background(Colors.Grey.Lighten3).Padding(5).AlignCenter().Text("Đã bán").Bold();
                        header.Cell().Background(Colors.Grey.Lighten3).Padding(5).AlignCenter().Text("Tồn kho").Bold();
                        header.Cell().Background(Colors.Grey.Lighten3).Padding(5).AlignRight().Text("Doanh thu").Bold();
                    });

                    foreach (var book in Data.TopBooks)
                    {
                        table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(5).AlignCenter().Text(book.Rank.ToString()).Bold();
                        table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(book.BookName);
                        table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(book.ProductType);
                        table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(5).AlignCenter().Text(book.SoldCount.ToString("N0"));
                        table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(5).AlignCenter().Text(book.StockCount.ToString("N0"));
                        table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(5).AlignRight().Text($"{book.TotalRevenue:N0}đ").Bold().FontColor(pinkColor);
                    }
                });
            });
        }

        private void ComposeFooter(IContainer container)
        {
            container.AlignBottom().AlignCenter().Column(c =>
            {
                c.Item().Text("Báo cáo số liệu kinh doanh được ký duyệt điện tử và phát hành tự động từ hệ thống BookBlossom.").FontSize(8).Italic().FontColor(Colors.Grey.Medium);
            });
        }
    }
}