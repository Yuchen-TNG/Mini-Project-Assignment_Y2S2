using Google.Cloud.Firestore;
using Microsoft.AspNetCore.Mvc;
using Mini_Project_Assignment_Y2S2.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using Google.Cloud.Firestore;
using Google.Cloud.Firestore.V1;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Templates.BlazorIdentity.Pages.Manage;
using Mini_Project_Assignment_Y2S2.Models;
using Mini_Project_Assignment_Y2S2.Services;

namespace Mini_Project_Assignment_Y2S2.Controllers
{
    public class AdminController : Controller
    {
        private readonly string projectId = "miniproject-d280e";
        private readonly FirestoreDb firestoreDb;

        private FirestoreDb firestoreDb;

        public AdminController()
        {
            // JSON FILE
            string path = Path.Combine(Directory.GetCurrentDirectory(), "firebase_config.json");

            // Environment Variable
            Environment.SetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS", path);

            // create connection
            firestoreDb = FirestoreDb.Create(projectId);
        }

        // Safe mapping for Firestore Item document
        private Item MapToItem(DocumentSnapshot doc)
        {
            return new Item
            {
                ItemID = doc.ContainsField("ItemID") ? Convert.ToInt32(doc.GetValue<object>("ItemID")) : 0,
                IName = doc.ContainsField("IName") ? doc.GetValue<string>("IName") : null,
                IType = doc.ContainsField("IType") ? doc.GetValue<string>("IType") : null,
                Idescription = doc.ContainsField("Description") ? doc.GetValue<string>("Description") : null,
                Date = doc.ContainsField("Date") ? doc.GetValue<DateTime>("Date") : DateTime.MinValue,
                LocationID = doc.ContainsField("LocationID") ? doc.GetValue<string>("LocationID") : null,
                Category = doc.ContainsField("Category") ? doc.GetValue<string>("Category") : null,
                Images = doc.ContainsField("Images") ? doc.GetValue<List<string>>("Images") : new List<string>()
            };
        }

        #region Login & Password

        [HttpGet]
        public IActionResult Login() => View("~/Views/Admin/Login/Login.cshtml");

        [HttpPost]
        public async Task<IActionResult> Login(string email, string password)
        {
            string error = "Invalid Email or Password";

            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
            {
                ViewBag.Error = error;
                return View("Login/Login");
            }

            // 🔍 Find user by email
            Query query = firestoreDb.Collection("Users")
                                    .WhereEqualTo("Email", email)
                                    .Limit(1);

            QuerySnapshot snapshot = await query.GetSnapshotAsync();

            if (snapshot.Count == 0)
                {
                ViewBag.Error = error;
                return View("Login/Login");
                    }
                }

            var user = snapshot.Documents[0].ConvertTo<User>();

            // ❌ Not admin
            if (user.Role != "Admin")
            {
                ViewBag.Error = "Access denied";
                return View("Login/Login");
            }

            if (!PasswordHelper.VerifyPassword(user.PasswordHash, password))
            {
                ViewBag.Error = error;
                return View("Login/Login");
            }

            // ✅ Store session
            HttpContext.Session.SetString("UserId", user.UserID);
            HttpContext.Session.SetString("Role", "Admin");

            return RedirectToAction("Dashboard");
        }

        [HttpGet]
        public IActionResult ForgotPassword()
        {
            return View("~/Views/Admin/Login/ForgotPassword.cshtml");
        }

        [HttpPost]
        public async Task<IActionResult> ForgotPassword(string email)
        {
            if (string.IsNullOrEmpty(email))
            {
                ViewBag.Error = "Email is required";
                return View("Login/ForgotPassword");
                }

            // 🔍 Find admin in Users table
            Query query = firestoreDb.Collection("Users")
                                     .WhereEqualTo("Email", email)
                                     .WhereEqualTo("Role", "Admin")
                                     .Limit(1);

            QuerySnapshot snapshot = await query.GetSnapshotAsync();

            if (snapshot.Count == 0)
                {
                ViewBag.Error = "Admin email not found";
                return View("Login/ForgotPassword");
                }

            // ✅ Generate OTP
            string otp = new Random().Next(100000, 999999).ToString();

            HttpContext.Session.SetString("AdminResetEmail", email);
            HttpContext.Session.SetString("AdminResetOtp", otp);


            // 📧 Send OTP email
            MailMessage mail = new MailMessage();
            mail.From = new MailAddress("example.notification123@gmail.com");
            mail.To.Add(email);
            mail.Subject = "Admin Password Reset OTP";
            mail.Body = $"Your OTP is: {otp}";

            SmtpClient smtp = new SmtpClient("smtp.gmail.com")
                {
                Port = 587,
                    EnableSsl = true,
                Credentials = new NetworkCredential(
                    "example.notification123@gmail.com",
                    "rwbjrhmkrorbbrpe"
                )
                };

                await smtp.SendMailAsync(mail);

            return RedirectToAction("ConfirmOtp");
            }

        [HttpGet]
        public IActionResult ConfirmOtp()
            {
            return View("Login/ConfirmOtp");
        }

        [HttpPost]
        public IActionResult ConfirmOtp(string otp)
        {
            string storedOtp = HttpContext.Session.GetString("AdminResetOtp");

            if (storedOtp == null)
            {
                ViewBag.Error = "OTP expired. Please request again.";
                return RedirectToAction("ForgotPassword");
            }

            if (otp != storedOtp)
            {
                ViewBag.Error = "Invalid OTP";
                return View("Login/ConfirmOtp");
            }

            return RedirectToAction("ResetPassword");
        }

        [HttpPost]
        public async Task<IActionResult> ResendOtp()
        {
            string email = HttpContext.Session.GetString("AdminResetEmail");

            if (string.IsNullOrEmpty(email))
        {
                return RedirectToAction("ForgotPassword");
            }

            // 🔄 Generate new OTP
            string newOtp = new Random().Next(100000, 999999).ToString();

            // ✅ Update OTP in session
            HttpContext.Session.SetString("AdminResetOtp", newOtp);

            // 📧 Send OTP email
            MailMessage mail = new MailMessage();
            mail.From = new MailAddress("example.notification123@gmail.com");
            mail.To.Add(email);
            mail.Subject = "Admin Password Reset OTP (Resent)";
            mail.Body = $"Your new OTP is: {newOtp}";

            SmtpClient smtp = new SmtpClient("smtp.gmail.com")
            {
                Port = 587,
                EnableSsl = true,
                Credentials = new NetworkCredential(
                    "example.notification123@gmail.com",
                    "rwbjrhmkrorbbrpe"
                )
            };

            await smtp.SendMailAsync(mail);

            ViewBag.Success = "A new OTP has been sent to your email.";

            return View("Login/ConfirmOtp");
        }


        [HttpGet]
        public IActionResult ResetPassword()
        {
            return View("Login/ResetPassword");
        }

        [HttpPost]
        public async Task<IActionResult> ResetPassword(ChangePasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View("Login/ResetPassword", model); // specify path and pass model
            }

            string email = HttpContext.Session.GetString("AdminResetEmail");
            if (string.IsNullOrEmpty(email))
            {
                return RedirectToAction("ForgotPassword");
            }

            Query query = firestoreDb.Collection("Users")
                                     .WhereEqualTo("Email", email)
                                     .WhereEqualTo("Role", "Admin")
                                     .Limit(1);

            QuerySnapshot snapshot = await query.GetSnapshotAsync();
            if (snapshot.Count == 0)
            {
                ViewBag.Error = "User not found";
                return View("Login/ResetPassword", model);
        }

            var docRef = snapshot.Documents[0].Reference;
            var user = snapshot.Documents[0].ConvertTo<User>();

            // ❌ Prevent resetting to current password
            if (PasswordHelper.VerifyPassword(user.PasswordHash, model.NewPassword?.Trim()))
        {
                ModelState.AddModelError("NewPassword", "New password cannot be the same as the current password");
                return View("Login/ResetPassword", model);
            }

            string hashedPassword = PasswordHelper.HashPassword(model.NewPassword);
            await docRef.UpdateAsync("PasswordHash", hashedPassword);

            HttpContext.Session.Remove("AdminResetEmail");
            HttpContext.Session.Remove("AdminResetOtp");

            TempData["Success"] = "Password reset successfully. Please login.";
            return RedirectToAction("Login");
        }


        public IActionResult Dashboard()
        {
            // 🔒 Protect route
            if (HttpContext.Session.GetString("Role") != "Admin")
                return RedirectToAction("Login");

            return View("Dashboard/Dashboard");
        }
        public IActionResult UserManagement()
        {
            return View("~/Views/Admin/UserManagement/Index.cshtml");
        }

    }
}


