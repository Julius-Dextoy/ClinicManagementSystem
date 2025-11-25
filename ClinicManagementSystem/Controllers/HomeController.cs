using ClinicManagementSystem.Models;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace ClinicManagementSystem.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        // LABEL: Constructor
        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        // LABEL: Index
        public IActionResult Index()
        {
            if (User.Identity.IsAuthenticated)
            {
                if (User.IsInRole("Admin"))
                    return RedirectToAction("Dashboard", "Admin");
                else if (User.IsInRole("Doctor"))
                    return RedirectToAction("Dashboard", "Doctor");
                else if (User.IsInRole("Patient"))
                    return RedirectToAction("Book", "Appointment");
            }
            return View();
        }

        // LABEL: Privacy
        public IActionResult Privacy()
        {
            return View();
        }

        // LABEL: Error
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = "123" });
        }
    }
}