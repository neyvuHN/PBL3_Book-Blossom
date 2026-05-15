using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BookBlossom.Web.Areas.Management.Controllers
{
    [Area("Management")]
    [Route("api/management/[controller]")]
    [ApiController]
    [Authorize(Policy = "StoreManagerOnly")]
    public class InventoryController : ControllerBase
    {
        [HttpGet]
        public IActionResult Index()
        {
            return Ok(new { message = "Inventory API - StoreManager Only" });
        }
    }
}
