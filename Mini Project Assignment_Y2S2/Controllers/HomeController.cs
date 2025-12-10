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
            CollectionReference collection = _firestore.Collection("Items");

            Query query = collection.WhereEqualTo("Category", "LOSTITEM");
            QuerySnapshot snapshot = await query.GetSnapshotAsync();

            var items = snapshot.Documents.Select(doc =>
            {
                var item = new Item
                {
                    LocationID = doc.ContainsField("LocationID") ? doc.GetValue<string?>("LocationID") : null,
                    Images = doc.ContainsField("Image")
    ? new List<string> { doc.GetValue<string>("Image") }
    : null,
                    IType = doc.GetValue<string?>("IType"),
                    IName = doc.GetValue<string?>("IName"),
                    Idescription = doc.GetValue<string?>("Description"),
                    Date = doc.GetValue<DateTime>("Date"),
                    Category = doc.GetValue<string>("Category")
                    // ImageFile 不需要从 Firestore 读取
                };
                return item;
            }).ToList();

            return View(items);
        }

        public async Task<IActionResult> IndexBack()
        {
            CollectionReference collection = _firestore.Collection("Items");

            QuerySnapshot snapshot = await collection.GetSnapshotAsync();

                        var items = snapshot.Documents.Select(doc =>
            {
                var item = new Item
                {
                    LocationID = doc.ContainsField("LocationID") ? doc.GetValue<string?>("LocationID") : null,
                    Images = doc.ContainsField("Image")
    ? new List<string> { doc.GetValue<string>("Image") }
    : null,
                    IType = doc.GetValue<string?>("IType"),
                    IName = doc.GetValue<string?>("IName"),
                    Idescription = doc.GetValue<string?>("Description"),
                    Date = doc.GetValue<DateTime>("Date"),
                    Category = doc.GetValue<string>("Category")
                    // ImageFile 不需要从 Firestore 读取
                };
                return item;
            }).ToList();

            return View("Index",items);
        }
        public async Task<IActionResult> updateCard(string category)
        {
            

    
                // 1. 获取集合
                CollectionReference collections = _firestore.Collection("Items");

                // 2. 条件查询
                Query query = collections.WhereEqualTo("Category", category);

                // 3. 执行查询
                QuerySnapshot snapshot = await query.GetSnapshotAsync();

                // 4. 转换为 Item 列表
                var item = snapshot.Documents.Select(doc =>
                {
                    var items = new Item
                    {
                        LocationID = doc.ContainsField("LocationID") ? doc.GetValue<string?>("LocationID") : null,
                        Images = doc.ContainsField("Image")
    ? new List<string> { doc.GetValue<string>("Image") }
    : null,
                        IType = doc.GetValue<string?>("IType"),
                        IName = doc.GetValue<string?>("IName"),
                        Idescription = doc.GetValue<string?>("Description"),
                        Date = doc.GetValue<DateTime>("Date"),
                        Category = doc.GetValue<string>("Category")
                        // ImageFile 不需要从 Firestore 读取
                    };
                    return items;
                }).ToList();

              
            // 返回 PartialView，即使 items 为空
            return PartialView("_ItemCard", item);
        }

        public async Task<IActionResult> filter(string category,string? startDate, string? endDate)
        {
            CollectionReference collection = _firestore.Collection("Items");

            Query query = collection.WhereEqualTo("Category", category);

            if ((startDate != null)&& DateTime.TryParse(startDate, out var start))
            {
                query = query.WhereGreaterThan("Date", start.ToUniversalTime());
            }

            if ((endDate != null)&&DateTime.TryParse(endDate, out var end))
            {
                var realEndDate = end.Date.AddDays(1);
                query = query.WhereLessThanOrEqualTo("Date", realEndDate.ToUniversalTime());
            }

            QuerySnapshot snapshot = await query.GetSnapshotAsync();

            var items = snapshot.Documents.Select(doc =>
            {
                var item = new Item
                {
                    LocationID = doc.ContainsField("LocationID") ? doc.GetValue<string?>("LocationID") : null,
                    Images = doc.ContainsField("Image")
    ? new List<string> { doc.GetValue<string>("Image") }
    : null,
                    IType = doc.GetValue<string?>("IType"),
                    IName = doc.GetValue<string?>("IName"),
                    Idescription = doc.GetValue<string?>("Description"),
                    Date = doc.GetValue<DateTime>("Date"),
                    Category = doc.GetValue<string>("Category")
                    // ImageFile 不需要从 Firestore 读取
                };
                return item;
            }).ToList();

            return PartialView("_itemCard", items);
        }
        public IActionResult CardDetails(int id)
        {
            return View(id);
        }

        public IActionResult Card(int id)
        {
            return View();
        }





        // GET: CreatePost (显示表单)
        [HttpGet]
        public IActionResult CreatePost(string Category)
        {
            var item = new Item();
            item.Category = Category; // 通过 query string 设置类别
            return View(item);
        }

        [HttpPost]
        public async Task<IActionResult> CreatePost(Item item)
        {
            if (!ModelState.IsValid)
                return View(item);

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

                        using (var stream = new FileStream(filePath, FileMode.Create))
                        {
                            await file.CopyToAsync(stream);
                        }

                        imageUrls.Add("/images/" + fileName);
                    }
                }
            }

            var itemsRef = _firestore.Collection("Items");
            await itemsRef.AddAsync(new
            {
                IName = item.IName,
                IType = item.IType,
                Idescription = item.Idescription,
                LocationID = item.LocationID,
                Images = imageUrls,
                Category = item.Category,
                Date = item.Date.ToUniversalTime(),
                CreatedAt = Timestamp.GetCurrentTimestamp()
            });

            return RedirectToAction("Index");
        }





        // GET: CreateFoundPost (显示表单)
        [HttpGet]
        public IActionResult CreateFoundPost(string Category)
        {
            var item = new Item();
            item.Category = Category; // 设置类别
            return View(item);
        }







        public IActionResult ChoosePostType()
        {
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
