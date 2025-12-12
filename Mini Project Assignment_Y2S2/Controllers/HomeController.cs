using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Google.Cloud.Firestore;
using Mini_Project_Assignment_Y2S2.Models;
using Mini_Project_Assignment_Y2S2.Services;

namespace Mini_Project_Assignment_Y2S2.Controllers
{
    public class HomeController : Controller
    {
        private readonly FirestoreDb _firestore;
        private readonly FirebaseDB _firebaseDB;

        public HomeController(FirebaseDB firebaseDB)
        {
            _firebaseDB = firebaseDB;
            _firestore = firebaseDB.Firestore;
        }

        public async Task<IActionResult> Index()
        {
            HttpContext.Session.SetString("CurrentCategory", "LOSTITEM");
            CollectionReference collection = _firestore.Collection("Items");
            QuerySnapshot snapshot = await collection.GetSnapshotAsync();

            var items = snapshot.Documents.Select(doc => new Item
            {
                ItemID = doc.ContainsField("ItemID") ? doc.GetValue<int>("ItemID") : 0,
                LocationID = doc.ContainsField("LocationID") ? doc.GetValue<string>("LocationID") : null,
                Images = doc.ContainsField("Images") ? doc.GetValue<List<string>>("Images") : new List<string>(),
                IType = doc.ContainsField("IType") ? doc.GetValue<string>("IType") : null,
                IName = doc.ContainsField("IName") ? doc.GetValue<string>("IName") : null,
                Idescription = doc.ContainsField("Description") ? doc.GetValue<string>("Description") :
                               doc.ContainsField("Idescription") ? doc.GetValue<string>("Idescription") : null,
                Date = doc.ContainsField("Date") ? doc.GetValue<DateTime>("Date") : DateTime.MinValue,
                Category = doc.ContainsField("Category") ? doc.GetValue<string>("Category") : null
            }).ToList();


            var dates = items.Where(i => i.Date != DateTime.MinValue).Select(i => i.Date).ToList();
            DateTime minDate = dates.Any() ? dates.Min() : DateTime.Today;
            DateTime maxDate = dates.Any() ? dates.Max() : DateTime.Today;
            ViewData["MinDate"] = minDate.ToString("yyyy-MM-dd");
            ViewData["MaxDate"] = maxDate.ToString("yyyy-MM-dd");

            return View(items);
        }

        public async Task<IActionResult> IndexBack(string category)
        {

            CollectionReference collection = _firestore.Collection("Items");
            Query query = collection.WhereEqualTo("Category", category);
            QuerySnapshot snapshot = await query.GetSnapshotAsync();

            var items = snapshot.Documents.Select(doc => new Item
            {
                ItemID = doc.ContainsField("ItemID") ? doc.GetValue<int>("ItemID") : 0,
                LocationID = doc.ContainsField("LocationID") ? doc.GetValue<string>("LocationID") : null,
                Images = doc.ContainsField("Images") ? doc.GetValue<List<string>>("Images") : new List<string>(),
                IType = doc.ContainsField("IType") ? doc.GetValue<string>("IType") : null,
                IName = doc.ContainsField("IName") ? doc.GetValue<string>("IName") : null,
                Idescription = doc.ContainsField("Description") ? doc.GetValue<string>("Description") :
                               doc.ContainsField("Idescription") ? doc.GetValue<string>("Idescription") : null,
                Date = doc.ContainsField("Date") ? doc.GetValue<DateTime>("Date") : DateTime.MinValue,
                Category = doc.ContainsField("Category") ? doc.GetValue<string>("Category") : null
            }).ToList();

            var dates = items.Where(i => i.Date != DateTime.MinValue).Select(i => i.Date).ToList();
            DateTime minDate = dates.Any() ? dates.Min() : DateTime.Today;
            DateTime maxDate = dates.Any() ? dates.Max() : DateTime.Today;
            ViewData["MinDate"] = minDate.ToString("yyyy-MM-dd");
            ViewData["MaxDate"] = maxDate.ToString("yyyy-MM-dd");
            return View("Index", items);
        }

        public async Task<IActionResult> updateCard(string category)
        {
            HttpContext.Session.SetString("CurrentCategory", category);

            CollectionReference collection = _firestore.Collection("Items");
            Query query = collection.WhereEqualTo("Category", category);
            QuerySnapshot snapshot = await query.GetSnapshotAsync();

            var items = snapshot.Documents.Select(doc => new Item
            {
                ItemID = doc.ContainsField("ItemID") ? doc.GetValue<int>("ItemID") : 0,
                LocationID = doc.ContainsField("LocationID") ? doc.GetValue<string>("LocationID") : null,
                Images = doc.ContainsField("Images") ? doc.GetValue<List<string>>("Images") : new List<string>(),
                IType = doc.ContainsField("IType") ? doc.GetValue<string>("IType") : null,
                IName = doc.ContainsField("IName") ? doc.GetValue<string>("IName") : null,
                Idescription = doc.ContainsField("Description") ? doc.GetValue<string>("Description") :
                               doc.ContainsField("Idescription") ? doc.GetValue<string>("Idescription") : null,
                Date = doc.ContainsField("Date") ? doc.GetValue<DateTime>("Date") : DateTime.MinValue,
                Category = doc.ContainsField("Category") ? doc.GetValue<string>("Category") : null
            }).ToList();

            return PartialView("_ItemCard", items);
        }

        public async Task<IActionResult> filter(string category, string? startDate, string? endDate,string? locationID)
        {
            CollectionReference collection = _firestore.Collection("Items");
            Query query = collection.WhereEqualTo("Category", category);

            if (startDate != null && DateTime.TryParse(startDate, out var start))
                query = query.WhereGreaterThan("Date", start.ToUniversalTime());

            if (endDate != null && DateTime.TryParse(endDate, out var end))
                query = query.WhereLessThanOrEqualTo("Date", end.Date.AddDays(1).ToUniversalTime());

            if (locationID != null)
                query = query.WhereEqualTo("LocationID", locationID);

            QuerySnapshot snapshot = await query.GetSnapshotAsync();

            var items = snapshot.Documents.Select(doc => new Item
            {
                ItemID = doc.ContainsField("ItemID") ? doc.GetValue<int>("ItemID") : 0,
                LocationID = doc.ContainsField("LocationID") ? doc.GetValue<string>("LocationID") : null,
                Images = doc.ContainsField("Images") ? doc.GetValue<List<string>>("Images") : new List<string>(),
                IType = doc.ContainsField("IType") ? doc.GetValue<string>("IType") : null,
                IName = doc.ContainsField("IName") ? doc.GetValue<string>("IName") : null,
                Idescription = doc.ContainsField("Description") ? doc.GetValue<string>("Description") :
                               doc.ContainsField("Idescription") ? doc.GetValue<string>("Idescription") : null,
                Date = doc.ContainsField("Date") ? doc.GetValue<DateTime>("Date") : DateTime.MinValue,
                Category = doc.ContainsField("Category") ? doc.GetValue<string>("Category") : null
            }).ToList();

            return PartialView("_ItemCard", items);
        }

        public async Task<IActionResult> CardDetails(int itemId)
        {

            string userId = HttpContext.Session.GetString("UserId");
            if (userId == null)
            {
               TempData["Error"] = "You haven't sign in yet";
                return RedirectToAction("Index");
            }
            User user = null;
            var collectionUser = _firestore.Collection("Users");
            Query queryUser = collectionUser.WhereEqualTo("UserID", "8840882");
            QuerySnapshot snapshotsUser = await queryUser.GetSnapshotAsync();

            if (snapshotsUser.Documents == null)
            {
                TempData["Error"] = "Can't find your UserID, please register again";
                return RedirectToAction("Index");
            }

            var userDoc = snapshotsUser.Documents[0];
            user = new User
            {
                Name = userDoc.GetValue<string>("Name"),
                PhoneNumber = userDoc.GetValue<string>("PhoneNumber"),
                Email = userDoc.GetValue<string>("Email"),
                UserID = userDoc.GetValue<string>("UserID")
            };

            var collection = _firestore.Collection("Items");
            Query query = collection.WhereEqualTo("ItemID", itemId);
            QuerySnapshot snapshot = await query.GetSnapshotAsync();

            if (snapshot.Documents.Count == 0)
                return NotFound();

            var doc = snapshot.Documents[0];

            var item = new Item
            {
                ItemID = doc.ContainsField("ItemID") ? doc.GetValue<int>("ItemID") : 0,
                LocationID = doc.ContainsField("LocationID") ? doc.GetValue<string>("LocationID") : null,
                Images = doc.ContainsField("Images") ? doc.GetValue<List<string>>("Images") : new List<string>(),
                IType = doc.ContainsField("IType") ? doc.GetValue<string>("IType") : null,
                IName = doc.ContainsField("IName") ? doc.GetValue<string>("IName") : null,
                Idescription = doc.ContainsField("Description") ? doc.GetValue<string>("Description") :
                               doc.ContainsField("Idescription") ? doc.GetValue<string>("Idescription") : null,
                Date = doc.ContainsField("Date") ? doc.GetValue<DateTime>("Date") : DateTime.MinValue,
                Category = doc.ContainsField("Category") ? doc.GetValue<string>("Category") : null
            };

            var viewModel = new CardDetailsViewModel
            {
                Item = item,
                User = user
            };
            return View(viewModel);
        }



        public IActionResult Card(string id) => View();

        [HttpGet]
        public IActionResult CreatePost(string Category)
        {
            var item = new Item { Category = Category };
            return View(item);
        }

        [HttpPost]
        public async Task<IActionResult> CreatePost(Item item)
        {
            if (!ModelState.IsValid) return View(item);

            var imageUrls = new List<string>();
            var imagesPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images");
            Directory.CreateDirectory(imagesPath);

            if (item.ImageFiles != null)
            {
                foreach (var file in item.ImageFiles)
                {
                    if (file.Length > 0)
                    {
                        var fileName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
                        var filePath = Path.Combine(imagesPath, fileName);
                        using var stream = new FileStream(filePath, FileMode.Create);
                        await file.CopyToAsync(stream);
                        imageUrls.Add("/images/" + fileName);
                    }
                }
            }

            var itemsRef = _firestore.Collection("Items");
            var counterRef = _firestore.Collection("Counters").Document("ItemCounter");
            int newItemID = 0;

            await _firestore.RunTransactionAsync(async transaction =>
            {
                var snapshot = await transaction.GetSnapshotAsync(counterRef);

                // 如果文档不存在，初始化为 1
                newItemID = snapshot.ContainsField("NextItemID") ? snapshot.GetValue<int>("NextItemID") : 1;

                // 使用 Set + MergeAll，无论文档是否存在都可以
                transaction.Set(counterRef, new Dictionary<string, object>
    {
        { "NextItemID", newItemID + 1 }
    }, SetOptions.MergeAll);
            });


            item.ItemID = newItemID;

            // 🔹 保存 Item
            DocumentReference docRef = itemsRef.Document(); // DocID 仍可自动生成
            await docRef.SetAsync(new
            {
                ItemID = item.ItemID,
                IName = item.IName,
                IType = item.IType,
                Description = item.Idescription,
                LocationID = item.LocationID,
                Images = imageUrls,
                Category = item.Category,
                Date = item.Date.ToUniversalTime(),
                CreatedAt = Timestamp.GetCurrentTimestamp()
            });

            return RedirectToAction("Index");
        }


        [HttpGet]
        public IActionResult CreateFoundPost(string Category)
        {
            var item = new Item { Category = Category };
            return View(item);
        }

        public IActionResult ChoosePostType() => View();

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel
            {
                RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier
            });
        }
    }
}
