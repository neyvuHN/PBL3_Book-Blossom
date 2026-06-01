using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using BookBlossom.Infrastructure.Data;
using BookBlossom.Core.Interfaces.Services;
using BookBlossom.Core.Enums;
using BookBlossom.Core.Entities;
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
        public async Task<IActionResult> Index()
        {
            var reports = await _context.Reports
                .Include(r => r.Post)
                    .ThenInclude(p => p.User)
                .Include(r => r.User)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            var result = reports.Select(r => new
            {
                ReportID = r.ReportID,
                PostID = r.PostID,
                CustomerID = r.CustomerID,
                ReporterName = r.User != null ? $"{r.User.LastName} {r.User.FirstName}".Trim() : "Unknown",
                ReporterUsername = r.User?.UserName ?? "Unknown",
                Reason = r.Reason,
                ReasonText = r.Reason.ToString(),
                Description = r.Description,
                CreatedAt = r.CreatedAt,
                IsAccurate = r.IsAccurate,
                Post = r.Post != null ? new
                {
                    PostID = r.Post.PostID,
                    Title = r.Post.Title,
                    Content = r.Post.Content,
                    CreatedAt = r.Post.CreatedAt,
                    IsHidden = r.Post.IsHidden,
                    ReportCount = r.Post.ReportCount,
                    AuthorName = r.Post.User != null ? $"{r.Post.User.LastName} {r.Post.User.FirstName}".Trim() : "Unknown",
                    AuthorUsername = r.Post.User?.UserName ?? "Unknown",
                    AuthorId = r.Post.CustomerID
                } : null
            });

            return Ok(result);
        }

        [HttpPost("{reportId}/process")]
        public async Task<IActionResult> ProcessReport(long reportId, [FromQuery] bool isAccurate, [FromQuery] int? customDeduction = null)
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
                int penaltyPoints = customDeduction ?? 10;
                
                var reputation = await _context.CustomerReputations
                    .Include(r => r.MembershipRank)
                    .FirstOrDefaultAsync(r => r.CustomerID == reportedUserId);

                if (reputation != null)
                {
                    int currentPoint = reputation.ReputationPoint ?? 100;
                    int newPoint = Math.Clamp(currentPoint - penaltyPoints, 0, 150);
                    reputation.ReputationPoint = newPoint;

                    var history = new ReputationHistory
                    {
                        CustomerID = reportedUserId,
                        ChangeAmount = -penaltyPoints,
                        Reason = $"Bài viết #{report.PostID} bị báo cáo vi phạm chính xác. Điểm trừ: -{penaltyPoints}",
                        ReferenceType = (byte)ReputationAction.ReportedTrue,
                        CreateAt = DateTime.UtcNow
                    };
                    _context.ReputationHistories.Add(history);
                    
                    if (newPoint < 30)
                    {
                        var user = await _context.Users.FirstOrDefaultAsync(u => u.UserID == reportedUserId);
                        if (user != null && user.IsActive == true) 
                        {
                            user.IsActive = false;
                        }
                    }
                }
                else
                {
                    await _reputationService.HandleReputationChangeAsync(
                        reportedUserId, 
                        ReputationAction.ReportedTrue, 
                        $"Bài viết #{report.PostID} bị báo cáo vi phạm chính xác.");
                }
                
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
