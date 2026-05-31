using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
// SỬA TẠI ĐÂY: Đảm bảo đúng namespace nơi bạn định nghĩa IStatisticsService
using BookBlossom.Core.Interfaces.Services; 

namespace BookBlossom.Web.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Policy = "AdminOnly")] // Bảo mật đa tầng phân quyền Admin
    public class StatisticsController : ControllerBase
    {
        private readonly IStatisticsService _statisticsService;

        public StatisticsController(IStatisticsService statisticsService)
        {
            _statisticsService = statisticsService;
        }

        /// <summary>
        /// API Lấy dữ liệu thống kê tổng hợp hiển thị lên màn hình Web Dashboard
        /// </summary>
        [HttpGet("dashboard-overview")]
        public async Task<IActionResult> GetDashboardOverview([FromQuery] DateTime from, [FromQuery] DateTime to)
        {
            if (from > to)
            {
                return BadRequest(new { Message = "Khoảng thời gian không hợp lệ. Ngày bắt đầu không thể lớn hơn ngày kết thúc." });
            }

            try
            {
                var result = await _statisticsService.GetDashboardDataAsync(from, to);
                return Ok(result);
            }
            catch (Exception ex)
            {
                // Detail = ex.Message giúp bạn dễ debug ở môi trường Dev
                return StatusCode(500, new { Message = "Đã xảy ra lỗi khi tính toán số liệu thống kê.", Detail = ex.Message });
            }
        }

        /// <summary>
        /// API Kết xuất và tải về file báo cáo PDF theo thời gian thực
        /// </summary>
        [HttpGet("export-pdf")]
        public async Task<IActionResult> ExportDashboardPdf([FromQuery] DateTime from, [FromQuery] DateTime to)
        {
            if (from > to)
            {
                return BadRequest(new { Message = "Khoảng thời gian xuất báo cáo PDF không hợp lệ." });
            }

            try
            {
                byte[] pdfFileBytes = await _statisticsService.GenerateDashboardPdfAsync(from, to);
                
                // SỬA TẠI ĐÂY: Thay ddMMffffff bằng yyyyMMdd để tên file tải về đẹp và rõ ràng (Ví dụ: BaoCao_KinhDoanh_BookBlossom_20260531_To_20260601.pdf)
                string downloadFileName = $"BaoCao_KinhDoanh_BookBlossom_{from:yyyyMMdd}_To_{to:yyyyMMdd}.pdf";
                
                return File(pdfFileBytes, "application/pdf", downloadFileName);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "Lỗi hệ thống trong quá trình đóng gói tệp PDF báo cáo.", Detail = ex.Message });
            }
        }
    }
}