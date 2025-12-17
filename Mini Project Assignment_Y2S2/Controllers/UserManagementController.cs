using Google.Cloud.Firestore;
using Microsoft.AspNetCore.Mvc;
using Mini_Project_Assignment_Y2S2.Filters;
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


    [Area("Admin")]
    [Route("Admin/UserManagement")]
    [AdminAuthorize] // Apply admin authorization filter to protect all routes
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
        // ADD USER (CREATE)
        // =================================================================

        [HttpPost]
        [Route("AddUser")]
        public async Task<IActionResult> AddUser([FromBody] AddUserRequest request)
        {
            try
            {
                Console.WriteLine("[v0] AddUser called");
                Console.WriteLine($"[v0] Request data - Name: {request.Name}, Email: {request.Email}, Role: {request.Role}");

                if (string.IsNullOrWhiteSpace(request.Name) ||
                    string.IsNullOrWhiteSpace(request.Email) ||
                    string.IsNullOrWhiteSpace(request.Password) ||
                    string.IsNullOrWhiteSpace(request.Role))
                {
                    Console.WriteLine("[v0] Validation failed - missing fields");
                    return Json(new { success = false, error = "All fields are required" });
                }

                string roleInput = request.Role.Trim();
                string roleLower = roleInput.ToLower();

                if (roleLower != "admin" && roleLower != "student")
                {
                    Console.WriteLine($"[v0] Invalid role: {request.Role}");
                    return Json(new { success = false, error = "Invalid role. Must be Admin or Student" });
                }

                // Set proper role casing for storage
                string roleValue = roleLower == "admin" ? "Admin" : "Student";
                Console.WriteLine($"[v0] Role set to: {roleValue}");

                QuerySnapshot emailSnapshot = await _firestore.Collection("Users")
                    .WhereEqualTo("Email", request.Email)
                    .Limit(1)
                    .GetSnapshotAsync();

                if (emailSnapshot.Count > 0)
                {
                    Console.WriteLine("[v0] Email already exists");
                    return Json(new { success = false, error = "Email already exists" });
                }

                string userId;
                string staffId = null;

                if (roleValue == "Admin")
                {
                    staffId = GenerateStaffId();
                    userId = staffId; // Use StaffID as the document ID for admins
                    Console.WriteLine($"[v0] Generated StaffID: {staffId}");
                }
                else
                {
                    userId = GenerateUserId();
                    Console.WriteLine($"[v0] Generated UserID: {userId}");
                }

                string passwordHash = PasswordHelper.HashPassword(request.Password);
                Console.WriteLine("[v0] Password hashed successfully");

                var newUser = new Dictionary<string, object>
                {
                    { "Id", userId },
                    { "UserID", userId },
                    { "Name", request.Name },
                    { "Email", request.Email },
                    { "PhoneNumber", request.PhoneNumber ?? "" },
                    { "PasswordHash", passwordHash },
                    { "Role", roleValue }, // Use proper casing
                    { "IsArchived", false }
                };

                if (roleValue == "Admin" && staffId != null)
                {
                    newUser.Add("StaffID", staffId);
                }

                Console.WriteLine($"[v0] Attempting to save user to Firestore with ID: {userId}");
                await _firestore.Collection("Users").Document(userId).SetAsync(newUser);
                Console.WriteLine("[v0] User saved successfully to Firestore");

                return Json(new
                {
                    success = true,
                    message = "User created successfully",
                    user = new
                    {
                        Id = userId,
                        UserID = userId,
                        StaffID = staffId,
                        Name = request.Name,
                        Email = request.Email,
                        Role = roleValue
                    }
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[v0] Error creating user: {ex.Message}");
                Console.WriteLine($"[v0] Stack trace: {ex.StackTrace}");
                return StatusCode(500, new { success = false, error = "Failed to create user", message = ex.Message });
            }
        }

        // =================================================================
        // ARCHIVE USER
        // =================================================================

        [HttpPost]
        [Route("ArchiveUser")]
        public async Task<IActionResult> ArchiveUser(string id)
        {
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

        private string GenerateStaffId()
        {
            lock (_random)
            {
                int randomNumber = _random.Next(1000, 10000); // Generates 1000-9999 (4 digits)
                return $"S{randomNumber}";
            }
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
