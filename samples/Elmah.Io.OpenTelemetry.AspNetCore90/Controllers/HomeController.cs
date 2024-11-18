using Elmah.Io.OpenTelemetry.AspNetCore90.Models;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace Elmah.Io.OpenTelemetry.AspNetCore90.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        public IActionResult Index()
        {
            // This won't get logged since Default LogLevel is set to Warning in Program.cs
            _logger.LogInformation("Request to the frontpage");

            return View();
        }

        public IActionResult Privacy()
        {
            _logger.LogWarning("Someone is looking at the privacy page");

            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
