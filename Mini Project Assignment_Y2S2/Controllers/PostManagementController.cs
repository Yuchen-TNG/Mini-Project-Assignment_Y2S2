using Google.Cloud.Firestore;
using Microsoft.AspNetCore.Mvc;
using Mini_Project_Assignment_Y2S2.Filters;
using Mini_Project_Assignment_Y2S2.Models;
using Mini_Project_Assignment_Y2S2.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Mini_Project_Assignment_Y2S2.Controllers
{
    [Area("Admin")]
    [Route("Admin/PostManagement")]
    [AdminAuthorize] // Apply admin authorization filter to all actions
    public class PostManagementController : Controller
    {
        private readonly FirestoreDb _firestore;

        public PostManagementController(FirebaseDB firebaseDB)
        {
            _firestore = firebaseDB.Firestore;
        }

        // =================================================================
        // DISPLAY POST MANAGEMENT PAGE
        // =================================================================

        [HttpGet]
        [Route("")]
        public async Task<IActionResult> Index()
        {
            try
            {
                // Get all posts with "Pending Approval" status
                QuerySnapshot snapshot = await _firestore.Collection("Posts")
                    .WhereEqualTo("Status", "Pending Approval")
                    .OrderByDescending("DatePosted")
                    .GetSnapshotAsync();

                var posts = snapshot.Documents.Select(doc =>
                {
                    var post = doc.ConvertTo<Post>();
                    post.Id = doc.Id;
                    return post;
                }).ToList();

                return View("~/Views/Admin/PostManagement/Index.cshtml", posts);
            }
            catch (Exception ex)
            {
                ViewBag.Error = "Failed to load posts: " + ex.Message;
                return View("~/Views/Admin/PostManagement/Index.cshtml", new List<Post>());
            }
        }

        // =================================================================
        // APPROVE POST
        // =================================================================

        [HttpPost]
        [Route("Approve/{id}")]
        public async Task<IActionResult> Approve(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                TempData["Error"] = "Post ID is required";
                return RedirectToAction("Index");
            }

            try
            {
                var adminId = HttpContext.Session.GetString("UserId");
                var docRef = _firestore.Collection("Posts").Document(id);
                var snapshot = await docRef.GetSnapshotAsync();

                if (!snapshot.Exists)
                {
                    TempData["Error"] = "Post not found";
                    return RedirectToAction("Index");
                }

                // Update post status to Approved
                var updates = new Dictionary<string, object>
                {
                    { "Status", "Approved" },
                    { "DateVerified", DateTime.UtcNow },
                    { "VerifiedByAdminId", adminId ?? "" }
                };

                await docRef.UpdateAsync(updates);

                TempData["Success"] = "Post approved successfully";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Failed to approve post: " + ex.Message;
                return RedirectToAction("Index");
            }
        }

        // =================================================================
        // REJECT POST
        // =================================================================

        [HttpPost]
        [Route("Reject/{id}")]
        public async Task<IActionResult> Reject(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                TempData["Error"] = "Post ID is required";
                return RedirectToAction("Index");
            }

            try
            {
                var adminId = HttpContext.Session.GetString("UserId");
                var docRef = _firestore.Collection("Posts").Document(id);
                var snapshot = await docRef.GetSnapshotAsync();

                if (!snapshot.Exists)
                {
                    TempData["Error"] = "Post not found";
                    return RedirectToAction("Index");
                }

                // Update post status to Rejected
                var updates = new Dictionary<string, object>
                {
                    { "Status", "Rejected" },
                    { "DateVerified", DateTime.UtcNow },
                    { "VerifiedByAdminId", adminId ?? "" }
                };

                await docRef.UpdateAsync(updates);

                TempData["Success"] = "Post rejected successfully";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Failed to reject post: " + ex.Message;
                return RedirectToAction("Index");
            }
        }

        // =================================================================
        // GET ALL POSTS (FOR VIEWING HISTORY)
        // =================================================================

        [HttpGet]
        [Route("History")]
        public async Task<IActionResult> History(string statusFilter = "All", int page = 1)
        {
            try
            {
                int pageSize = 10;
                Query query = _firestore.Collection("Posts");

                // Filter by status if not "All"
                if (!string.IsNullOrEmpty(statusFilter) && statusFilter != "All")
                {
                    query = query.WhereEqualTo("Status", statusFilter);
                }

                QuerySnapshot snapshot = await query
                    .OrderByDescending("DatePosted")
                    .GetSnapshotAsync();

                var posts = snapshot.Documents.Select(doc =>
                {
                    var post = doc.ConvertTo<Post>();
                    post.Id = doc.Id;
                    return post;
                }).ToList();

                int totalCount = posts.Count;
                var pagedPosts = posts
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                ViewBag.CurrentPage = page;
                ViewBag.TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
                ViewBag.StatusFilter = statusFilter;

                return View("~/Views/Admin/PostManagement/History.cshtml", pagedPosts);
            }
            catch (Exception ex)
            {
                ViewBag.Error = "Failed to load post history: " + ex.Message;
                return View("~/Views/Admin/PostManagement/History.cshtml", new List<Post>());
            }
        }
    }
}
