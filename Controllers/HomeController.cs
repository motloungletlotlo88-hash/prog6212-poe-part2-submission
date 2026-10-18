using Microsoft.AspNetCore.Mvc;

namespace RaceDay.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            // Redirect the root URL to the Events UI
            return RedirectToAction("Index", "EventsUI");
        }
    }
}