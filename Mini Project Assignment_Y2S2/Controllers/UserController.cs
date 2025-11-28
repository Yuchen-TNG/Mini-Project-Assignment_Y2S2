using Microsoft.AspNetCore.Mvc;

namespace Mini_Project_Assignment_Y2S2.Controllers
{
    public class UserController : Controller
    {
        // GET: /User/Login
        public IActionResult Login()
        {
            return View();
        }

        // POST: /User/Login
        [HttpPost]
        public IActionResult Login(string username, string password)
        {
            // TODO: Add authentication logic here
            // Example:
            // Check DB for username + password
            // If correct -> redirect to profile page
            // If wrong -> return view with error message

            if (username == "admin" && password == "123")
            {
                return RedirectToAction("Index", "Home");
            }

            ViewBag.Error = "Invalid username or password!";
            return View();
        }

        // GET: /User/Register   (optional)
        public IActionResult Register()
        {
            return View();
        }

        // POST: /User/Register
        [HttpPost]
        public IActionResult Register(string username, string password, string confirmPassword)
        {
            // TODO: Save new user logic here
            // Insert into database

            if (password != confirmPassword)
            {
                ViewBag.Error = "Passwords do not match!";
                return View();
            }

            // Save to DB...
            return RedirectToAction("Login", "User");
        }

        // GET: /User/Profile   (optional)
        public IActionResult Profile()
        {
            // TODO: Load logged user info from DB
            return View();
        }
    }
}
