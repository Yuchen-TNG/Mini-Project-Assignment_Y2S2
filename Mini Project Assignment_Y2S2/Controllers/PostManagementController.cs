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

        private async Task<Item> MapToItemAsync(DocumentSnapshot doc)
        {
            var item = new Item
            {
                ItemID = doc.ContainsField("ItemID") ? doc.GetValue<int>("ItemID") : 0,
                IType = doc.ContainsField("IType") ? doc.GetValue<string>("IType") : null,
                Idescription = doc.ContainsField("Description") ? doc.GetValue<string>("Description") : null,
                Date = doc.ContainsField("Date") ? doc.GetValue<DateTime>("Date") : DateTime.MinValue,
                Category = doc.ContainsField("Category") ? doc.GetValue<string>("Category") : null,
                IStatus = doc.ContainsField("IStatus") ? doc.GetValue<string>("IStatus") : "PENDING",
                UserID = doc.ContainsField("UserID") ? doc.GetValue<string>("UserID") : null,
                LocationName = doc.ContainsField("LocationName") ? doc.GetValue<string>("LocationName") : null,
                LocationFound = doc.ContainsField("LocationFound") ? doc.GetValue<string>("LocationFound") : null,
                LocationOther = doc.ContainsField("LocationOther") ? doc.GetValue<string>("LocationOther") : null,
                Images = doc.ContainsField("Images") ? doc.GetValue<List<string>>("Images") : new List<string>()
            };

            string resolvedLocation = null;

            // 1️⃣ Try getting LocationName from Locations collection using LocationID
            if (doc.ContainsField("LocationID"))
            {
                string locationId = doc.GetValue<string>("LocationID");

                QuerySnapshot locationSnap = await firestoreDb
                    .Collection("Locations")
                    .WhereEqualTo("LocationID", locationId)
                    .Limit(1)
                    .GetSnapshotAsync();

                if (locationSnap.Count > 0)
                {
                    resolvedLocation = locationSnap.Documents[0].GetValue<string>("LocationName");
                }
            }

            // 2️⃣ If still null, use LocationFound
            if (string.IsNullOrWhiteSpace(resolvedLocation) && !string.IsNullOrWhiteSpace(item.LocationFound))
            {
                resolvedLocation = item.LocationFound;
            }

            // 3️⃣ If still null, use LocationOther
            if (string.IsNullOrWhiteSpace(resolvedLocation) && !string.IsNullOrWhiteSpace(item.LocationOther))
            {
                resolvedLocation = item.LocationOther;
            }

            // Set final LocationName
            item.LocationName = resolvedLocation ?? "Unknown";

            return item;
        }



        [HttpGet]
        [Route("")]
        [Route("Index")]
        public async Task<IActionResult> Index()
        {
            QuerySnapshot snapshot = await firestoreDb.Collection("Items").GetSnapshotAsync();

            var list = new List<Item>();
            foreach (var doc in snapshot.Documents)
            {
                list.Add(await MapToItemAsync(doc));
            }

            var items = list
                .Where(i => i.IStatus.Equals("PENDING", StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(i => i.Date)
                .ToList();

            return View("~/Views/Admin/PostManagement/Index.cshtml", items);
        }

        [HttpGet]
        [Route("ViewDetails/{itemId}")]
        public async Task<IActionResult> ViewDetails(int itemId)
        {
            QuerySnapshot snapshot = await firestoreDb.Collection("Items")
                .WhereEqualTo("ItemID", itemId)
                .Limit(1)
                .GetSnapshotAsync();

            if (snapshot.Count == 0)
            {
                TempData["Error"] = "Item not found";
                return RedirectToAction("Index");
            }

            var item = await MapToItemAsync(snapshot.Documents[0]);
            return View("~/Views/Admin/PostManagement/ViewDetails.cshtml", item);
        }


        [HttpGet]
        [Route("GetItems")]
        public async Task<IActionResult> GetItems(string search = "", string category = "", int page = 1, int pageSize = 10)
        {
            try
            {
                Console.WriteLine($"[v0] GetItems called - search: '{search}', category: '{category}', page: {page}");

                QuerySnapshot snapshot = await firestoreDb.Collection("Items").GetSnapshotAsync();

                var allItems = await Task.WhenAll(snapshot.Documents.Select(MapToItemAsync));

                Console.WriteLine($"[v0] Total items retrieved: {allItems.Length}");

                var items = allItems.Where(i =>
                    i.IStatus != null && i.IStatus.Equals("PENDING", StringComparison.OrdinalIgnoreCase)
                ).ToList();

                Console.WriteLine($"[v0] Pending items: {items.Count}");

                // Apply search filter
                if (!string.IsNullOrEmpty(search))
                {
                    search = search.ToLower();
                    items = items.Where(i =>
                        (i.IType?.ToLower().Contains(search) ?? false) ||
                        (i.Idescription?.ToLower().Contains(search) ?? false) ||
                        (i.LocationFound?.ToLower().Contains(search) ?? false) ||
                        (i.LocationName?.ToLower().Contains(search) ?? false)
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

        // ===================== APPROVE =====================
        [HttpPost]
        [Route("Approve/{itemId}")]
        public async Task<IActionResult> Approve(int itemId)
        {
            QuerySnapshot snapshot = await firestoreDb.Collection("Items")
                .WhereEqualTo("ItemID", itemId)
                .Limit(1)
                .GetSnapshotAsync();

            if (snapshot.Count == 0)
            {
                TempData["Error"] = "Item not found";
                return RedirectToAction("Index");
            }

            await snapshot.Documents[0].Reference.UpdateAsync(new Dictionary<string, object>
            {
                { "IStatus", "Approved" },
                { "ApprovedDate", DateTime.UtcNow }
            });

            TempData["Success"] = "Post approved successfully";
            return RedirectToAction("Index");
        }

        [HttpPost]
        [Route("Reject/{itemId}")]
        public async Task<IActionResult> Reject(int itemId)
        {
            QuerySnapshot snapshot = await firestoreDb.Collection("Items")
                .WhereEqualTo("ItemID", itemId)
                .Limit(1)
                .GetSnapshotAsync();

            if (snapshot.Count == 0)
            {
                TempData["Error"] = "Item not found";
                return RedirectToAction("Index");
            }

            await snapshot.Documents[0].Reference.UpdateAsync(new Dictionary<string, object>
            {
                { "IStatus", "Rejected" },
                { "RejectedDate", DateTime.UtcNow }
            });

            TempData["Success"] = "Post rejected successfully";
            return RedirectToAction("Index");
        }


        [HttpGet]
        [Route("Approved")]
        public async Task<IActionResult> Approved()
        {
            QuerySnapshot snapshot = await firestoreDb.Collection("Items").GetSnapshotAsync();

            var list = new List<Item>();
            foreach (var doc in snapshot.Documents)
            {
                list.Add(await MapToItemAsync(doc));
            }

            var items = list
                .Where(i => i.IStatus.Equals("Approved", StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(i => i.Date)
                .ToList();

            return View("~/Views/Admin/PostManagement/Approved.cshtml", items);
        }


        [HttpGet]
        [Route("Rejected")]
        public async Task<IActionResult> Rejected()
        {
            QuerySnapshot snapshot = await firestoreDb.Collection("Items").GetSnapshotAsync();

            var list = new List<Item>();
            foreach (var doc in snapshot.Documents)
            {
                list.Add(await MapToItemAsync(doc));
            }

            var items = list
                .Where(i => i.IStatus.Equals("Rejected", StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(i => i.Date)
                .ToList();

            return View("~/Views/Admin/PostManagement/Rejected.cshtml", items);
        }
    
    }
}