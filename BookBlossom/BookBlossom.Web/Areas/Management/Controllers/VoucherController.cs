using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BookBlossom.Web.Areas.Management.Controllers
{
    [Area("Management")]
    [Authorize(Policy = "MarketingManagerOnly")]
    public class VoucherController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
