using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using BookBlossom.Infrastructure.Data;
using BookBlossom.Core.Interfaces.Services;

namespace BookBlossom.Web.Areas.Moderation.Controllers
{
    [Area("Moderation")]
    [Route("api/moderation/[controller]")]
    [ApiController]
    [Authorize(Policy = "AdminOnly")]
    public class ReportController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IGamificationService _gamificationService;

        public ReportController(ApplicationDbContext context, IGamificationService gamificationService)
        {
            _context = context;
            _gamificationService = gamificationService;
        }

        [HttpGet]
        public IActionResult Index()
        {
            return Ok(new { message = "Report API - Admin Only" });
        }

        [HttpPost("{reportId}/process")]
        public async Task<IActionResult> ProcessReport(long reportId, [FromQuery] bool isAccurate)
        {
            var report = await _context.Reports.FindAsync(reportId);
            if (report == null)
            {
                return NotFound(new { message = "Không tìm thấy báo cáo vi phạm." });
            }

            report.IsAccurate = isAccurate;
            await _context.SaveChangesAsync();

            try
            {
                await _gamificationService.CheckAndGrantReputationBadgesAsync(report.CustomerID);
            }
            catch (Exception ex)
            {
                // Log or ignore errors in badge checking during moderation
            }

            return Ok(new { message = $"Báo cáo đã được xử lý. Trạng thái chính xác: {isAccurate}" });
        }
    }
}
