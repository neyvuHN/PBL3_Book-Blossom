using Microsoft.AspNetCore.Mvc;

namespace BookBlossom.Web.Controllers
{
    public class AuthController : Controller
    {
        public IActionResult Login()
        {
            return View();
        }

        public IActionResult Register()
        {
            return View();
        }

        public IActionResult InterestSelection()
        {
            return View();
        }
    }
}
