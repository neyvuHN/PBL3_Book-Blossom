using Microsoft.AspNetCore.Mvc;

namespace BookBlossom.Web.Controllers
{
    public class ReviewController : Controller
    {
        [HttpGet("/Reviews")]
        public IActionResult Index()
        {
            return View();
        }
    }
}