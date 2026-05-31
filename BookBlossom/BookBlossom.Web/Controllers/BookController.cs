using Microsoft.AspNetCore.Mvc;

namespace BookBlossom.Web.Controllers
{
    public class BookController : Controller
    {
        [HttpGet("/Explore")]
        public IActionResult Explore()
        {
            return View();
        }

        [HttpGet("/Book/Details")]
        public IActionResult Details()
        {
            return View();
        }
    }
}