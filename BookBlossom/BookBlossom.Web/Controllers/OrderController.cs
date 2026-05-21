using Microsoft.AspNetCore.Mvc;

namespace BookBlossom.Web.Controllers
{
    public class OrderController : Controller
    {
        [HttpGet("/Cart")]
        public IActionResult Cart()
        {
            return View();
        }

        [HttpGet("/Checkout")]
        public IActionResult Checkout()
        {
            return View();
        }
    }
}