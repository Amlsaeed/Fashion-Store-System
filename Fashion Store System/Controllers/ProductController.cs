using Fashion_Store_System.Data;
using Fashion_Store_System.Models;
using Fashion_Store_System.ViewModels.ProductVM;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace Fashion_Store_System.Controllers
{
    public class ProductController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;

        // Constructor
        public ProductController(ApplicationDbContext context, IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment;
        }
        //public IActionResult Index()
        //{
        //    return View();
        //}

        public async Task<IActionResult> Index()
        {
            
            var products = await _context.Products
        .Include(p => p.Category)
        .OrderByDescending(p => p.Id) // رتبنا حسب الـ ID عشان الأحدث يظهر فوق
        .Select(p => new ProductVMDt
        {
            Id = p.Id, // التأكد من إرسال الـ ID
            Name = p.Name,
            Price = p.Price,
            Quantity = p.Quantity,
            Discount = p.Discount,
            ImageUrl = p.ImageUrl,
            CategoryName = p.Category.Name,
            IsActive = p.IsActive,
            CreatedAt = p.CreatedAt
        })
        .ToListAsync();

            // 1. حساب إجمالي عدد المنتجات (الأنواع)
            ViewBag.TotalProducts = products.Count();

            // 2. حساب إجمالي كمية القطع في المخزن (كل الكميات مجموعة)
            ViewBag.TotalQuantity = products.Sum(p => p.Quantity);

            // 3. حساب إجمالي قيمة البضاعة (السعر × الكمية لكل منتج)
            ViewBag.TotalValue = products.Sum(p => p.Price * p.Quantity);

            // 4. عد المنتجات اللي كميتها أقل من 5 (النواقص)
            ViewBag.LowStockCount = products.Count(p => p.Quantity <= 5);

            return View(products);
        }

        // 1. أكشن الـ GET عشان نفتح صفحة الإضافة
        [HttpGet]
        public IActionResult Create()
        {
            // بنجهز قائمة الأقسام عشان الأدمن يختار المنتج ده تبع أنهي قسم
            var categories = _context.Category.ToList();
            ViewBag.CategoryList = new SelectList(categories, "Id", "Name");

            return View();
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ProductVM model)
        {
            // شيلنا شرط الـ ModelState.IsValid مؤقتاً للتجربة أو عشان نتأكد إن البيانات بتدخل
            try
            {
                string fileName = "default-product.png"; // قيمة افتراضية

                if (model.ImageFile != null)
                {
                    string uploadDir = Path.Combine(_webHostEnvironment.WebRootPath, "Images/Products");
                    if (!Directory.Exists(uploadDir)) Directory.CreateDirectory(uploadDir);

                    fileName = Guid.NewGuid().ToString() + "-" + model.ImageFile.FileName;
                    string filePath = Path.Combine(uploadDir, fileName);

                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await model.ImageFile.CopyToAsync(fileStream);
                    }
                    fileName = "/Images/Products/" + fileName; // المسار الكامل
                }

                var product = new Product
                {
                    Name = model.Name,
                    Description = model.Description,
                    Price = model.Price,
                    Quantity = model.Quantity,
                    Discount = model.Discount,
                    CategoryId = model.CategoryId,
                    ImageUrl = fileName,
                    IsActive = model.IsActive,
                    CreatedAt = DateTime.Now
                };

                _context.Products.Add(product);
                await _context.SaveChangesAsync(); // السطر ده هو اللي بيسمع في الداتابيز

                TempData["SuccessMessage"] = "تم إضافة المنتج بنجاح!";
                return RedirectToAction(nameof(Index)); // هيروح للجدول والرسالة هتظهر هناك
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "حدث خطأ: " + ex.Message;
            }

            ViewBag.CategoryList = new SelectList(_context.Category.ToList(), "Id", "Name");
            return View(model);
        }



        // 1. التفاصيل (Details)
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var product = await _context.Products
                .Include(p => p.Category)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (product == null) return NotFound();

            return View(product);
        }

        // 2. التعديل (Edit - GET)
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var product = await _context.Products.FindAsync(id);
            if (product == null) return NotFound();

            // تجهيز البيانات للـ ViewModel
            var model = new ProductVM
            {
                Name = product.Name,
                Description = product.Description,
                Price = product.Price,
                Quantity = product.Quantity,
                Discount = product.Discount,
                CategoryId = product.CategoryId,
                IsActive = product.IsActive
            };

            ViewBag.ImageUrl = product.ImageUrl; // عشان نعرض الصورة القديمة
            ViewBag.CategoryList = new SelectList(_context.Category.ToList(), "Id", "Name", product.CategoryId);
            return View(model);
        }

        // 3. التعديل (Edit - POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, ProductVM model)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null) return NotFound();

            if (ModelState.IsValid)
            {
                if (model.ImageFile != null) // لو الأدمن رفع صورة جديدة
                {
                    string uploadDir = Path.Combine(_webHostEnvironment.WebRootPath, "Images/Products");
                    string fileName = Guid.NewGuid().ToString() + "-" + model.ImageFile.FileName;
                    string filePath = Path.Combine(uploadDir, fileName);
                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await model.ImageFile.CopyToAsync(fileStream);
                    }
                    product.ImageUrl = "/Images/Products/" + fileName;
                }

                product.Name = model.Name;
                product.Description = model.Description;
                product.Price = model.Price;
                product.Quantity = model.Quantity;
                product.Discount = model.Discount;
                product.CategoryId = model.CategoryId;
                product.IsActive = model.IsActive;

                _context.Update(product);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "تم تحديث بيانات المنتج بنجاح!";
                return RedirectToAction(nameof(Index));
            }
            ViewBag.CategoryList = new SelectList(_context.Category.ToList(), "Id", "Name", model.CategoryId);
            return View(model);
        }

        // 4. الحذف (Delete - POST)
        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null) return Json(new { success = false, message = "المنتج غير موجود" });

            _context.Products.Remove(product);
            await _context.SaveChangesAsync();
            return Json(new { success = true, message = "تم حذف المنتج نهائياً" });
        }



    }
}

