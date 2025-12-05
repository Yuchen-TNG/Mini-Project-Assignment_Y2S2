using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Google.Cloud.Firestore;
using Mini_Project_Assignment_Y2S2.Models;
using Mini_Project_Assignment_Y2S2.Services;

namespace Mini_Project_Assignment_Y2S2.Controllers
{
    public class HomeController : Controller
    {
        private readonly FirestoreDb _firestore;
        private readonly FirebaseDB _firebaseDB;

        public HomeController(FirebaseDB firebaseDB)
        {
            _firebaseDB = firebaseDB;
            _firestore = firebaseDB.Firestore;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult CardDetails(int id)
        {
            return View(id);
        }

        public IActionResult Card(int id)
        {
            return View();
        }

        // GET: Create Post
        public IActionResult CreatePost()
        {
            return View();
        }

        [HttpPost]
        public IActionResult CreatePost(Item item)
        {
            if (ModelState.IsValid)
            {
                _context.Items.Add(item);
                _context.SaveChanges();
                return RedirectToAction("Index");
            }
            return RedirectToAction("Index");

        }

        public IActionResult CreateFoundPost()
        {
            return View();
        }

        public IActionResult ChoosePostType()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel
            {
                RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier
            });
        }
    }
}
