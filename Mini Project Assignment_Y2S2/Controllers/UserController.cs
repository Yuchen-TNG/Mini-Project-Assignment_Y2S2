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
            string errorMessage = "Invalid User ID or Password";

            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(password))
            {
                ViewBag.Error = errorMessage;
                return View();
            }

            // Get user from Firestore
            DocumentReference docRef = _firestore.Collection("Users").Document(userId);
            DocumentSnapshot snapshot = await docRef.GetSnapshotAsync();

            if (!snapshot.Exists)
            {
                ViewBag.Error = errorMessage;
                return View();
            }

            var user = snapshot.ConvertTo<User>();

            if (!PasswordHelper.VerifyPassword(user.PasswordHash, password))
            {
                ViewBag.Error = errorMessage;
                return View();
            }

            HttpContext.Session.SetString("CurrentCategory", "LOSTITEM");
            // Store session
            HttpContext.Session.SetString("UserId", userId);

            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        // POST: /User/Register
        [HttpPost]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model); // Return view with validation errors
            }

            string userId = GenerateUserId();
            string passwordHash = PasswordHelper.HashPassword(model.Password);


            // Create a new user object
            var user = new Dictionary<string, object>
    {
        { "UserID", userId },
        { "Name", model.Name },
        { "Email", model.Email },
        { "PhoneNumber", model.PhoneNumber },
        { "PasswordHash", passwordHash },
        { "Role", "Student" },
        { "IsArchived", false }
    };

            // Save to Firestore with document ID = userId
            CollectionReference usersRef = _firestore.Collection("Users");
            await usersRef.Document(userId).SetAsync(user);

            TempData["RegisterSuccess"] = "You have successfully registered! We have sent your student ID via Email.";


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
        public async Task<IActionResult> ForgotPassword(string email)
        {
            if (string.IsNullOrEmpty(email))
            {
                ModelState.AddModelError("Email", "Email is required");
                return View();
            }

            // Query Firestore for matching email
            Query query = _firestore.Collection("Users")
                                    .WhereEqualTo("Email", email)
                                    .Limit(1);

            QuerySnapshot snapshot = await query.GetSnapshotAsync();

            if (snapshot.Count == 0)
            {
                // Email not found
                ModelState.AddModelError("Email", "Email not found");
                return View();
            }

            // Email exists → store email temporarily
            TempData["ResetEmail"] = email;

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
        public IActionResult ChangePassword(ChangePasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

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

            var user = snapshot.ConvertTo<User>();


            return View(user);
        }



        [HttpGet]
        public IActionResult ChangeCurrentPassword()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> ChangeCurrentPassword(ChangeCurrentPasswordViewModel model)
        {

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            string userId = HttpContext.Session.GetString("UserId");

            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("Login");
            }

            var docRef = _firestore.Collection("Users").Document(userId);
            var snapshot = await docRef.GetSnapshotAsync();
            var user = snapshot.ConvertTo<User>();

            if (!PasswordHelper.VerifyPassword(user.PasswordHash, model.CurrentPassword))
            {
                ModelState.AddModelError("CurrentPassword", "Current password is incorrect");
                return View(model);
            }

            string newHash = PasswordHelper.HashPassword(model.NewPassword);
            await docRef.UpdateAsync("PasswordHash", newHash);

            return RedirectToAction("MyAccount");
        }


        [HttpGet]
        public async Task<IActionResult> EditProfile()
        {
            string userId = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("Login");
            }

            var docRef = _firestore.Collection("Users").Document(userId);
            var snapshot = await docRef.GetSnapshotAsync();

            if (!snapshot.Exists)
            {
                return RedirectToAction("Login");
            }

            var data = snapshot.ToDictionary();

            var user = new User
            {
                UserID = snapshot.Id,
                Name = data["Name"].ToString(),
                Email = data["Email"].ToString(),
                PhoneNumber = data["PhoneNumber"].ToString(),
                ProfileImageUrl = data.ContainsKey("ProfileImageUrl") ? data["ProfileImageUrl"].ToString() : null

            };

            return View(user);
        }

        [HttpPost]
        public async Task<IActionResult> EditProfile(User model, IFormFile? ProfileImage)
        {
            string userId = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("Login");
            }

            var docRef = _firestore.Collection("Users").Document(userId);

            string imageUrl = null;

            // Handle profile image upload
            if (ProfileImage != null && ProfileImage.Length > 0)
            {
                var imagesPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/profile");
                Directory.CreateDirectory(imagesPath);

                string fileName = $"{Guid.NewGuid()}{Path.GetExtension(ProfileImage.FileName)}";
                string filePath = Path.Combine(imagesPath, fileName);

                using var stream = new FileStream(filePath, FileMode.Create);
                await ProfileImage.CopyToAsync(stream);

                imageUrl = "/profile/" + fileName;

                await docRef.UpdateAsync("ProfileImageUrl", imageUrl);
            }

            // Update other fields
            await docRef.UpdateAsync(new Dictionary<string, object>
    {
        { "Name", model.Name },
        { "Email", model.Email },
        { "PhoneNumber", model.PhoneNumber }

    });

            return RedirectToAction("MyAccount");
        }

    }
}
