using Google.Cloud.Firestore;
using Microsoft.AspNetCore.Mvc;
using Mini_Project_Assignment_Y2S2.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Mini_Project_Assignment_Y2S2.Controllers
{
    [Route("PostManagement")]
    public class PostManagementController : Controller
    {
        private readonly string projectId = "miniproject-d280e";
        private readonly FirestoreDb firestoreDb;

        public PostManagementController()
        {
            // JSON FILE
            string path = Path.Combine(Directory.GetCurrentDirectory(), "firebase_config.json");

            // Environment Variable
            Environment.SetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS", path);

            // create connection
            firestoreDb = FirestoreDb.Create(projectId);
        }

        private Item MapToItem(DocumentSnapshot doc)
        {
            return new Item
            {
                ItemID = doc.ContainsField("ItemID") ? Convert.ToInt32(doc.GetValue<object>("ItemID")) : 0,
                IName = doc.ContainsField("IName") ? doc.GetValue<string>("IName") : null,
                IType = doc.ContainsField("IType") ? doc.GetValue<string>("IType") : null,
                Idescription = doc.ContainsField("Description") ? doc.GetValue<string>("Description") :
                              doc.ContainsField("Idescription") ? doc.GetValue<string>("Idescription") : null,
                Date = doc.ContainsField("Date") ? doc.GetValue<DateTime>("Date") : DateTime.MinValue,
                LocationID = doc.ContainsField("LocationID") ? doc.GetValue<string>("LocationID") : null,
                LocationFound = doc.ContainsField("LocationFound") ? doc.GetValue<string>("LocationFound") : null,
                Category = doc.ContainsField("Category") ? doc.GetValue<string>("Category") : null,
                Images = doc.ContainsField("Images") ? doc.GetValue<List<string>>("Images") : new List<string>(),
                IStatus = doc.ContainsField("Status") ? doc.GetValue<string>("Status") :
                         doc.ContainsField("IStatus") ? doc.GetValue<string>("IStatus") : "ACTIVE",
                UserID = doc.ContainsField("UserID") ? doc.GetValue<string>("UserID") : null
            };
        }

        [HttpGet]
        [Route("")]
        [Route("Index")]
        public async Task<IActionResult> Index()
        {
            try
            {
                QuerySnapshot snapshot = await firestoreDb.Collection("Items").GetSnapshotAsync();

                var allItems = snapshot.Documents.Select(MapToItem).ToList();

                var items = allItems.Where(i =>
                    (i.IStatus != null && i.IStatus.Equals("PENDING", StringComparison.OrdinalIgnoreCase))
                ).OrderByDescending(i => i.Date).ToList();

                Console.WriteLine($"[v0] Total items in Firebase: {allItems.Count}");
                Console.WriteLine($"[v0] Pending approval items: {items.Count}");

                return View("~/Views/Admin/PostManagement/Index.cshtml", items);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[v0] Error loading items: {ex.Message}");
                ViewBag.Error = "Failed to load items: " + ex.Message;
                return View("~/Views/Admin/PostManagement/Index.cshtml", new List<Item>());
            }
        }

        [HttpGet]
        [Route("GetItems")]
        public async Task<IActionResult> GetItems(string search = "", string category = "", int page = 1, int pageSize = 10)
        {
            try
            {
                Console.WriteLine($"[v0] GetItems called - search: '{search}', category: '{category}', page: {page}");

                QuerySnapshot snapshot = await firestoreDb.Collection("Items").GetSnapshotAsync();

                var allItems = snapshot.Documents.Select(MapToItem).ToList();

                Console.WriteLine($"[v0] Total items retrieved: {allItems.Count}");

                var items = allItems.Where(i =>
                    i.IStatus != null && i.IStatus.Equals("PENDING", StringComparison.OrdinalIgnoreCase)
                ).ToList();

                Console.WriteLine($"[v0] Pending items: {items.Count}");

                // Apply search filter
                if (!string.IsNullOrEmpty(search))
                {
                    search = search.ToLower();
                    items = items.Where(i =>
                        (i.IName?.ToLower().Contains(search) ?? false) ||
                        (i.IType?.ToLower().Contains(search) ?? false) ||
                        (i.Idescription?.ToLower().Contains(search) ?? false) ||
                        (i.LocationFound?.ToLower().Contains(search) ?? false) ||
                        (i.LocationID?.ToLower().Contains(search) ?? false)
                    ).ToList();

                    Console.WriteLine($"[v0] After search filter: {items.Count}");
                }

                // Apply category filter (LOSTITEM or FOUNDITEM)
                if (!string.IsNullOrEmpty(category))
                {
                    items = items.Where(i =>
                        i.Category != null && i.Category.Equals(category, StringComparison.OrdinalIgnoreCase)
                    ).ToList();

                    Console.WriteLine($"[v0] After category filter: {items.Count}");
                }

                // Order by date
                items = items.OrderByDescending(i => i.Date).ToList();

                // Calculate pagination
                var totalCount = items.Count;
                var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
                var pagedItems = items.Skip((page - 1) * pageSize).Take(pageSize).ToList();

                Console.WriteLine($"[v0] Returning {pagedItems.Count} items for page {page}");

                return Json(new
                {
                    success = true,
                    items = pagedItems,
                    currentPage = page,
                    totalPages = totalPages,
                    totalCount = totalCount,
                    pageSize = pageSize
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[v0] Error in GetItems: {ex.Message}");
                Console.WriteLine($"[v0] Stack trace: {ex.StackTrace}");
                return Json(new { success = false, error = ex.Message });
            }
        }

        [HttpPost]
        [Route("Approve/{itemId}")]
        public async Task<IActionResult> Approve(int itemId)
        {
            try
            {
                Console.WriteLine($"[v0] Approving item: {itemId}");

                QuerySnapshot snapshot = await firestoreDb.Collection("Items")
                    .WhereEqualTo("ItemID", itemId)
                    .Limit(1)
                    .GetSnapshotAsync();

                if (snapshot.Count == 0)
                {
                    TempData["Error"] = "Item not found";
                    return RedirectToAction("Index");
                }

                var docRef = snapshot.Documents[0].Reference;

                await docRef.UpdateAsync(new Dictionary<string, object>
                {
                    { "Status", "Approved" },
                    { "IStatus", "Approved" }
                });

                TempData["Success"] = "Item approved successfully";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[v0] Error approving item: {ex.Message}");
                TempData["Error"] = "Failed to approve item: " + ex.Message;
                return RedirectToAction("Index");
            }
        }

        [HttpPost]
        [Route("Reject/{itemId}")]
        public async Task<IActionResult> Reject(int itemId)
        {
            try
            {
                Console.WriteLine($"[v0] Rejecting item: {itemId}");

                QuerySnapshot snapshot = await firestoreDb.Collection("Items")
                    .WhereEqualTo("ItemID", itemId)
                    .Limit(1)
                    .GetSnapshotAsync();

                if (snapshot.Count == 0)
                {
                    TempData["Error"] = "Item not found";
                    return RedirectToAction("Index");
                }

                var docRef = snapshot.Documents[0].Reference;

                await docRef.UpdateAsync(new Dictionary<string, object>
                {
                    { "Status", "Rejected" },
                    { "IStatus", "Rejected" }
                });

                TempData["Success"] = "Item rejected successfully";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[v0] Error rejecting item: {ex.Message}");
                TempData["Error"] = "Failed to reject item: " + ex.Message;
                return RedirectToAction("Index");
            }
        }
    }
}
