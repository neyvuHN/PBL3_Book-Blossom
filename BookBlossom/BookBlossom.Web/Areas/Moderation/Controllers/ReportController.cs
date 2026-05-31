using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BookBlossom.Web.Areas.Moderation.Controllers
{
    [Area("Moderation")]
    [Route("api/moderation/[controller]")]
    [ApiController]
    [Authorize(Policy = "AdminOnly")]
    public class ReportController : ControllerBase
    {
        [HttpGet]
        public IActionResult Index()
        {
            return Ok(new { message = "Report API - Admin Only" });
        }
    }
}
