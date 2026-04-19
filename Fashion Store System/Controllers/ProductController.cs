using Fashion_Store_System.Data;
using Fashion_Store_System.Models;
using Fashion_Store_System.ViewModels.ProductVM;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

public class ProductController : Controller
{
    private readonly ApplicationDbContext _context;
    public ProductController(ApplicationDbContext context) => _context = context;

    // --- 1. العرض (Index) ---
    public async Task<IActionResult> Index()
    {
        // سحب البيانات مع الجداول المرتبطة
        var rawData = await _context.Products
            .Include(p => p.Category)
            .Include(p => p.Variants)
            .OrderByDescending(p => p.Id)
            .ToListAsync();

        // تحويل البيانات للـ ViewModel وحساب الكميات
        var products = rawData.Select(p => new ProductVMDt
        {
            Id = p.Id,
            Name = p.Name,
            Price = p.Price,
            ImageUrl = p.ImageUrl,
            CategoryName = p.Category?.Name ?? "بدون قسم",
            Discount = p.Discount,
            IsActive = p.IsActive,
            // الحسبة اللي بتصلح الأصفار
            Quantity = p.Variants?.Sum(v => v.Quantity) ?? 0,
            Variants = p.Variants?.Select(v => new ProductVariantVMDt
            {
                ColorName = v.ProductColor?.Name,
                SizeName = v.ProductSize?.Name,
                Quantity = v.Quantity
            }).ToList()
        }).ToList();

        // حسابات الـ ViewBag من القائمة الجاهزة
        ViewBag.TotalProducts = products.Count;
        ViewBag.TotalQuantity = products.Sum(p => p.Quantity);
        ViewBag.LowStockCount = products.Count(p => p.Quantity <= 5);
        ViewBag.TotalValue = products.Sum(p => p.Price * p.Quantity);

        return View(products);
    }

    // --- 2. الإضافة (Create) ---
    // في الـ GET Action
    public async Task<IActionResult> Create()
    {
        ViewBag.CategoryList = new SelectList(await _context.Category.ToListAsync(), "Id", "Name");
        ViewBag.ColorList = new SelectList(await _context.ProductColors.ToListAsync(), "Id", "Name");
        ViewBag.SizeList = new SelectList(await _context.ProductSizes.ToListAsync(), "Id", "Name");

        return View(new ProductVM());
    }

    // في الـ POST Action (لو الداتا منقوصة ورجعتي للـ View)
   
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ProductVM vm, int[] VariantColors, int[] VariantSizes, int[] VariantQuantities)
    {
        if (ModelState.IsValid)
        {
            // 1. معالجة الصورة (نفس الكود السابق)
            string fileName = "default.jpg";

            if (vm.ImageFile != null)
            {
                string uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images/Products");

                // اسم عشوائي عشان يمنع التكرار
                fileName = Guid.NewGuid().ToString() + Path.GetExtension(vm.ImageFile.FileName);

                string filePath = Path.Combine(uploadsFolder, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await vm.ImageFile.CopyToAsync(stream);
                }
            }

            // 2. إنشاء كائن المنتج الأساسي
            var product = new Product
            {
                Name = vm.Name,
                Description = vm.Description,
                Price = vm.Price,
                Discount = vm.Discount,
                CategoryId = vm.CategoryId,
                ImageUrl = fileName,
                IsActive = vm.IsActive
            };
            _context.Products.Add(product);
            await _context.SaveChangesAsync(); // بنحفظ عشان ناخد الـ ProductId

            // 3. الربط مع الكميات المحددة يدوياً
            if (VariantQuantities != null && VariantQuantities.Length > 0)
            {
                for (int i = 0; i < VariantQuantities.Length; i++)
                {
                    var variant = new ProductVariant
                    {
                        ProductId = product.Id,
                        ProductColorId = VariantColors[i],
                        ProductSizeId = VariantSizes[i],
                        Quantity = VariantQuantities[i]
                    };
                    _context.ProductVariants.Add(variant);
                }
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }
        return View(vm);
    }
    // --- 3. التعديل (Edit) ---
    public async Task<IActionResult> Edit(int id)
    {
        // 1. نجيب المنتج من الداتابيز
        var product = await _context.Products.FindAsync(id);
        if (product == null) return NotFound();

        // 2. نملا الـ ViewModel بالداتا اللي جبناها
        var viewModel = new ProductVM
        {
            Id = product.Id,
            Name = product.Name,
            Price = product.Price,
            Discount = product.Discount,
            Description = product.Description,
            CategoryId = product.CategoryId,
            IsActive = product.IsActive
            // لاحظي هنا مش بنحط الـ ImageFile لأنه ملف هيترفع، بس بنعرض الصورة القديمة في الـ ViewBag
        };

        // 3. نجهز القوائم والصورة للعرض
        ViewBag.CategoryList = new SelectList(_context.Category, "Id", "Name", product.CategoryId);
        ViewBag.CurrentImageUrl = product.ImageUrl;

        return View(viewModel); // بنبعت الـ ViewModel مش الـ Product
    }
    [HttpPost]
    public async Task<IActionResult> Edit(ProductVM model)
    {
        if (ModelState.IsValid)
        {
            // 1. نجيب المنتج الأصلي من الداتابيز عشان نحدثه
            var product = await _context.Products.FindAsync(model.Id);
            if (product == null) return NotFound();

            // 2. نحدث البيانات الأساسية
            product.Name = model.Name;
            product.Price = model.Price;
            product.Discount = model.Discount;
            product.Description = model.Description;
            product.CategoryId = model.CategoryId;
            product.IsActive = model.IsActive;

            // 3. لو اليوزر رفع صورة جديدة، بنعالجها
            if (model.ImageFile != null)
            {
                string folder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images/Products");
                string fileName = Guid.NewGuid().ToString() + "_" + model.ImageFile.FileName;
                string filePath = Path.Combine(folder, fileName);

                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await model.ImageFile.CopyToAsync(fileStream);
                }
                product.ImageUrl = fileName; // نحدث اسم الصورة الجديد
            }

            _context.Update(product);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // لو الـ Model مش Valid، بنرجع لنفس الصفحة ونملا الـ Dropdown تاني
        ViewBag.CategoryList = new SelectList(_context.Category, "Id", "Name", model.CategoryId);
        return View(model);
    }

    // --- 4. الحذف (Delete) ---
    [HttpPost]
    public async Task<IActionResult> Delete(int id)
    {
        var product = await _context.Products.FindAsync(id);
        if (product == null) return Json(new { success = false });

        _context.Products.Remove(product);
        await _context.SaveChangesAsync();
        return Json(new { success = true });
    }


    public async Task<IActionResult> Details(int id)
    {
        var product = await _context.Products
            .Include(p => p.Category) // عشان اسم القسم يظهر
            .Include(p => p.Variants) // جدول الـ Variants نفسه
                .ThenInclude(v => v.ProductColor) // جدول الألوان المرتبط بالـ Variant
            .Include(p => p.Variants)
                .ThenInclude(v => v.ProductSize)  // جدول المقاسات المرتبط بالـ Variant
            .FirstOrDefaultAsync(m => m.Id == id);

        if (product == null) return NotFound();

        return View(product);
    }
}