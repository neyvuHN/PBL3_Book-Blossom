using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BookBlossom.Core.Interfaces.Services;
using BookBlossom.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Threading.Tasks;

namespace BookBlossom.Web.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ReputationController : ControllerBase
    {
        private readonly IReputationService _reputationService;
        private readonly ApplicationDbContext _context;

        public ReputationController(IReputationService reputationService, ApplicationDbContext context)
        {
            _reputationService = reputationService;
            _context = context;
        }

        [HttpGet("my-reputation")]
        [Authorize(Policy = "CustomerOnly")]
        public async Task<IActionResult> GetMyReputation()
        {
            var customerIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!long.TryParse(customerIdStr, out long customerId)) return Unauthorized();

            var rep = await _reputationService.GetReputationWithRankAsync(customerId);
            var canComment = await _reputationService.CanCommentAsync(customerId);
            var canUseCod = await _reputationService.CanUseCodAsync(customerId);

            // Read rank from CustomerDetail (same source as ProfileController SSR)
            // to avoid inconsistency between CustomerDetail.RankID and CustomerReputation.RankID
            var customerDetail = await _context.CustomerDetails
                .Include(cd => cd.MembershipRank)
                .FirstOrDefaultAsync(cd => cd.CustomerID == customerId);

            var spending = customerDetail?.TotalSpending ?? 0;
            var currentRankEnum = spending switch
            {
                >= 10000000m => BookBlossom.Core.Enums.RankType.Diamond,
                >= 5000000m => BookBlossom.Core.Enums.RankType.Gold,
                >= 1000000m => BookBlossom.Core.Enums.RankType.Silver,
                _ => BookBlossom.Core.Enums.RankType.Bronze
            };

            var rankName = currentRankEnum.ToString();

            return Ok(new {
                Points = rep.ReputationPoint,
                Rank = rankName,
                CanComment = canComment,
                CanUseCod = canUseCod
            });
        }
    }    
}