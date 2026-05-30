using Microsoft.AspNetCore.Mvc;

namespace BookBlossom.Web.Controllers
{
    public class CommunityController : Controller
    {
        [HttpGet("/Community")]
        public IActionResult Index()
        {
            return View();
        }
    }
}
