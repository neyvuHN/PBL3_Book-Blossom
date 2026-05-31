using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using BookBlossom.Core.DTOs.Statistics;
using BookBlossom.Core.Interfaces.Services;

// THƯ VIỆN ITEXT7 CHUẨN
using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Element;
using iText.IO.Font.Constants; // Thêm dòng này để dùng font hệ thống
using iText.Kernel.Font;         // Thêm dòng này để dùng PdfFontFactory

namespace BookBlossom.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Policy = "AdminOnly")]
    public class StatisticsController : ControllerBase
    {
        private readonly IStatisticsService _statisticsService;
        private readonly ILogger<StatisticsController> _logger;

        public StatisticsController(IStatisticsService statisticsService, ILogger<StatisticsController> logger)
        {
            _statisticsService = statisticsService;
            _logger = logger;
        }

        // API lấy thống kê doanh thu chi tiết theo ngày
        [HttpGet("revenue")]
        public async Task<ActionResult<RevenueStatsDTO>> GetRevenueStatistics([FromQuery] DateTime from, [FromQuery] DateTime to, [FromQuery] string period = "day")
        {
            if (from > to)
            {
                return BadRequest("Ngày bắt đầu không được lớn hơn ngày kết thúc.");
            }

            _logger.LogInformation($"Admin yêu cầu thống kê doanh thu từ {from:yyyy-MM-dd} đến {to:yyyy-MM-dd}");
            var result = await _statisticsService.GetRevenueStatisticsAsync(from, to, period);
            return Ok(result);
        }

        // API lấy top sách bán chạy / trending
        [HttpGet("trending")]
        public async Task<ActionResult<List<TrendingBookDTO>>> GetTrendingBooks([FromQuery] int top = 5)
        {
            if (top <= 0 || top > 50)
            {
                return BadRequest("Số lượng sách yêu cầu phải nằm trong khoảng từ 1 đến 50.");
            }

            _logger.LogInformation($"Admin yêu cầu lấy top {top} sách trending");
            var result = await _statisticsService.GetTrendingBooksAsync(top);
            return Ok(result);
        }

        // API lấy hiệu suất sử dụng mã giảm giá Voucher (ROI)
        [HttpGet("vouchers")]
        public async Task<ActionResult<List<VoucherPerformanceDTO>>> GetVoucherPerformance()
        {
            _logger.LogInformation("Admin yêu cầu lấy thống kê hiệu suất Voucher");
            var result = await _statisticsService.GetVoucherPerformanceAsync();
            return Ok(result);
        }

        // API lấy số liệu tổng quan và tỉ lệ hoàn hàng (Return Rate)
        [HttpGet("overview")]
        public async Task<ActionResult<OverviewStatsDTO>> GetOverviewStats([FromQuery] DateTime from, [FromQuery] DateTime to)
        {
            if (from > to)
            {
                return BadRequest("Ngày bắt đầu không được lớn hơn ngày kết thúc.");
            }

            _logger.LogInformation($"Admin yêu cầu lấy số liệu tổng quan từ {from:yyyy-MM-dd} đến {to:yyyy-MM-dd}");
            var result = await _statisticsService.GetOverviewStatsAsync(from, to);
            return Ok(result);
        }

        // API xuất báo cáo PDF chuẩn iText7 - ĐÃ SỬA LỖI SETBOLD
        [HttpGet("export-pdf")]
        public async Task<IActionResult> ExportPdfReport([FromQuery] DateTime from, [FromQuery] DateTime to)
        {
            _logger.LogInformation($"Admin yeu cau xuat bao cao PDF tu {from:yyyy-MM-dd} den {to:yyyy-MM-dd}");

            var statsData = await _statisticsService.GetOverviewStatsAsync(from, to);

            using (var memoryStream = new MemoryStream())
            {
                var writer = new PdfWriter(memoryStream);
                writer.SetCloseStream(false); 
                
                var pdf = new PdfDocument(writer);
                var document = new Document(pdf);

                // ĐÃ SỬA: Tạo Font chữ IN ĐẬM tiêu chuẩn để thay thế cho hàm .SetBold() bị lỗi
                PdfFont fontBold = PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD);

                // Áp dụng font in đậm bằng .SetFont(fontBold)
                document.Add(new Paragraph("BAO CAO THONG KE HE THONG - BOOKBLOSSOM").SetFont(fontBold).SetFontSize(16));
                
                document.Add(new Paragraph($"Tu ngay: {from:dd/MM/yyyy} - Den ngay: {to:dd/MM/yyyy}"));
                document.Add(new Paragraph("--------------------------------------------------"));
                document.Add(new Paragraph($"1. Tong doanh thu: {statsData.Revenue:N0} VND"));
                document.Add(new Paragraph($"2. Tong so don hang: {statsData.TotalOrders:N0}"));
                document.Add(new Paragraph($"3. Ti le tra hang: {statsData.ReturnRate}%"));

                document.Close();

                byte[] pdfBytes = memoryStream.ToArray();
                string fileName = $"BaoCaoThongKe_{from:yyyyMMdd}.pdf";

                return File(pdfBytes, "application/pdf", fileName);
            }
        }
    }
}