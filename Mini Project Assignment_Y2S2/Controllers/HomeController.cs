    using System.Diagnostics;
    using System.Text.Json;
    using Google.Api;
    using Google.Cloud.Firestore;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.EntityFrameworkCore;
    using Mini_Project_Assignment_Y2S2.Models;
    using Mini_Project_Assignment_Y2S2.Services;
    using static Microsoft.EntityFrameworkCore.DbLoggerCategory;
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

            private Item MapToItem(DocumentSnapshot doc)
            {
                return new Item
                {
                    ItemID = doc.ContainsField("ItemID") ? doc.GetValue<int>("ItemID") : 0,
                    LocationID = doc.ContainsField("LocationID") ? doc.GetValue<string>("LocationID") : null,
                    LocationOther=doc.ContainsField("LocationOther")? doc.GetValue<string>("LocationOther"):null,
                    Images = doc.ContainsField("Images") ? doc.GetValue<List<string>>("Images") : new List<string>(),
                    IType = doc.ContainsField("IType") ? doc.GetValue<string>("IType") : null,
                    Idescription = doc.ContainsField("Description")
                                    ? doc.GetValue<string>("Description")
                                    : doc.ContainsField("Idescription")
                                        ? doc.GetValue<string>("Idescription")
                                        : null,
                    Date = doc.ContainsField("Date") ? doc.GetValue<DateTime>("Date") : DateTime.MinValue,
                    Category = doc.ContainsField("Category") ? doc.GetValue<string>("Category") : null,
                    LocationFound = doc.ContainsField("LocationFound") ? doc.GetValue<string>("LocationFound") : null,
                    UserID = doc.ContainsField("UserID") ? doc.GetValue<string>("UserID") : null // ⭐ 加这行
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

            public IActionResult totalItem()
            {
                var item = HttpContext.Session.GetObject<List<Item>>("FilteredItems");
                var totalCount=item.Count();
                return Ok(new
                {
                    totalCount = totalCount,
                });
            }

            public async Task<IActionResult> Index()
            {
            var query = _firestore.Collection("Items")
                                  .WhereEqualTo("Category", "LOSTITEM")
                                  .WhereEqualTo("IStatus", "APPROVED")
                                  .OrderByDescending("Date");

            var snapshot = await query.GetSnapshotAsync();

            var items = snapshot.Documents
                                .Select(MapToItem)
                                .ToList();

            HttpContext.Session.Remove("PageItems");
                HttpContext.Session.Remove("Location");
                HttpContext.Session.Remove("Page");
                HttpContext.Session.SetObject("FilteredItems", items);
            

                return RedirectToAction("UpdateCard");
            }
        public async Task<IActionResult> IndexPagingReset(int page, int? size)
        {
            var finalsize = 0;
            if (size != null)
            {
                HttpContext.Session.SetObject("sizee", size);
                finalsize = HttpContext.Session.GetObject<int>("sizee");
            }
            else
            {
                finalsize = HttpContext.Session.GetObject<int>("sizee");
            }

            var category = HttpContext.Session.GetObject<string>("ResetCategory") ?? "LOSTITEM";
            var query = _firestore.Collection("Items")
                      .WhereEqualTo("Category", category)
                      .WhereEqualTo("IStatus", "APPROVED")
                      .OrderByDescending("Date");

            var snapshot = await query.GetSnapshotAsync();

            var items = snapshot.Documents
                                .Select(MapToItem)
                                .ToList();

            var locations = HttpContext.Session.GetObject<List<Location>>("Location");

            var skip = (page - 1) * finalsize;
            HttpContext.Session.SetObject("Page", page);
            HttpContext.Session.Remove("ResetCategory");

            var itemCards = items
                .Skip(skip)
                .Take(finalsize)
                .Select(item =>
                {
                    var loc = (locations ?? new List<Location>()).FirstOrDefault(l => l.LocationID == item.LocationID);
                    return new ItemCardViewModel
                    {
                        ItemID = item.ItemID,
                        IType = item.IType,
                        Date = item.Date,
                        Images = item.Images,
                        LocationName = loc?.LocationName ?? "Unknown"
                    };
                }).ToList();

            return PartialView("_ItemCard", itemCards);
        }


        public async Task<IActionResult> UpdateCard()
            {
            var itemsFromSession =HttpContext.Session.GetObject<List<Item>>("PageItems")?? HttpContext.Session.GetObject<List<Item>>("FilteredItems");


            var query = _firestore.Collection("Items")
                      .WhereEqualTo("IStatus", "APPROVED")
                      .OrderByDescending("Date");

            var snapshot = await query.GetSnapshotAsync();

            var items = snapshot.Documents
                                .Select(MapToItem)
                                .ToList();


            // ===== 日期逻辑（原样保留）=====
            var dates = items
                    .Where(i => i.Date != DateTime.MinValue)
                    .Select(i => i.Date)
                    .ToList();

                DateTime minDate = dates.Any() ? dates.Min() : DateTime.Today;
                DateTime maxDate = dates.Any() ? dates.Max() : DateTime.Today;

                ViewData["MinDate"] = minDate.ToString("yyyy-MM-dd");
                ViewData["MaxDate"] = maxDate.ToString("yyyy-MM-dd");

                // ===== 读取 Location =====
                var snapshot2 = await _firestore.Collection("Location").GetSnapshotAsync();
                var locations = snapshot2.Documents.Select(MapToLocation).ToList();

                HttpContext.Session.SetObject("Location", locations);

                // ===== ⭐ 关键：手动 Join Item + Location =====
                var itemCards = itemsFromSession.Select(item =>
                {
                    var loc = (locations ?? new List<Location>()).FirstOrDefault(l => l.LocationID == item.LocationID);

                    return new ItemCardViewModel
                    {
                        ItemID = item.ItemID,
                        IType = item.IType,
                        Date = item.Date,
                        Images = item.Images,
                        LocationName = loc.LocationName
                    };
                }).ToList();

                // ===== 返回给 Index 的 ViewModel =====
                var vm = new LocationItemsViewModel
                {
                    Locations = locations,
                    Items = itemsFromSession   // 其他地方还可能用
                };

                // ⭐ 把 itemCards 另外丢进 ViewData / Session / 新字段
                ViewData["ItemCards"] = itemCards;

                return View("Index", vm);
            }
            public IActionResult UpdatePartialCard()
            {
                var items = HttpContext.Session.GetObject<List<Item>>("FilteredItems");
                var locations = HttpContext.Session.GetObject<List<Location>>("Location");

                var itemCards = items.Select(item =>
                {
                    var loc = (locations ?? new List<Location>()).FirstOrDefault(l => l.LocationID == item.LocationID);
                    return new ItemCardViewModel
                    {
                        ItemID = item.ItemID,
                        IType = item.IType,
                        Date = item.Date,
                        Images = item.Images,
                        LocationName = loc?.LocationName ?? "Unknown"
                    };
                }).ToList();

                return PartialView("_ItemCard", itemCards);
            }

            public async Task<IActionResult> filter(string category, string? startDate, string? endDate,string? locationID)
            {


                CollectionReference collection = _firestore.Collection("Items");
                Google.Cloud.Firestore.Query query = collection.WhereEqualTo("Category", category);

                if (!string.IsNullOrEmpty(startDate) && DateTime.TryParse(startDate, out var start)&&startDate != "null")
                    query = query.WhereGreaterThan("Date", start.ToUniversalTime());

                if (!string.IsNullOrEmpty(endDate) && DateTime.TryParse(endDate, out var end) &&endDate != "null")
                    query = query.WhereLessThanOrEqualTo("Date", end.Date.AddDays(1).ToUniversalTime());

                if (!string.IsNullOrEmpty(locationID)&& locationID!="null")
                    query = query.WhereEqualTo("LocationID", locationID);

            query = query.WhereEqualTo("IStatus", "APPROVED");
            query = query.OrderByDescending("Date");

            QuerySnapshot snapshot = await query.GetSnapshotAsync();

                var items = snapshot.Documents.Select(MapToItem).ToList();

                HttpContext.Session.SetObject("FilteredItems", items);
                HttpContext.Session.SetObject("ResetCategory", category);
                return RedirectToAction("IndexPaging",1);
            }

            public IActionResult IndexPaging(int page, int? size)
            {
                var finalsize = 0;
                if (size != null)
                {
                    HttpContext.Session.SetObject("sizee", size);
                    finalsize = HttpContext.Session.GetObject<int>("sizee");
                }
                else
                {
                    finalsize = HttpContext.Session.GetObject<int>("sizee");
                }

                var items = HttpContext.Session.GetObject<List<Item>>("FilteredItems");
                var locations = HttpContext.Session.GetObject<List<Location>>("Location");

                var skip = (page - 1) * finalsize;
                HttpContext.Session.SetObject("Page", page);

                var itemCards = items
                    .Skip(skip)
                    .Take(finalsize)
                    .Select(item =>
                    {
                        var loc = (locations ?? new List<Location>()).FirstOrDefault(l => l.LocationID == item.LocationID);
                        return new ItemCardViewModel
                        {
                            ItemID = item.ItemID,
                            IType = item.IType,
                            Date = item.Date,
                            Images = item.Images,
                            LocationName = loc?.LocationName ?? "Unknown"
                        };
                    }).ToList();
            
                return PartialView("_ItemCard", itemCards);
            }


            public async Task<IActionResult> CardDetails(int itemId)
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
                    Location=location
                };
                return View(viewModel);
            }



            public IActionResult Card(string id) => View();
            [HttpGet]
            public async Task<IActionResult> CreatePost(string Category)
            {
                var item = new Item { Category = Category };

            ViewBag.Category = Category;

            var locationSnap = await _firestore.Collection("Location").GetSnapshotAsync();

                var userId = HttpContext.Session.GetString("UserId");
                ViewBag.UserId = userId; // ⭐ 加这行

                // 手动映射
                var locations = locationSnap.Documents.Select(doc => new Location
                {
                    LocationID = doc.ContainsField("LocationID") ? doc.GetValue<string>("LocationID") : null,
                    LocationName = doc.ContainsField("LocationName") ? doc.GetValue<string>("LocationName") : null
                }).ToList();

                ViewBag.Locations = locations;

                return View(item);
            }

        [HttpPost]
        public async Task<IActionResult> CreatePost(Item item, string? OtherLocation)
        {
            var userId = HttpContext.Session.GetString("UserId");

            // ==========================================
            // 关键修复：移除LocationName的验证
            // ==========================================
            ModelState.Remove("LocationName");

            // ⭐ 条件验证1：只有 FOUNDITEM 需要 LocationFound
            if (item.Category == "FOUNDITEM" && string.IsNullOrWhiteSpace(item.LocationFound))
            {
                ModelState.AddModelError(nameof(item.LocationFound),
                    "Where you found is required for found items");
            }

            // ⭐ 条件验证2：图片文件验证
            if (item.ImageFiles == null || !item.ImageFiles.Any())
            {
                ModelState.AddModelError(nameof(item.ImageFiles), "At least one image is required");
            }
            else
            {
                // ⭐ 新增：最多 10 张图片
                if (item.ImageFiles.Count > 10)
                {
                    ModelState.AddModelError(
                        nameof(item.ImageFiles),
                        "You can upload a maximum of 10 images"
                    );
                }

                var maxSize = 5 * 1024 * 1024; // 5MB
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".webp" };

                foreach (var file in item.ImageFiles)
                {
                    if (file.Length > maxSize)
                    {
                        ModelState.AddModelError(
                            nameof(item.ImageFiles),
                            $"File {file.FileName} exceeds 5MB limit"
                        );
                        break;
                    }

                    var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
                    if (!allowedExtensions.Contains(extension))
                    {
                        ModelState.AddModelError(
                            nameof(item.ImageFiles),
                            $"File {file.FileName} has invalid file type. Allowed: {string.Join(", ", allowedExtensions)}"
                        );
                        break;
                    }
                }
            }


            if (!ModelState.IsValid)
            {
                var locationSnap = await _firestore.Collection("Location").GetSnapshotAsync();
                ViewBag.Locations = locationSnap.Documents.Select(doc => new Location
                {
                    LocationID = doc.ContainsField("LocationID") ? doc.GetValue<string>("LocationID") : null,
                    LocationName = doc.ContainsField("LocationName") ? doc.GetValue<string>("LocationName") : null
                }).ToList();

                ViewBag.UserId = userId;
                ViewBag.Category = item.Category; // 重要！

                return View(item);
            }

            // 如果用户选择 Other → 用输入框的值
            if (item.LocationID == "Other")
            {
                item.LocationID = OtherLocation;
            }

            // 处理图片上传
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

            // ItemID 自增
            var itemsRef = _firestore.Collection("Items");
            var counterRef = _firestore.Collection("Counters").Document("ItemCounter");
            int newItemID = 0;

            await _firestore.RunTransactionAsync(async transaction =>
            {
                var snapshot = await transaction.GetSnapshotAsync(counterRef);
                newItemID = snapshot.ContainsField("NextItemID") ? snapshot.GetValue<int>("NextItemID") : 1;

                transaction.Set(counterRef, new Dictionary<string, object>
        {
            { "NextItemID", newItemID + 1 }
        }, SetOptions.MergeAll);
            });

            item.ItemID = newItemID;

            // 保存到 Firestore
            DocumentReference docRef = itemsRef.Document();
            await docRef.SetAsync(new
            {
                ItemID = item.ItemID,
                IType = item.IType,
                Description = item.Idescription,
                LocationID = item.LocationID,
                LocationOther = item.LocationOther,
                Images = imageUrls,
                Category = item.Category,
                Date = item.Date.ToUniversalTime(),
                UserID = userId,
                CreatedAt = Timestamp.GetCurrentTimestamp(),
                IStatus = "PENDING",
                LocationFound = item.LocationFound

            });

            return RedirectToAction("Index");
        }

        [HttpGet]
            public async Task<IActionResult> CreateFoundPost(string Category)
            {
                var item = new Item { Category = Category };

                // 读取 Location
                var locationSnap = await _firestore.Collection("Location").GetSnapshotAsync();

                var locations = locationSnap.Documents.Select(doc => new Location
                {
                    LocationID = doc.ContainsField("LocationID") ? doc.GetValue<string>("LocationID") : null,
                    LocationName = doc.ContainsField("LocationName") ? doc.GetValue<string>("LocationName") : null
                }).ToList();

                ViewBag.Locations = locations;

                return View(item);
            }


            public IActionResult ChoosePostType()
        {
            var userId = HttpContext.Session.GetString("UserId");

            if (string.IsNullOrEmpty(userId))
            {
                TempData["Error"] = "Please login before you create a post";
                return RedirectToAction("Index", "Home");
            }
            return View();

        }

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
