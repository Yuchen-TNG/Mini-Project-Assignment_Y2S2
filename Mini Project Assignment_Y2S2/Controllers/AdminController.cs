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
                Images = doc.ContainsField("Images") ? doc.GetValue<List<string>>("Images") : new List<string>(),
                IStatus = doc.ContainsField("IStatus")? doc.GetValue<string>("IStatus"): "Approved"
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



        public async Task<IActionResult> Dashboard()
        {
            if (HttpContext.Session.GetString("Role") != "Admin")
                return RedirectToAction("Login");

            var usersSnapshot = await firestoreDb.Collection("Users").GetSnapshotAsync();
            int totalUsers = usersSnapshot.Count;

            var lostSnapshot = await firestoreDb.Collection("Items")
                .WhereEqualTo("Category", "LOSTITEM")
                .GetSnapshotAsync();
            int totalLostItems = lostSnapshot.Count;

            var foundSnapshot = await firestoreDb.Collection("Items")
                .WhereEqualTo("Category", "FOUNDITEM")
                .GetSnapshotAsync();
            int totalFoundItems = foundSnapshot.Count;

            var pendingPostSnapshot = await firestoreDb.Collection("Items")
                .WhereEqualTo("IStatus", "PENDING")
                .GetSnapshotAsync();
            int totalPendingPosts = pendingPostSnapshot.Count;

            ViewBag.TotalUsers = totalUsers;
            ViewBag.TotalLostItems = totalLostItems;
            ViewBag.TotalFoundItems = totalFoundItems;
            ViewBag.TotalPendingPosts = totalPendingPosts;

            return View("Dashboard/Dashboard");
        }

        public IActionResult UserManagement()
        {
            return View("UserManagement/Index.cshtml");
        }

        public IActionResult PostManagement()
        {
            return View("~/Views/Admin/PostManagement/Index.cshtml");
        }

        #endregion

        #region Lost & Found Item Management
        public async Task<IActionResult> LostItem(string? locationID, DateTime? startDate, DateTime? endDate, int page = 1, int pageSize = 8)
        {
            // 🔍 Base query
            var query = firestoreDb.Collection("Items")
                .WhereEqualTo("Category", "LOSTITEM")
                .WhereEqualTo("IStatus", "Approved")   // ✅ USE IStatus
                .OrderByDescending("Date");

            // Filter by location
            if (!string.IsNullOrEmpty(locationID))
                query = query.WhereEqualTo("LocationID", locationID);

            // Filter by date
            if (startDate.HasValue)
                query = query.WhereGreaterThanOrEqualTo("Date", startDate.Value.ToUniversalTime());

            if (endDate.HasValue)
                query = query.WhereLessThanOrEqualTo("Date", endDate.Value.ToUniversalTime());

            var snapshot = await query.GetSnapshotAsync();
            var items = snapshot.Documents.Select(MapToItem).OrderByDescending(x => x.Date).ToList();

            var totalCount = items.Count;
            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

            var pagedItems = items.Skip((page - 1) * pageSize).Take(pageSize).ToList();

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.TotalCount = totalCount;
            ViewBag.PageSize = pageSize;
            ViewBag.LocationID = locationID;
            ViewBag.StartDate = startDate?.ToString("yyyy-MM-dd");
            ViewBag.EndDate = endDate?.ToString("yyyy-MM-dd");

            // For filter dropdown
            var locationSnapshot = await firestoreDb.Collection("Location").GetSnapshotAsync();
            ViewBag.Locations = locationSnapshot.Documents.Select(d => d.GetValue<string>("LocationID")).ToList();

            return View(pagedItems);
        }


        public async Task<IActionResult> FoundItem(string? locationID, DateTime? startDate, DateTime? endDate, int page = 1, int pageSize = 8)
        {
            // 🔍 Base query
            var query = firestoreDb.Collection("Items")
                .WhereEqualTo("Category", "FOUNDITEM")
                .WhereEqualTo("IStatus", "Approved")   // ✅ USE IStatus
                .OrderByDescending("Date"); // requires composite index for combined filters + order

            // Filter by location
            if (!string.IsNullOrEmpty(locationID))
                query = query.WhereEqualTo("LocationID", locationID);

            // Filter by date
            if (startDate.HasValue)
                query = query.WhereGreaterThanOrEqualTo("Date", startDate.Value.ToUniversalTime());

            if (endDate.HasValue)
                query = query.WhereLessThanOrEqualTo("Date", endDate.Value.ToUniversalTime());

            var snapshot = await query.GetSnapshotAsync();
            var items = snapshot.Documents.Select(MapToItem).OrderByDescending(x => x.Date).ToList();

            var totalCount = items.Count;
            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

            var pagedItems = items.Skip((page - 1) * pageSize).Take(pageSize).ToList();

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.TotalCount = totalCount;
            ViewBag.PageSize = pageSize;
            ViewBag.LocationID = locationID;
            ViewBag.StartDate = startDate?.ToString("yyyy-MM-dd");
            ViewBag.EndDate = endDate?.ToString("yyyy-MM-dd");

            var locationSnapshot = await firestoreDb.Collection("Location").GetSnapshotAsync();
            ViewBag.Locations = locationSnapshot.Documents.Select(d => d.GetValue<string>("LocationID")).ToList();

            return View(pagedItems);
        }



        public async Task<IActionResult> LostItemDetails(int itemId)
        {
            var snapshot = await firestoreDb.Collection("Items")
                .WhereEqualTo("ItemID", itemId)
                .Limit(1)
                .GetSnapshotAsync();

            if (!snapshot.Documents.Any())
                return NotFound();

            var item = MapToItem(snapshot.Documents.First());

            return View("~/Views/Admin/LostItemDetails.cshtml", item);
        }



        public async Task<IActionResult> FoundItemDetails(int itemId)
        {
            var snapshot = await firestoreDb.Collection("Items")
                .WhereEqualTo("ItemID", itemId)
                .Limit(1)
                .GetSnapshotAsync();

            if (!snapshot.Documents.Any())
                return NotFound();

            var item = MapToItem(snapshot.Documents.First());

            return View("~/Views/Admin/FoundItemDetails.cshtml", item);
        }

        [HttpPost]
        public async Task<IActionResult> DeleteLostItem(int itemId)
        {
            try
            {
                var snapshot = await firestoreDb.Collection("Items")
                    .WhereEqualTo("ItemID", itemId)
                    .WhereEqualTo("Category", "LOSTITEM")
                    .Limit(1)
                    .GetSnapshotAsync();

                if (!snapshot.Documents.Any())
                {
                    TempData["Error"] = "Lost item not found.";
                    return RedirectToAction("LostItem");
                }

                var doc = snapshot.Documents.First();

                // Add to history
                await firestoreDb.Collection("ItemHistory").AddAsync(new
                {
                    ItemID = itemId,
                    OldStatus = doc.ContainsField("IStatus") ? doc.GetValue<string>("IStatus") : "UNKNOWN",
                    NewStatus = "DELETED",
                    ChangedBy = "ADMIN",
                    ChangedAt = Timestamp.GetCurrentTimestamp()
                });

                // Delete
                await doc.Reference.DeleteAsync();

                TempData["Success"] = "Lost item deleted successfully.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Failed to delete lost item: {ex.Message}";
            }

            return RedirectToAction("LostItem");
        }

        [HttpPost]
        public async Task<IActionResult> DeleteFoundItem(int itemId)
        {
            try
            {
                var snapshot = await firestoreDb.Collection("Items")
                    .WhereEqualTo("ItemID", itemId)
                    .WhereEqualTo("Category", "FOUNDITEM")
                    .Limit(1)
                    .GetSnapshotAsync();

                if (!snapshot.Documents.Any())
                {
                    TempData["Error"] = "Found item not found.";
                    return RedirectToAction("FoundItem");
                }

                var doc = snapshot.Documents.First();

                // Add to history
                await firestoreDb.Collection("ItemHistory").AddAsync(new
                {
                    ItemID = itemId,
                    OldStatus = doc.ContainsField("IStatus") ? doc.GetValue<string>("IStatus") : "UNKNOWN",
                    NewStatus = "DELETED",
                    ChangedBy = "ADMIN",
                    ChangedAt = Timestamp.GetCurrentTimestamp()
                });

                // Delete
                await doc.Reference.DeleteAsync();

                TempData["Success"] = "Found item deleted successfully.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Failed to delete found item: {ex.Message}";
            }

            return RedirectToAction("FoundItem");
        }



        #endregion

        #region History & Status Management

        public async Task<IActionResult> History(string search = "", int page = 1, int pageSize = 8)
        {
            // ⭐ Auto expire items first
            await AutoExpireItems();

            var snapshot = await firestoreDb.Collection("Items").GetSnapshotAsync();

            var items = snapshot.Documents.Select(d => new Item
            {
                ItemID = d.ContainsField("ItemID") ? d.GetValue<int>("ItemID") : 0,
                IName = d.ContainsField("IName") ? d.GetValue<string>("IName") : null,
                IType = d.ContainsField("IType") ? d.GetValue<string>("IType") : null,
                IStatus = d.ContainsField("Status") ? d.GetValue<string>("Status") : "ACTIVE",
                Date = d.ContainsField("Date") ? d.GetValue<DateTime>("Date") : DateTime.MinValue
            });

            // Apply search filter if not empty
            if (!string.IsNullOrEmpty(search))
            {
                search = search.ToLower();
                items = items.Where(i =>
                    i.ItemID.ToString().Contains(search) ||
                    (!string.IsNullOrEmpty(i.IName) && i.IName.ToLower().Contains(search)) ||
                    (!string.IsNullOrEmpty(i.IType) && i.IType.ToLower().Contains(search)) ||
                    (!string.IsNullOrEmpty(i.IStatus) && i.IStatus.ToLower().Contains(search))
                );
            }

            var totalCount = items.Count();
            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

            // Apply paging
            var pagedItems = items
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            ViewBag.TotalCount = totalCount;
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.Search = search;
            ViewBag.PageSize = pageSize;

            return View("~/Views/Admin/History/History.cshtml", pagedItems);
        }


        private async Task AutoExpireItems()
        {
            var snapshot = await firestoreDb.Collection("Items")
                .WhereEqualTo("Status", "ACTIVE")
                .GetSnapshotAsync();

            var now = DateTime.UtcNow;

            foreach (var doc in snapshot.Documents)
            {
                if (!doc.ContainsField("Date")) continue;

                var itemDate = doc.GetValue<DateTime>("Date");

                if ((now - itemDate).TotalDays >= 4)
                {
                    await ExpireItem(doc);
                }
            }
        }

        private async Task ExpireItem(DocumentSnapshot doc)
        {
            var oldStatus = doc.ContainsField("Status")
                ? doc.GetValue<string>("Status")
                : "ACTIVE";

            // Update item status
            await doc.Reference.UpdateAsync("Status", "EXPIRED");

            // Add history
            await firestoreDb.Collection("ItemHistory").AddAsync(new
            {
                ItemID = doc.GetValue<int>("ItemID"),
                OldStatus = oldStatus,
                NewStatus = "EXPIRED",
                ChangedBy = "SYSTEM",
                ChangedAt = Timestamp.GetCurrentTimestamp()
            });
        }

        [HttpPost]
        public async Task<IActionResult> ChangeStatus(int itemId, string newStatus)
        {
            var snapshot = await firestoreDb.Collection("Items")
                .WhereEqualTo("ItemID", itemId)
                .GetSnapshotAsync();

            if (!snapshot.Documents.Any())
                return NotFound();

            var doc = snapshot.Documents.First();

            var oldStatus = doc.ContainsField("Status")
                ? doc.GetValue<string>("Status")
                : "ACTIVE";

            await doc.Reference.UpdateAsync("Status", newStatus);

            await firestoreDb.Collection("ItemHistory").AddAsync(new
            {
                ItemID = itemId,
                OldStatus = oldStatus,
                NewStatus = newStatus,
                ChangedBy = "ADMIN",
                ChangedAt = Timestamp.GetCurrentTimestamp()
            });

            return RedirectToAction("History", new { itemId = itemId });
        }


        public async Task<IActionResult> ViewHistory(int itemId)
        {
            var snapshot = await firestoreDb.Collection("ItemHistory")
                .WhereEqualTo("ItemID", itemId)
                .GetSnapshotAsync();

            var history = snapshot.Documents.Select(d => new ItemHistory
            {
                ItemID = d.GetValue<int>("ItemID"),
                OldStatus = d.GetValue<string>("OldStatus"),
                NewStatus = d.GetValue<string>("NewStatus"),
                ChangedBy = d.GetValue<string>("ChangedBy"),
                ChangedAt = d.GetValue<Timestamp>("ChangedAt").ToDateTime()
            })
            .OrderByDescending(h => h.ChangedAt)
            .ToList();

            ViewBag.ItemID = itemId;
            return View("~/Views/Admin/History/ViewHistory.cshtml", history);
        }

        #endregion
    }
}




