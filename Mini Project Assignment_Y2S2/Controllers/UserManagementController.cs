using Google.Cloud.Firestore;
using Microsoft.AspNetCore.Mvc;
using Mini_Project_Assignment_Y2S2.Services;
using Mini_Project_Assignment_Y2S2.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Mini_Project_Assignment_Y2S2.Controllers
{
    public class AddUserRequest
    {
        public string Name { get; set; }
        public string Email { get; set; }
        public string PhoneNumber { get; set; }
        public string Password { get; set; }
        public string Role { get; set; }
    }

    public class UpdateUserRequest
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public string PhoneNumber { get; set; }
        public string Role { get; set; }
    }

    [Area("Admin")]
    [Route("Admin/UserManagement")]
    public class UserManagementController : Controller
    {
        private readonly FirestoreDb _firestore;
        private static readonly Random _random = new Random();

        public UserManagementController(FirebaseDB firebaseDB)
        {
            _firestore = firebaseDB.Firestore;
        }

        // =================================================================
        // MVC VIEW ACTION
        // =================================================================

        [HttpGet]
        [Route("")]
        public IActionResult Index()
        {
            return View("~/Views/Admin/UserManagement/Index.cshtml");
        }

        // =================================================================
        // AJAX TABLE DATA
        // =================================================================

        [HttpGet]
        [Route("GetTableData")]
        public async Task<IActionResult> GetTableData(string search, string roleFilter, int page = 1)
        {
            try
            {
                int pageSize = 10;
                QuerySnapshot snapshot = await _firestore.Collection("Users").GetSnapshotAsync();

                var users = snapshot.Documents.Select(d =>
                {
                    var data = d.ToDictionary();
                    return new UserManagementViewModel
                    {
                        Id = d.Id,
                        UserID = GetStringValue(data, "Id"),
                        Name = GetStringValue(data, "Name"),
                        Email = GetStringValue(data, "Email"),
                        PhoneNumber = GetStringValue(data, "PhoneNumber"),
                        Role = GetStringValue(data, "Role"),
                        IsArchived = GetBooleanValue(data, "IsArchived")
                    };
                }).AsQueryable();

                if (!string.IsNullOrEmpty(roleFilter) && roleFilter != "All")
                {
                    users = users.Where(u => u.Role.Equals(roleFilter, StringComparison.OrdinalIgnoreCase));
                }

                if (!string.IsNullOrEmpty(search))
                {
                    search = search.ToLower();
                    users = users.Where(u =>
                        u.Name.ToLower().Contains(search) ||
                        u.Email.ToLower().Contains(search) ||
                        u.UserID.Contains(search));
                }

                int totalCount = users.Count();
                var pagedUsers = users
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                return Json(new
                {
                    users = pagedUsers,
                    currentPage = page,
                    totalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    error = "Internal Server Error",
                    message = ex.Message
                });
            }
        }

        // =================================================================
        // GET USER JSON (FOR EDIT MODAL)
        // =================================================================

        [HttpGet]
        [Route("GetUserJson/{id}")]
        public async Task<IActionResult> GetUserJson(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound(new { error = "User ID is required" });
            }

            try
            {
                var docRef = _firestore.Collection("Users").Document(id);
                var snapshot = await docRef.GetSnapshotAsync();

                if (!snapshot.Exists)
                {
                    return NotFound(new { error = "User not found" });
                }

                var data = snapshot.ToDictionary();

                var model = new UserManagementViewModel
                {
                    Id = id,
                    UserID = GetStringValue(data, "Id"),
                    Name = GetStringValue(data, "Name"),
                    Email = GetStringValue(data, "Email"),
                    PhoneNumber = GetStringValue(data, "PhoneNumber"),
                    Role = GetStringValue(data, "Role")
                };

                return Json(model);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Failed to load user data", message = ex.Message });
            }
        }

        // =================================================================
        // ADD USER (CREATE)
        // =================================================================

        [HttpPost]
        [Route("AddUser")]
        public async Task<IActionResult> AddUser([FromBody] AddUserRequest request)
        {
            var userRole = HttpContext.Session.GetString("UserRole");
            if (userRole?.ToLower() != "staff")
            {
                return Json(new { success = false, error = "Only staff can create users" });
            }

            try
            {
                if (string.IsNullOrWhiteSpace(request.Name) ||
                    string.IsNullOrWhiteSpace(request.Email) ||
                    string.IsNullOrWhiteSpace(request.Password) ||
                    string.IsNullOrWhiteSpace(request.Role))
                {
                    return Json(new { success = false, error = "All fields are required" });
                }

                string roleLower = request.Role.ToLower();
                if (roleLower != "staff" && roleLower != "student")
                {
                    return Json(new { success = false, error = "Invalid role. Must be Staff or Student" });
                }

                QuerySnapshot emailSnapshot = await _firestore.Collection("Users")
                    .WhereEqualTo("Email", request.Email)
                    .Limit(1)
                    .GetSnapshotAsync();

                if (emailSnapshot.Count > 0)
                {
                    return Json(new { success = false, error = "Email already exists" });
                }

                string userId = GenerateUserId();
                string passwordHash = PasswordHelper.HashPassword(request.Password);

                var newUser = new Dictionary<string, object>
                {
                    { "Id", userId },
                    { "UserID", userId },
                    { "Name", request.Name },
                    { "Email", request.Email },
                    { "PhoneNumber", request.PhoneNumber ?? "" },
                    { "PasswordHash", passwordHash },
                    { "Role", roleLower },
                    { "IsArchived", false }
                };

                await _firestore.Collection("Users").Document(userId).SetAsync(newUser);

                return Json(new
                {
                    success = true,
                    message = "User created successfully",
                    user = new
                    {
                        Id = userId,
                        UserID = userId,
                        Name = request.Name,
                        Email = request.Email,
                        Role = roleLower
                    }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, error = "Failed to create user", message = ex.Message });
            }
        }

        // =================================================================
        // UPDATE USER (EDIT)
        // =================================================================

        [HttpPost]
        [Route("UpdateUser")]
        public async Task<IActionResult> UpdateUser([FromBody] UpdateUserRequest request)
        {
            var userRole = HttpContext.Session.GetString("UserRole");
            if (userRole?.ToLower() != "staff")
            {
                return Json(new { success = false, error = "Only staff can edit users" });
            }

            if (string.IsNullOrEmpty(request.Id))
            {
                return Json(new { success = false, error = "User ID is required" });
            }

            try
            {
                string roleLower = request.Role?.ToLower() ?? "student";
                if (roleLower != "staff" && roleLower != "student")
                {
                    return Json(new { success = false, error = "Invalid role. Must be Staff or Student" });
                }

                var docRef = _firestore.Collection("Users").Document(request.Id);
                var snapshot = await docRef.GetSnapshotAsync();

                if (!snapshot.Exists)
                {
                    return Json(new { success = false, error = "User not found" });
                }

                var updates = new Dictionary<string, object>
                {
                    { "Name", request.Name ?? "" },
                    { "Email", request.Email ?? "" },
                    { "PhoneNumber", request.PhoneNumber ?? "" },
                    { "Role", roleLower }
                };

                await docRef.UpdateAsync(updates);

                return Json(new { success = true, message = "User updated successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, error = "Failed to update user", message = ex.Message });
            }
        }

        // =================================================================
        // ARCHIVE USER
        // =================================================================

        [HttpPost]
        [Route("ArchiveUser")]
        public async Task<IActionResult> ArchiveUser(string id)
        {
            var userRole = HttpContext.Session.GetString("UserRole");
            if (userRole?.ToLower() != "staff")
            {
                return Json(new { success = false, message = "Only staff can archive users" });
            }

            if (string.IsNullOrEmpty(id))
            {
                return Json(new { success = false, message = "User ID is required" });
            }

            try
            {
                var docRef = _firestore.Collection("Users").Document(id);
                var snapshot = await docRef.GetSnapshotAsync();

                if (!snapshot.Exists)
                {
                    return Json(new { success = false, message = "User not found" });
                }

                await docRef.UpdateAsync("IsArchived", true);

                return Json(new { success = true, message = "User archived successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Failed to archive user", error = ex.Message });
            }
        }

        // =================================================================
        // UNARCHIVE USER
        // =================================================================

        [HttpPost]
        [Route("UnarchiveUser")]
        public async Task<IActionResult> UnarchiveUser(string id)
        {
            var userRole = HttpContext.Session.GetString("UserRole");
            if (userRole?.ToLower() != "staff")
            {
                return Json(new { success = false, message = "Only staff can reactivate users" });
            }

            if (string.IsNullOrEmpty(id))
            {
                return Json(new { success = false, message = "User ID is required" });
            }

            try
            {
                var docRef = _firestore.Collection("Users").Document(id);
                var snapshot = await docRef.GetSnapshotAsync();

                if (!snapshot.Exists)
                {
                    return Json(new { success = false, message = "User not found" });
                }

                await docRef.UpdateAsync("IsArchived", false);

                return Json(new { success = true, message = "User reactivated successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Failed to reactivate user", error = ex.Message });
            }
        }

        // =================================================================
        // HELPER METHODS
        // =================================================================

        private string GetStringValue(IReadOnlyDictionary<string, object> data, string key)
        {
            if (data.TryGetValue(key, out object value) && value != null)
            {
                return value.ToString();
            }
            return string.Empty;
        }

        private bool GetBooleanValue(IReadOnlyDictionary<string, object> data, string key)
        {
            if (data.TryGetValue(key, out object value) && value is bool boolValue)
            {
                return boolValue;
            }
            return false;
        }

        private string GenerateUserId()
        {
            lock (_random)
            {
                return _random.Next(1000000, 10000000).ToString();
            }
        }
    }
}