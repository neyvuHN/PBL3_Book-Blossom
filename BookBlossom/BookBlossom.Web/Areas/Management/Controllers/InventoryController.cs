using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BookBlossom.Web.Areas.Management.Controllers
{
    [Area("Management")]
    [Authorize(Policy = "StoreManagerOnly")]
    public class InventoryController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
