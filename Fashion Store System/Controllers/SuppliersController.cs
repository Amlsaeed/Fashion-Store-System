using Fashion_Store_System.Data;
using Fashion_Store_System.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Fashion_Store_System.Controllers
{
    public class SuppliersController : Controller
    {
        private readonly ApplicationDbContext _context;

        public SuppliersController(ApplicationDbContext context)
        {
            _context = context;
        }

        // 1. عرض قائمة الموردين
        public async Task<IActionResult> Index()
        {
            // بنجيب كل الموردين من الداتابيز ونبعتهم للفيو
            var suppliers = await _context.Supplier.ToListAsync();
            return View(suppliers);
        }

        // 2. عرض تفاصيل مورد معين
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var supplier = await _context.Supplier
                .FirstOrDefaultAsync(m => m.Id == id);

            if (supplier == null) return NotFound();

            return View(supplier);
        }

        // 3. إضافة مورد جديد (الصفحة نفسها - GET)
        public IActionResult Create()
        {
            return View();
        }

        // 4. حفظ المورد الجديد (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Supplier supplier)
        {
            if (ModelState.IsValid)
            {
                _context.Add(supplier);
                await _context.SaveChangesAsync();
                TempData["Success"] = "تم إضافة المورد بنجاح";
                return RedirectToAction(nameof(Index));
            }
            return View(supplier);
        }

        // 5. تعديل بيانات مورد (الصفحة نفسها - GET)
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var supplier = await _context.Supplier.FindAsync(id);
            if (supplier == null) return NotFound();

            return View(supplier);
        }

        // 6. حفظ تعديلات المورد (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Supplier supplier)
        {
            if (id != supplier.Id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(supplier);
                    await _context.SaveChangesAsync();
                    TempData["Success"] = "تم تحديث بيانات المورد";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!SupplierExists(supplier.Id)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }
            return View(supplier);
        }

        // 7. حذف مورد (بنظام الـ Ajax اللي بنحبه)
        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var supplier = await _context.Supplier.FindAsync(id);
            if (supplier == null)
                return Json(new { success = false, message = "المورد غير موجود" });

            _context.Supplier.Remove(supplier);
            await _context.SaveChangesAsync();
            return Json(new { success = true, message = "تم حذف المورد بنجاح" });
        }

        // وظيفة مساعدة للتأكد من وجود المورد
        private bool SupplierExists(int id)
        {
            return _context.Supplier.Any(e => e.Id == id);
        }
    }
}

