using Microsoft.AspNetCore.Mvc;

namespace BookBlossom.Web.Controllers
{
    public class MessagesController : Controller
    {
        [HttpGet("/Messages")]
        public IActionResult Index()
        {
            return View();
        }
    }
}