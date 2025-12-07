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

        public IActionResult Index()
        {
            return View();
        }

        public async Task<IActionResult> filter(string category,DateTime? startDate,DateTime? endDate)
        {
            CollectionReference collection = _firestore.Collection("Items");

            Query query = collection.WhereEqualTo("Category", category);

            if (startDate != null)
            {
                query = query.WhereGreaterThanOrEqualTo("Date", startDate.Value.ToUniversalTime());
            }

            if (endDate != null)
            {
                query = query.WhereLessThanOrEqualTo("Date", endDate.Value.ToUniversalTime());
            }

            QuerySnapshot snapshot = await query.GetSnapshotAsync();

                var item = snapshot.Documents.Select(d => d.ConvertTo<Item>()).ToList();

                return PartialView("_itemCard", item);
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

        // POST: CreatePost (提交表单)
        [HttpPost]
        public async Task<IActionResult> CreatePost(Item item)
        {
            if (!ModelState.IsValid)
                return View(item);

            // 处理图片上传
            if (item.ImageFile != null && item.ImageFile.Length > 0)
            {
                // 确保目录存在
                var imagesPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images");
                if (!Directory.Exists(imagesPath))
                {
                    Directory.CreateDirectory(imagesPath);
                }

                // 使用安全文件名，避免中文或特殊字符问题
                var fileName = Path.GetFileName(item.ImageFile.FileName);
                var safeFileName = $"{Guid.NewGuid()}_{fileName}";
                var filePath = Path.Combine(imagesPath, safeFileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await item.ImageFile.CopyToAsync(stream);
                }

                item.Image = "/images/" + safeFileName; // 存图片 URL
            }

            // Firestore 存储
            CollectionReference itemsRef = _firestore.Collection("Items");
            await itemsRef.AddAsync(new
            {
                IName = item.IName,
                IType = item.IType,
                Description = item.Idescription,
                LocationID = item.LocationID,
                Image = item.Image,
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

        [HttpPost]
        public async Task<IActionResult> CreateFoundPost(Item item)
        {
            if (!ModelState.IsValid)
                return View(item);

            if (item.ImageFile != null && item.ImageFile.Length > 0)
            {
                var imagesPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images");
                if (!Directory.Exists(imagesPath))
                {
                    Directory.CreateDirectory(imagesPath);
                }

                var fileName = Path.GetFileName(item.ImageFile.FileName);
                var safeFileName = $"{Guid.NewGuid()}_{fileName}";
                var filePath = Path.Combine(imagesPath, safeFileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await item.ImageFile.CopyToAsync(stream);
                }

                item.Image = "/images/" + safeFileName;
            }

            CollectionReference foundItemsRef = _firestore.Collection("FoundItems");
            await foundItemsRef.AddAsync(new
            {
                IName = item.IName,
                IType = item.IType,
                Description = item.Idescription,
                LocationID = item.LocationID,
                Image = item.Image,
                Category = item.Category,
                Date = item.Date.ToUniversalTime(),
                CreatedAt = Timestamp.GetCurrentTimestamp()
            });

            return RedirectToAction("Index");
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
