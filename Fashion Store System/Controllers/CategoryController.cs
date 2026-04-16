using Fashion_Store_System.Data;
using Fashion_Store_System.Models;
using Fashion_Store_System.ViewModels.CategoryVM;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;

namespace Fashion_Store_System.Controllers
{
    public class CategoryController : Controller
    {
        private readonly ApplicationDbContext _dbContext;
        public CategoryController(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }
        public IActionResult Index()
        {
            var Category = _dbContext.Category.ToList();
            return View(Category);
        }

        // GET: Category/Create
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CategoryVM categoryVM)
        {
            if (ModelState.IsValid)
            {
                string fileName = "";

                // التأكد من أن المستخدم اختار ملف
                if (categoryVM.ImageFile != null)
                {
                    // تحديد مسار المجلد (wwwroot/images/categories)
                    string uploadDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images/categories");

                    // إنشاء اسم فريد للملف لمنع التكرار
                    fileName = Guid.NewGuid().ToString() + "-" + categoryVM.ImageFile.FileName;

                    string filePath = Path.Combine(uploadDir, fileName);

                    // حفظ الملف فعلياً
                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await categoryVM.ImageFile.CopyToAsync(fileStream);
                    }
                }

                var category = new Category
                {
                    Name = categoryVM.Name,
                    ImageUrl = fileName != "" ? "/images/categories/" + fileName : null, // حفظ المسار في القاعدة
                    IsActive = categoryVM.IsActive,
                    CreatedAt = DateTime.Now
                };

                _dbContext.Category.Add(category);
                await _dbContext.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(categoryVM);
        }

        // 1. التفاصيل - Details
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var category = await _dbContext.Category
                .FirstOrDefaultAsync(m => m.Id == id);

            if (category == null) return NotFound();

            // هنا استخدمنا الـ DTO اللي انت عامله للعرض
            var viewModel = new CategoryVMDt
            {
                Id = category.Id,
                Name = category.Name,
                ImageUrl = category.ImageUrl,
                IsActive = category.IsActive,
                CreatedAt = category.CreatedAt
            };

            return View(viewModel);
        }
        // --- أضيفي هذا الجزء قبل أكشن الـ HttpPost Edit ---

        // GET: Category/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var category = await _dbContext.Category.FindAsync(id);
            if (category == null) return NotFound();

            // هنا بنملى الـ ViewModel بالبيانات القديمة عشان تظهر في الصفحة
            var viewModel = new CategoryVM
            {
                Name = category.Name,
                ImageUrl = category.ImageUrl, // بنحتفظ بالمسار القديم هنا للعرض
                IsActive = category.IsActive
            };

            return View(viewModel);
        }

        // --- أكشن الـ HttpPost Edit (تأكدي إنه كدة) ---
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, CategoryVM categoryVM)
        {
            if (ModelState.IsValid)
            {
                var category = await _dbContext.Category.FindAsync(id);
                if (category == null) return NotFound();

                category.Name = categoryVM.Name;
                category.IsActive = categoryVM.IsActive;

                if (categoryVM.ImageFile != null)
                {
                    string uploadDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images/categories");
                    if (!Directory.Exists(uploadDir)) Directory.CreateDirectory(uploadDir);

                    string fileName = Guid.NewGuid().ToString() + "-" + categoryVM.ImageFile.FileName;
                    string filePath = Path.Combine(uploadDir, fileName);

                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await categoryVM.ImageFile.CopyToAsync(fileStream);
                    }
                    category.ImageUrl = "/images/categories/" + fileName;
                }

                _dbContext.Update(category);
                await _dbContext.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(categoryVM);
        }
        // 4. الحذف - Delete (GET)
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var category = await _dbContext.Category.FirstOrDefaultAsync(m => m.Id == id);
            if (category == null) return NotFound();

            return View(category);
        }

        // 5. الحذف - Delete (POST)
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var category = await _dbContext.Category.FindAsync(id);
            if (category != null)
            {
                _dbContext.Category.Remove(category);
                await _dbContext.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
