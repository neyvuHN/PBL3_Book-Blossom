using BookBlossom.Web.ViewModels;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using BookBlossom.Core.Interfaces.Services;
using System.Threading.Tasks;

namespace BookBlossom.Web.Controllers
{
    public class HomeController : Controller
    {
        private readonly IThreadService _threadService;

        public HomeController(IThreadService threadService)
        {
            _threadService = threadService;
        }

        public async Task<IActionResult> Index()
        {
            var randomThreads = await _threadService.GetRandomPostsAsync(5);
            ViewBag.RandomThreads = randomThreads;
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
