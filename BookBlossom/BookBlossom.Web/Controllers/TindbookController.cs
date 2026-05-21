using Microsoft.AspNetCore.Mvc;

namespace BookBlossom.Web.Controllers
{
    public class TindbookController : Controller
    {
        [HttpGet("/Tindbook")]
        public IActionResult Index()
        {
            return View();
        }
    }
}