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

        public IActionResult Card(int id)
        {
            return View();
        }

        // GET: CreatePost page
        public IActionResult CreatePost()
        {
            return View();
        }

        // POST: CreatePost - save Item to Firebase
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreatePost(Item item)
        {
            if (ModelState.IsValid)
            {
                // Save item to Firestore "Items" collection
                CollectionReference itemsRef = _firestore.Collection("Items");
                await itemsRef.AddAsync(item);

                return RedirectToAction("VerifyPost");
            }

            return View(item);
        }

        public IActionResult VerifyPost()
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
