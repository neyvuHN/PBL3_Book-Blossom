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
            return Ok(new {
                Points = rep.ReputationPoint,
                Rank = rep.MembershipRank?.RankType
            });
        }
    }    
}