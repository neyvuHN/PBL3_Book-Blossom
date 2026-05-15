using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BookBlossom.Web.Areas.Management.Controllers
{
    [Area("Management")]
    [Authorize(Policy = "StaffOnly")]
    public class DashboardController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
