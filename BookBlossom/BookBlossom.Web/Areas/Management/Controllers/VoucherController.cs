using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BookBlossom.Web.Areas.Management.Controllers
{
    [Area("Management")]
    [Route("api/management/[controller]")]
    [ApiController]
    [Authorize(Policy = "AdminOnly")]
    public class VoucherController : ControllerBase
    {
        [HttpGet]
        public IActionResult Index()
        {
            return Ok(new { message = "Voucher API - Admin Only" });
        }
    }
}
