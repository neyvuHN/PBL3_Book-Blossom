using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BookBlossom.Web.Areas.Management.Controllers
{
    [Area("Management")]
    [Route("api/management/[controller]")]
    [Authorize(Policy = "MarketingManagerOnly")]
    public class VoucherController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
