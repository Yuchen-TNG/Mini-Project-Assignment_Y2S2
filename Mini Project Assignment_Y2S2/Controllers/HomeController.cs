using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Mini_Project_Assignment_Y2S2.Models;

namespace Mini_Project_Assignment_Y2S2.Controllers
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
            return View();
        }

        public IActionResult Card(int id)
        {
            return View();
        }

        public IActionResult CreatePost()
        {
            return View("CreatePost");
        }

        public IActionResult VerifyPost()
        {
            return View();
        }
    }
}
