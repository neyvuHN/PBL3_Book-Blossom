using BookBlossom.Web.ViewModels.Profile;
using Microsoft.AspNetCore.Mvc;

namespace BookBlossom.Web.Controllers
{
    public class ProfileController : Controller
    {
        [HttpGet("/Profile")]
        public IActionResult Index()
        {
            var model = new ProfileViewModel();
            return View(model);
        }
    }
}
