using Microsoft.AspNetCore.Mvc;

namespace BookBlossom.Web.Controllers
{
    public class WishlistController : Controller
    {
        [HttpGet("/Wishlist")]
        public IActionResult Index()
        {
            return View();
        }
    }
}
