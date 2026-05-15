using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BookBlossom.Web.Areas.Moderation.Controllers
{
    [Area("Moderation")]
    [Route("api/moderation/[controller]")]
    [ApiController]
    [Authorize(Policy = "ModeratorOnly")]
    public class ReportController : ControllerBase
    {
        [HttpGet]
        public IActionResult Index()
        {
            return Ok(new { message = "Report API - Moderator Only" });
        }
    }
}
