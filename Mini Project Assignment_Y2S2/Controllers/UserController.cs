using System.Threading.Tasks;
using Google.Cloud.Firestore;
using Microsoft.AspNetCore.Mvc;
using Mini_Project_Assignment_Y2S2.Models;
using Mini_Project_Assignment_Y2S2.Services;

namespace Mini_Project_Assignment_Y2S2.Controllers
{
    public class UserController : Controller
    {
        private readonly FirestoreDb _firestore;
        private readonly FirebaseDB _firebaseDB;   // 1. Add this

        public UserController(FirebaseDB firebaseDB)
        {
            _firebaseDB = firebaseDB;              // 2. Assign it
            _firestore = firebaseDB.Firestore;
        }

        // : /User/Login
        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        // POST: /User/Login
        [HttpPost]
        public async Task<IActionResult> Login(string userId, string password)
        {
            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(password))
            {
                ViewBag.Error = "Please enter both UserID and password.";
                return View();
            }

            // Get user from Firestore (document ID = userId)
            DocumentReference docRef = _firestore.Collection("Users").Document(userId);
            DocumentSnapshot snapshot = await docRef.GetSnapshotAsync();

            if (!snapshot.Exists)
            {
                ViewBag.Error = "User ID not found.";
                return View();
            }

            var user = snapshot.ConvertTo<User>();

            if (user.Password != password)
            {
                ViewBag.Error = "Incorrect password.";
                return View();
            }

            // Store session
            HttpContext.Session.SetString("UserId", userId);

            return RedirectToAction("MyAccount");
        }

        // GET: /User/Register   (optional)
        public IActionResult Register()
        {
            return View();
        }

        // POST: /User/Register
        [HttpPost]
        public async Task<IActionResult> Register(string name, string email, string phoneNumber, string password, string confirmPassword)
        {
            if (password != confirmPassword)
            {
                ViewBag.Error = "Passwords do not match.";
                return View();
            }

            // Generate 7-digit UserID
            string userId = GenerateUserId();

            // Create a new user object
            var user = new Dictionary<string, object>
    {
        { "Name", name },
        { "Email", email },
        { "PhoneNumber", phoneNumber },
        { "Password", password },  // Consider hashing later
        { "Role", "Student" },
        { "UserID", userId }       // Store the generated UserID
    };

            // Save to Firestore with document ID = userId
            CollectionReference usersRef = _firestore.Collection("Users");
            await usersRef.Document(userId).SetAsync(user);

            return RedirectToAction("Login");
        }

        // Helper method to generate 7-digit numeric string
        private string GenerateUserId()
        {
            Random rnd = new Random();
            int number = rnd.Next(1000000, 10000000); // generates 7-digit number
            return number.ToString();
        }


        [HttpGet]
        public IActionResult ForgotPassword()
        {
            return View();
        }

        [HttpPost]
        public IActionResult ForgotPassword(string email)
        {
            // After user submits email → go to OTP page
            return RedirectToAction("ConfirmOtp");
        }


        // ============================
        // CONFIRM OTP
        // ============================
        [HttpGet]
        public IActionResult ConfirmOtp()
        {
            return View();
        }

        [HttpPost]
        public IActionResult ConfirmOtp(string otp)
        {
            // After OTP submit → go to change password page
            return RedirectToAction("ChangePassword");
        }

        // ============================
        // CHANGE PASSWORD
        // ============================
        [HttpGet]
        public IActionResult ChangePassword()
        {
            return View();
        }

        [HttpPost]
        public IActionResult ChangePassword(string newPassword, string confirmPassword)
        {
            // After changing password → return to login
            return RedirectToAction("Login");
        }

        // ============================
        // LOGOUT
        // ============================
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Login");
        }
        [HttpGet]
        public async Task<IActionResult> MyAccount()
        {
            string userId = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("Login");
            }

            DocumentReference docRef = _firestore.Collection("Users").Document(userId);
            DocumentSnapshot snapshot = await docRef.GetSnapshotAsync();

            if (!snapshot.Exists)
            {
                return RedirectToAction("Login");
            }

            // Manual mapping
            var data = snapshot.ToDictionary();

            var user = new Models.User
            {
                UserID = snapshot.Id,
                Name = data.ContainsKey("Name") ? data["Name"].ToString() : "",
                Email = data.ContainsKey("Email") ? data["Email"].ToString() : "",
                PhoneNumber = data.ContainsKey("PhoneNumber") ? data["PhoneNumber"].ToString() : "",
                Password = data.ContainsKey("Password") ? data["Password"].ToString() : "",
                Role = data.ContainsKey("Role") ? data["Role"].ToString() : ""
            };

            return View(user);
        }



        [HttpGet]
        public IActionResult ChangeCurrentPassword()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> ChangeCurrentPassword(string currentPassword, string newPassword, string confirmPassword)
        {
            // 1. Validation
            if (newPassword != confirmPassword)
            {
                ViewBag.Error = "New password and confirmation do not match.";
                return View();
            }

            string userId = HttpContext.Session.GetString("UserId");
            if (userId == null)
            {
                return RedirectToAction("Login", "User");
            }

            // 2. Retrieve user from Firebase
            var user = await _firebaseDB.GetDataAsync<User>($"Users/{userId}");

            if (user == null)
            {
                ViewBag.Error = "User not found.";
                return View();
            }

            // 3. Check current password
            if (user.Password != currentPassword)
            {
                ViewBag.Error = "Current password is incorrect.";
                return View();
            }

            // 4. Update password in Firebase
            user.Password = newPassword;

            await _firebaseDB.UpdateDataAsync($"Users/{userId}", user);

            // 5. Redirect to My Account page
            TempData["Success"] = "Password updated successfully.";
            return RedirectToAction("MyAccount");
        }
    }
}
