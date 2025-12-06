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

            if (!PasswordHelper.VerifyPassword(user.PasswordHash, password))
            {
                ViewBag.Error = "Invalid User ID or password";
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

            string userId = GenerateUserId();
            string passwordHash = PasswordHelper.HashPassword(password);


            // Create a new user object
            var user = new Dictionary<string, object>
    {
        { "Name", name },
        { "Email", email },
        { "PhoneNumber", phoneNumber },
        { "PasswordHash", passwordHash }, // ✅ HASHED
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
        public async Task<IActionResult> ChangeCurrentPassword(string currentPassword,string newPassword,string confirmPassword)
        {
            if (newPassword != confirmPassword)
            {
                ViewBag.Error = "New passwords do not match";
                return View();
            }

            string userId = HttpContext.Session.GetString("UserId");

            var docRef = _firestore.Collection("Users").Document(userId);
            var snapshot = await docRef.GetSnapshotAsync();
            var user = snapshot.ConvertTo<User>();

            if (!PasswordHelper.VerifyPassword(user.PasswordHash, currentPassword))
            {
                ViewBag.Error = "Current password is incorrect";
                return View();
            }

            string newHash = PasswordHelper.HashPassword(newPassword);

            await docRef.UpdateAsync("PasswordHash", newHash);

            return RedirectToAction("MyAccount");
        }


    }
}
