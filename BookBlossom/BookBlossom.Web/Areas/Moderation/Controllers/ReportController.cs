using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using BookBlossom.Infrastructure.Data;
using BookBlossom.Core.Interfaces.Services;
using BookBlossom.Core.Enums;
using Microsoft.EntityFrameworkCore;

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
        private readonly IReputationService _reputationService;

        public ReportController(ApplicationDbContext context, IGamificationService gamificationService, IReputationService reputationService)
        {
            _context = context;
            _gamificationService = gamificationService;
            _reputationService = reputationService;
        }

        [HttpGet]
        public IActionResult Index()
        {
            return Ok(new { message = "Report API - Admin Only" });
        }

        [HttpPost("{reportId}/process")]
        public async Task<IActionResult> ProcessReport(long reportId, [FromQuery] bool isAccurate)
        {
            var report = await _context.Reports
                .Include(r => r.Post)
                .FirstOrDefaultAsync(r => r.ReportID == reportId);

            if (report == null)
            {
                return NotFound(new { message = "Không tìm thấy báo cáo vi phạm." });
            }

            report.IsAccurate = isAccurate;

            if (isAccurate && report.Post != null)
            {
                var reportedUserId = report.Post.CustomerID;
                await _reputationService.HandleReputationChangeAsync(
                    reportedUserId, 
                    ReputationAction.ReportedTrue, 
                    $"Bài viết #{report.PostID} bị báo cáo vi phạm chính xác.");
                await _reputationService.UpdateCustomerRankAsync(reportedUserId);
            }

            await _context.SaveChangesAsync();

            try
            {
                await _gamificationService.CheckAndGrantReputationBadgesAsync(report.CustomerID);
            }
            catch (Exception)
            {
                // Log or ignore errors in badge checking during moderation
            }

            return Ok(new { message = $"Báo cáo đã được xử lý. Trạng thái chính xác: {isAccurate}" });
        }
    }
}
