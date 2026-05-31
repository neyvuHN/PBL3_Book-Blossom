using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using BookBlossom.Core.DTOs.Statistics;
using BookBlossom.Core.Interfaces.Services;

namespace BookBlossom.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Policy = "AdminOnly")] // Chỉ cho phép Admin truy cập vào các dữ liệu thống kê nhạy cảm này
    public class StatisticsController : ControllerBase
    {
        private readonly IStatisticsService _statisticsService;
        private readonly ILogger<StatisticsController> _logger;

        public StatisticsController(IStatisticsService statisticsService, ILogger<StatisticsController> logger)
        {
            _statisticsService = statisticsService;
            _logger = logger;
        }

        // ĐÃ SỬA: Bổ sung [FromQuery] string period = "day" để khớp với Interface Service
        // API lấy thống kê doanh thu chi tiết theo khoảng thời gian linh hoạt (Ngày/Tháng/Năm)
        [HttpGet("revenue")]
        public async Task<ActionResult<RevenueStatsDTO>> GetRevenueStatistics(
            [FromQuery] DateTime from, 
            [FromQuery] DateTime to, 
            [FromQuery] string period = "day") // Gán mặc định là "day" nếu client không truyền
        {
            if (from > to)
            {
                return BadRequest("Ngày bắt đầu không được lớn hơn ngày kết thúc.");
            }

            // Chuẩn hóa chuỗi period để tránh lỗi ghi log hoặc xử lý sai
            period = string.IsNullOrWhiteSpace(period) ? "day" : period.Trim().ToLower();

            // Kiểm tra giá trị hợp lệ của period để phản hồi sớm cho Frontend
            if (period != "day" && period != "month" && period != "year")
            {
                return BadRequest("Tham số chu kỳ (period) không hợp lệ. Chỉ chấp nhận: day, month, year.");
            }

            _logger.LogInformation($"Admin yêu cầu thống kê doanh thu từ {from:yyyy-MM-dd} đến {to:yyyy-MM-dd} theo chu kỳ: {period}");
            
            // ĐÃ SỬA: Truyền đủ 3 đối số (from, to, period) vào phương thức của Service
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

        // API xuất báo cáo PDF
        [HttpGet("export-pdf")]
        public async Task<IActionResult> ExportPdfReport([FromQuery] DateTime from, [FromQuery] DateTime to)
        {
            _logger.LogInformation($"Admin yêu cầu xuất báo cáo PDF từ {from:yyyy-MM-dd} đến {to:yyyy-MM-dd}");

            // Bước 1: Gọi Service lấy số liệu (Ví dụ lấy số liệu tổng quan để đưa vào PDF)
            var statsData = await _statisticsService.GetOverviewStatsAsync(from, to);

            // Bước 2: Tạm thời tạo mảng byte giả lập cấu trúc file để kiểm tra tính năng Download trên Swagger
            // (Sau này khi cài QuestPDF hoặc iText7, bạn sẽ thay đoạn này bằng code sinh dữ liệu PDF thật)
            string mockPdfContent = $"%PDF-1.5 - BAO CAO HE THONG BOOKBLOSSOM\n" +
                                    $"Tu ngay: {from:dd/MM/yyyy} - Den ngay: {to:dd/MM/yyyy}\n" +
                                    $"Tong doanh thu: {statsData.Revenue} VND\n" +
                                    $"Tong so don hang: {statsData.TotalOrders}";
                                    
            byte[] pdfBytes = System.Text.Encoding.UTF8.GetBytes(mockPdfContent);

            // Bước 3: Định dạng tên file khi Admin tải về máy
            string fileName = $"BaoCaoThongKe_{from:yyyyMMdd}_To_{to:yyyyMMdd}.pdf";

            // ĐÃ THAY ĐỔI: Thay vì trả về Ok(Object), ta trả về File(...) với Content-Type là "application/pdf"
            return File(pdfBytes, "application/pdf", fileName);
        }
    }
}