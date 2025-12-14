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

namespace Mini_Project_Assignment_Y2S2.Controllers
{
    public class AdminController : Controller
    {
        private readonly string projectId = "miniproject-d280e";
        private readonly FirestoreDb firestoreDb;

        public AdminController()
        {
            string path = Path.Combine(Directory.GetCurrentDirectory(), "firebase_config.json");
            Environment.SetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS", path);
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
        public async Task<IActionResult> Login(string Email, string Password)
        {
            try
            {
                var usersRef = firestoreDb.Collection("Admins");
                var query = usersRef.WhereEqualTo("Email", Email);
                var snapshot = await query.GetSnapshotAsync();

                if (snapshot.Count > 0)
                {
                    var document = snapshot.Documents[0];
                    if (document.ContainsField("Password") && document.GetValue<string>("Password") == Password)
                    {
                        HttpContext.Session.SetString("AdminEmail", Email); // store session
                        return RedirectToAction("Dashboard");
                    }
                }

                ViewBag.Error = "Invalid Email or Password";
                return View("~/Views/Admin/Login/Login.cshtml");
            }
            catch (Exception ex)
            {
                ViewBag.Error = $"Database Error: {ex.Message}";
                return View("~/Views/Admin/Login/Login.cshtml");
            }
        }

        [HttpGet]
        public IActionResult ForgotPassword() => View("~/Views/Admin/Login/ForgotPassword.cshtml");

        [HttpPost]
        public async Task<IActionResult> ForgotPassword(string Email)
        {
            try
            {
                var usersRef = firestoreDb.Collection("Admins");
                var snapshot = await usersRef.WhereEqualTo("Email", Email).GetSnapshotAsync();

                if (!snapshot.Documents.Any())
                {
                    ViewBag.Error = "Email not found.";
                    return View("~/Views/Admin/Login/ForgotPassword.cshtml");
                }

                var doc = snapshot.Documents[0];
                string password = doc.ContainsField("Password") ? doc.GetValue<string>("Password") : null;

                if (string.IsNullOrEmpty(password))
                {
                    ViewBag.Error = "No password found for this account.";
                    return View("~/Views/Admin/Login/ForgotPassword.cshtml");
                }

                // Send email
                var mail = new MailMessage("example.notification123@gmail.com", Email)
                {
                    Subject = "Admin Password Recovery",
                    Body = $"Your password is: {password}"
                };

                using var smtp = new SmtpClient("smtp.gmail.com", 587)
                {
                    EnableSsl = true,
                    Credentials = new NetworkCredential("example.notification123@gmail.com", "rwbjrhmkrorbbrpe")
                };
                await smtp.SendMailAsync(mail);

                ViewBag.Success = "Password sent to your email.";
                return View("~/Views/Admin/Login/ForgotPassword.cshtml");
            }
            catch (Exception ex)
            {
                ViewBag.Error = $"Error: {ex.Message}";
                return View("~/Views/Admin/Login/ForgotPassword.cshtml");
            }
        }

        #endregion

        #region Dashboard & User

        public IActionResult Dashboard() => View("~/Views/Admin/Dashboard/Dashboard.cshtml");

        public IActionResult UserManagement() => View("~/Views/Admin/UserManagement/Index.cshtml");

        #endregion

        #region Lost & Found Items

        public async Task<IActionResult> LostItem(string? locationID, DateTime? startDate, DateTime? endDate, int page = 1, int pageSize = 9)
        {
            var query = firestoreDb.Collection("Items").WhereEqualTo("Category", "LOSTITEM");

            // Filter by location
            if (!string.IsNullOrEmpty(locationID))
                query = query.WhereEqualTo("LocationID", locationID);

            // Firestore requires exact types for date comparison
            if (startDate.HasValue)
                query = query.WhereGreaterThanOrEqualTo("Date", startDate.Value.ToUniversalTime());

            if (endDate.HasValue)
                query = query.WhereLessThanOrEqualTo("Date", endDate.Value.ToUniversalTime());

            var snapshot = await query.GetSnapshotAsync();
            var items = snapshot.Documents.Select(MapToItem).OrderByDescending(x => x.Date).ToList();

            var pagedItems = items.Skip((page - 1) * pageSize).Take(pageSize).ToList();
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling(items.Count / (double)pageSize);

            // For filter dropdown
            var locationSnapshot = await firestoreDb.Collection("Location").GetSnapshotAsync();
            ViewBag.Locations = locationSnapshot.Documents.Select(d => d.GetValue<string>("LocationID")).ToList();

            return View(pagedItems);
        }

        public async Task<IActionResult> FoundItem(string? locationID, DateTime? startDate, DateTime? endDate, int page = 1, int pageSize = 9)
        {
            var query = firestoreDb.Collection("Items").WhereEqualTo("Category", "FOUNDITEM");

            if (!string.IsNullOrEmpty(locationID))
                query = query.WhereEqualTo("LocationID", locationID);

            if (startDate.HasValue)
                query = query.WhereGreaterThanOrEqualTo("Date", startDate.Value.ToUniversalTime());

            if (endDate.HasValue)
                query = query.WhereLessThanOrEqualTo("Date", endDate.Value.ToUniversalTime());

            var snapshot = await query.GetSnapshotAsync();
            var items = snapshot.Documents.Select(MapToItem).OrderByDescending(x => x.Date).ToList();

            var pagedItems = items.Skip((page - 1) * pageSize).Take(pageSize).ToList();
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling(items.Count / (double)pageSize);

            var locationSnapshot = await firestoreDb.Collection("Location").GetSnapshotAsync();
            ViewBag.Locations = locationSnapshot.Documents.Select(d => d.GetValue<string>("LocationID")).ToList();

            return View(pagedItems);
        }


        public async Task<IActionResult> LostItemDetail(int itemId)
        {
            var snapshot = await firestoreDb.Collection("Items").GetSnapshotAsync();

            var doc = snapshot.Documents.FirstOrDefault(d =>
                d.ContainsField("ItemID") && d.GetValue<int>("ItemID") == itemId);

            if (doc == null) return NotFound();

            var item = MapToItem(doc);
            return View(item);
        }


        public async Task<IActionResult> FoundItemDetail(int itemId)
        {
            var snapshot = await firestoreDb.Collection("Items").GetSnapshotAsync();

            var doc = snapshot.Documents.FirstOrDefault(d =>
                d.ContainsField("ItemID") && d.GetValue<int>("ItemID") == itemId);

            if (doc == null) return NotFound();

            var item = MapToItem(doc);
            return View(item);
        }

        #endregion
    }
}
