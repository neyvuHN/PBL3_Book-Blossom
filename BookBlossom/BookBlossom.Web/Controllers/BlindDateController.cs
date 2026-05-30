using Microsoft.AspNetCore.Mvc;

namespace BookBlossom.Web.Controllers
{
    public class BlindDateController : Controller
    {
        [HttpGet("/BlindDate")]
        public IActionResult Index()
        {
            return View();
        }

        [HttpGet("/BlindDate/Details")]
        public IActionResult Details()
        {
            return View();
        }
    }
}