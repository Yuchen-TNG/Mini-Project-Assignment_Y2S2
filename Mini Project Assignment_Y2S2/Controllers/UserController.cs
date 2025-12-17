using System.Net;
using System.Net.Mail;
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

            // for archived account
            if (user.IsArchived)
            {
                ViewBag.Error =
                    "Your account has been archived. Please contact the administrator for assistance.";
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

            Query emailQuery = _firestore.Collection("Users")
                                 .WhereEqualTo("Email", model.Email)
                                 .Limit(1);
            QuerySnapshot emailSnapshot = await emailQuery.GetSnapshotAsync();
            if (emailSnapshot.Count > 0)
            {
                ModelState.AddModelError("Email", "This email is already registered.");
                return View(model);
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

            // ----------------------------
            // Send UserID to student's email
            // ----------------------------
            try
            {
                MailMessage mail = new MailMessage();
                mail.From = new MailAddress("example.notification123@gmail.com", "Your School Name");
                mail.To.Add(model.Email);
                mail.Subject = "Your Student ID";
                mail.Body = $"Hello {model.Name},\n\nYour student ID is: {userId}\nUse this ID to login to your account.";

                SmtpClient smtp = new SmtpClient("smtp.gmail.com")
                {
                    Port = 587,
                    EnableSsl = true,
                    Credentials = new NetworkCredential(
                        "example.notification123@gmail.com",
                        "rwbjrhmkrorbbrpe" // Must be Gmail App Password, not normal password
                    )
                };

                await smtp.SendMailAsync(mail);
            }
            catch (Exception ex)
            {
                TempData["EmailError"] = $"Failed to send email: {ex.Message}";
            }

            TempData["RegisterSuccess"] = "You have successfully registered! Your student ID has been sent to your email.";

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

            // Find student by email
            Query query = _firestore.Collection("Users")
                                     .WhereEqualTo("Email", email)
                                     .WhereEqualTo("Role", "Student")
                                     .Limit(1);

            QuerySnapshot snapshot = await query.GetSnapshotAsync();
            if (snapshot.Count == 0)
            {
                ModelState.AddModelError("Email", "Email not found");
                return View();
            }

            // Generate OTP
            string otp = new Random().Next(100000, 999999).ToString();

            // Store in session
            HttpContext.Session.SetString("ResetEmail", email);
            HttpContext.Session.SetString("ResetOtp", otp);

            // Send OTP via email
            MailMessage mail = new MailMessage();
            mail.From = new MailAddress("example.notification123@gmail.com");
            mail.To.Add(email);
            mail.Subject = "Student Password Reset OTP";
            mail.Body = $"Your OTP is: {otp}";

            SmtpClient smtp = new SmtpClient("smtp.gmail.com")
            {
                Port = 587,
                EnableSsl = true,
                Credentials = new NetworkCredential("example.notification123@gmail.com", "rwbjrhmkrorbbrpe")
            };

            await smtp.SendMailAsync(mail);

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
            string storedOtp = HttpContext.Session.GetString("ResetOtp");

            if (string.IsNullOrEmpty(storedOtp))
            {
                ViewBag.Error = "OTP expired. Please request again.";
                return RedirectToAction("ForgotPassword");
            }

            if (otp != storedOtp)
            {
                ViewBag.Error = "Invalid OTP";
                return View();
            }

            // ✅ OTP correct → go to reset password
            return RedirectToAction("ChangePassword");
        }

        [HttpPost]
        public async Task<IActionResult> ResendOtp()
        {
            // Get student email from session
            string email = HttpContext.Session.GetString("ResetEmail");

            if (string.IsNullOrEmpty(email))
            {
                // If no email stored, redirect to forgot password
                return RedirectToAction("ForgotPassword");
            }

            // 🔄 Generate new OTP
            string newOtp = new Random().Next(100000, 999999).ToString();

            // ✅ Update OTP in session
            HttpContext.Session.SetString("ResetOtp", newOtp);

            // 📧 Send OTP email
            MailMessage mail = new MailMessage();
            mail.From = new MailAddress("example.notification123@gmail.com");
            mail.To.Add(email);
            mail.Subject = "Student Password Reset OTP (Resent)";
            mail.Body = $"Your new OTP is: {newOtp}";

            SmtpClient smtp = new SmtpClient("smtp.gmail.com")
            {
                Port = 587,
                EnableSsl = true,
                Credentials = new NetworkCredential(
                    "example.notification123@gmail.com",
                    "your-app-password" // Use app password or real credentials
                )
            };

            try
            {
                await smtp.SendMailAsync(mail);
                ViewBag.Success = "A new OTP has been sent to your email.";
            }
            catch (Exception ex)
            {
                ViewBag.Error = "Failed to send OTP: " + ex.Message;
            }

            return View("ConfirmOtp");
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
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            string email = HttpContext.Session.GetString("ResetEmail");
            if (string.IsNullOrEmpty(email))
            {
                return RedirectToAction("ForgotPassword");
            }

            // Find student
            Query query = _firestore.Collection("Users")
                                     .WhereEqualTo("Email", email)
                                     .WhereEqualTo("Role", "Student")
                                     .Limit(1);

            QuerySnapshot snapshot = await query.GetSnapshotAsync();
            if (snapshot.Count == 0)
            {
                ViewBag.Error = "User not found";
                return View(model);
            }

            var docRef = snapshot.Documents[0].Reference;
            var user = snapshot.Documents[0].ConvertTo<User>();

            // ❌ Check if new password is the same as old
            if (PasswordHelper.VerifyPassword(user.PasswordHash, model.NewPassword))
            {
                ModelState.AddModelError("NewPassword", "New password cannot be the same as your current password.");
                return View(model);
            }

            string hashedPassword = PasswordHelper.HashPassword(model.NewPassword);
            await docRef.UpdateAsync("PasswordHash", hashedPassword);

            // Clear session
            HttpContext.Session.Remove("ResetEmail");
            HttpContext.Session.Remove("ResetOtp");

            TempData["Success"] = "Password reset successfully. Please login.";
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

            if (PasswordHelper.VerifyPassword(user.PasswordHash, model.NewPassword))
            {
                ModelState.AddModelError("NewPassword", "New password cannot be the same as the current password");
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
                return RedirectToAction("Login");

            var data = snapshot.ToDictionary();

            var model = new EditProfileViewModel
            {
                Name = data["Name"].ToString(),
                Email = data["Email"].ToString(),
                PhoneNumber = data["PhoneNumber"].ToString(),
                ProfileImageUrl = data.ContainsKey("ProfileImageUrl") ? data["ProfileImageUrl"].ToString() : null
            };

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> EditProfile(EditProfileViewModel model, IFormFile? ProfileImage)
        {


            string userId = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userId))
                return RedirectToAction("Login");
            var docRef = _firestore.Collection("Users").Document(userId);


            // Check if email is already used by another user
            Query emailQuery = _firestore.Collection("Users")
                                          .WhereEqualTo("Email", model.Email)
                                          .WhereNotEqualTo("UserID", userId)
                                          .Limit(1);

            var emailSnap = await emailQuery.GetSnapshotAsync();
            if (emailSnap.Count > 0)
            {
                ModelState.AddModelError("Email", "This email is already used by another account.");
                return View(model);
            }



            string imageUrl = model.ProfileImageUrl; // keep current image URL in case of validation errors

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

            if (!ModelState.IsValid)
                return View(model);

            var updateData = new Dictionary<string, object>
    {
        { "Name", model.Name },
        { "Email", model.Email },
        { "PhoneNumber", model.PhoneNumber }
    };
            await docRef.UpdateAsync(updateData);

            TempData["Success"] = "Profile updated successfully!";
            return RedirectToAction("MyAccount");
        }

        public async Task<IActionResult> MyPost(string? category, string? status, int page = 1, int pageSize = 5)
        {
            string userId = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userId))
                return RedirectToAction("Login");

            // 🔹 Base query: user's items
            Query itemQuery = _firestore.Collection("Items")
                                        .WhereEqualTo("UserID", userId);

            // 添加状态过滤
            if (!string.IsNullOrEmpty(status))
            {
                if (status == "CLAIMED")
                {
                    itemQuery = itemQuery.WhereEqualTo("IStatus", "CLAIMED");
                }
                else if (status == "PENDING")
                {
                    itemQuery = itemQuery.WhereEqualTo("IStatus", "PENDING");
                }
                else if (status == "APPROVED")
                {
                    itemQuery = itemQuery.WhereEqualTo("IStatus", "Approved");
                }
                else if (status == "REJECTED")
                {
                    itemQuery = itemQuery.WhereEqualTo("IStatus", "Rejected");
                }
                else if (status == "EXPIRED")
                {
                    itemQuery = itemQuery.WhereEqualTo("IStatus", "EXPIRED");
                }
            }

            // 添加类别过滤
            if (!string.IsNullOrEmpty(category))
            {
                itemQuery = itemQuery.WhereEqualTo("Category", category);
            }

            // 按日期降序排序
            itemQuery = itemQuery.OrderByDescending("Date");

            // 获取总数量
            var totalQuery = itemQuery;
            var totalSnapshot = await totalQuery.GetSnapshotAsync();
            int totalItems = totalSnapshot.Count;

            // 计算分页
            int totalPages = (int)Math.Ceiling((double)totalItems / pageSize);

            // 确保页码有效
            if (page < 1) page = 1;
            if (page > totalPages && totalPages > 0) page = totalPages;

            // 应用分页
            itemQuery = itemQuery.Offset((page - 1) * pageSize).Limit(pageSize);

            // 获取数据
            var snap = await itemQuery.GetSnapshotAsync();
            var itemDocs = snap.Documents.ToList();

            ViewBag.Category = category;
            ViewBag.Status = status;
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.TotalItems = totalItems;
            ViewBag.PageSize = pageSize;

            return View(itemDocs);
        }

        private Item MapToItem(DocumentSnapshot doc)
        {
            return new Item
            {
                ItemID = doc.ContainsField("ItemID") ? doc.GetValue<int>("ItemID") : 0,
                LocationID = doc.ContainsField("LocationID") ? doc.GetValue<string>("LocationID") : null,
                LocationOther = doc.ContainsField("LocationOther") ? doc.GetValue<string>("LocationOther") : null,
                Images = doc.ContainsField("Images") ? doc.GetValue<List<string>>("Images") : new List<string>(),
                IType = doc.ContainsField("IType") ? doc.GetValue<string>("IType") : null,
                IName = doc.ContainsField("IName") ? doc.GetValue<string>("IName") : null,
                Idescription = doc.ContainsField("Description")
                                ? doc.GetValue<string>("Description")
                                : doc.ContainsField("Idescription")
                                    ? doc.GetValue<string>("Idescription")
                                    : null,
                Date = doc.ContainsField("Date") ? doc.GetValue<DateTime>("Date") : DateTime.MinValue,
                Category = doc.ContainsField("Category") ? doc.GetValue<string>("Category") : null,
                UserID = doc.ContainsField("UserID") ? doc.GetValue<string>("UserID") : null, // ⭐ 加这行
                LocationFound = doc.ContainsField("LocationFound") ? doc.GetValue<string>("LocationFound") : null,
                IStatus = doc.ContainsField("IStatus") ? doc.GetValue<string>("IStatus") : null
            };
        }

        private Location MapToLocation(DocumentSnapshot doc)
        {
            return new Location
            {
                LocationID = doc.ContainsField("LocationID") ? doc.GetValue<string>("LocationID") : null,
                LocationName = doc.ContainsField("LocationName") ? doc.GetValue<string>("LocationName") : null,
                Items = new List<Item>() // 这里先空着，后续可以填充对应的 Items
            };
        }

        public async Task<IActionResult> MyPostDetails(int itemId)
        {

            var collection = _firestore.Collection("Items");
            Google.Cloud.Firestore.Query query = collection.WhereEqualTo("ItemID", itemId);
            QuerySnapshot snapshot = await query.GetSnapshotAsync();

            if (snapshot.Documents.Count == 0)
                return NotFound();

            var item = MapToItem(snapshot.Documents[0]);


            User user = null;

            var collectionUser = _firestore.Collection("Users");
            Google.Cloud.Firestore.Query queryUser = collectionUser.WhereEqualTo("UserID", item.UserID);
            QuerySnapshot snapshotsUser = await queryUser.GetSnapshotAsync();

            var userDoc = snapshotsUser.Documents[0];
            user = new User
            {
                Name = userDoc.GetValue<string>("Name"),
                PhoneNumber = userDoc.GetValue<string>("PhoneNumber"),
                Email = userDoc.GetValue<string>("Email"),
                UserID = userDoc.GetValue<string>("UserID")
            };



            var locationCollection = _firestore.Collection("Location");
            Google.Cloud.Firestore.Query locationQuery = locationCollection.WhereEqualTo("LocationID", item.LocationID);
            QuerySnapshot locationSnapshot = await locationQuery.GetSnapshotAsync();

            var location = MapToLocation(locationSnapshot.Documents[0]);

            var viewModel = new CardDetailsViewModel
            {
                Item = item,
                User = user,
                Location = location
            };
            return View(viewModel);
        }

        [HttpGet]
        public async Task<IActionResult> EditPost(int itemId)
        {
            string userId = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userId))
                return RedirectToAction("Login");

            // Get the item
            var collection = _firestore.Collection("Items");
            var query = collection.WhereEqualTo("ItemID", itemId)
                                  .WhereEqualTo("UserID", userId);
            var snapshot = await query.GetSnapshotAsync();

            if (snapshot.Documents.Count == 0)
                return NotFound();

            var doc = snapshot.Documents[0];
            var item = MapToItem(doc);

            // Load all locations for dropdown
            var locationSnapshot = await _firestore.Collection("Location").GetSnapshotAsync();
            ViewBag.Locations = locationSnapshot.Documents.Select(d => MapToLocation(d)).ToList();

            return View(item); // pass item as model
        }

        [HttpPost]
        public async Task<IActionResult> EditPost(Item model, IFormFile[] ImageFiles, string? OtherLocation, List<string>? ExistingImages)
        {
            string userId = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userId))
                return RedirectToAction("Login");

            var collection = _firestore.Collection("Items");
            var query = collection.WhereEqualTo("ItemID", model.ItemID)
                                  .WhereEqualTo("UserID", userId);
            var snapshot = await query.GetSnapshotAsync();

            if (snapshot.Documents.Count == 0)
                return NotFound();

            var docRef = snapshot.Documents[0].Reference;

            // Handle OtherLocation
            if (!string.IsNullOrEmpty(OtherLocation))
                model.LocationID = OtherLocation;

            // ✅ 处理现有图片：使用 ExistingImages 参数
            var imageUrls = new List<string>();

            // 1. 保留用户选择的现有图片
            if (ExistingImages != null && ExistingImages.Count > 0)
            {
                imageUrls.AddRange(ExistingImages);
            }

            // 2. 处理新上传的图片
            if (ImageFiles != null && ImageFiles.Length > 0)
            {
                var imagesPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/itemimages");
                Directory.CreateDirectory(imagesPath);

                foreach (var file in ImageFiles)
                {
                    if (file.Length > 0)
                    {
                        string fileName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
                        string filePath = Path.Combine(imagesPath, fileName);

                        using var stream = new FileStream(filePath, FileMode.Create);
                        await file.CopyToAsync(stream);

                        imageUrls.Add("/itemimages/" + fileName);
                    }
                }
            }

            // ✅ 关键修复：将DateTime转换为UTC
            var dateTimeUtc = model.Date.ToUniversalTime();

            var updateData = new Dictionary<string, object>
    {
        { "IName", model.IName },
        { "IType", model.IType },
        { "Idescription", model.Idescription },
        { "Date", dateTimeUtc }, // 使用UTC时间
        { "LocationID", model.LocationID },
        { "LocationFound", model.LocationFound },
        { "Images", imageUrls } // ✅ 只保存最终的图片列表
    };

            await docRef.UpdateAsync(updateData);

            TempData["Success"] = "Post updated successfully!";
            return RedirectToAction("MyPost");
        }

        [HttpPost]
        public async Task<IActionResult> DeletePost(int itemId)
        {
            string userId = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userId))
                return RedirectToAction("Login");

            try
            {
                // 找到用户要删除的帖子
                var collection = _firestore.Collection("Items");
                var query = collection.WhereEqualTo("ItemID", itemId)
                                      .WhereEqualTo("UserID", userId);
                var snapshot = await query.GetSnapshotAsync();

                if (snapshot.Documents.Count == 0)
                {
                    TempData["Error"] = "Post not found or you don't have permission to delete it.";
                    return RedirectToAction("MyPost");
                }

                // 获取文档引用并删除
                var docRef = snapshot.Documents[0].Reference;
                await docRef.DeleteAsync();

                // 可选：删除相关的图片文件
                var item = snapshot.Documents[0];
                if (item.ContainsField("Images"))
                {
                    var images = item.GetValue<List<string>>("Images");
                    await DeleteImageFiles(images);
                }

                TempData["Success"] = "Post deleted successfully!";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error deleting post: {ex.Message}";
            }

            return RedirectToAction("MyPost");
        }

        // 辅助方法：删除图片文件
        private async Task DeleteImageFiles(List<string> imageUrls)
        {
            if (imageUrls == null || !imageUrls.Any())
                return;

            try
            {
                foreach (var imageUrl in imageUrls)
                {
                    if (!string.IsNullOrEmpty(imageUrl) && imageUrl.StartsWith("/images/"))
                    {
                        var fileName = Path.GetFileName(imageUrl);
                        var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images", fileName);

                        if (System.IO.File.Exists(filePath))
                        {
                            System.IO.File.Delete(filePath);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // 记录错误但不影响主流程
                Console.WriteLine($"Error deleting image files: {ex.Message}");
            }
        }

        [HttpPost]
        public async Task<IActionResult> MarkClaimed(int itemId)
        {
            string userId = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userId))
                return RedirectToAction("Login");

            try
            {
                // 找到用户要标记为Claimed的帖子
                var collection = _firestore.Collection("Items");
                var query = collection.WhereEqualTo("ItemID", itemId)
                                      .WhereEqualTo("UserID", userId);
                var snapshot = await query.GetSnapshotAsync();

                if (snapshot.Documents.Count == 0)
                {
                    TempData["Error"] = "Post not found or you don't have permission to update it.";
                    return RedirectToAction("MyPost");
                }

                var docRef = snapshot.Documents[0].Reference;

                // 更新状态为CLAIMED
                await docRef.UpdateAsync(new Dictionary<string, object>
        {
            { "IStatus", "CLAIMED" },
            { "ClaimedAt", Timestamp.GetCurrentTimestamp() }
        });

                TempData["Success"] = "Post marked as CLAIMED successfully!";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error marking post as claimed: {ex.Message}";
            }

            return RedirectToAction("MyPost");
        }
    }
}
