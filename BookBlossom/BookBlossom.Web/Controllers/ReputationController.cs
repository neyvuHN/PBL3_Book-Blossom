using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BookBlossom.Core.Interfaces.Services;
using System.Security.Claims;
using System.Threading.Tasks;

namespace BookBlossom.Web.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ReputationController : ControllerBase
    {
        private readonly IReputationService _reputationService;

        public ReputationController(IReputationService reputationService)
        {
            _reputationService = reputationService;
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
            return Ok(new {
                points = rep.ReputationPoint,
                Points = rep.ReputationPoint,
                rank = rep.MembershipRank?.RankType.ToString(),
                Rank = rep.MembershipRank?.RankType.ToString(),
                canComment = canComment,
                CanComment = canComment,
                canUseCod = canUseCod,
                CanUseCod = canUseCod
            });
        }
    }    
}