using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BookBlossom.Web.Areas.Moderation.Controllers
{
    [Area("Moderation")]
    [Authorize(Policy = "ModeratorOnly")]
    public class ReportController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
