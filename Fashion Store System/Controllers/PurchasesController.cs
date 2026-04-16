using Fashion_Store_System.Data;
using Fashion_Store_System.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Fashion_Store_System.Controllers
{
    public class PurchasesController : Controller
    {
        private readonly ApplicationDbContext _context;
        public PurchasesController(ApplicationDbContext dbContext)
        {
            _context = dbContext;
        }

      
        public async Task<IActionResult> Index()
        {
            // بنجيب الفواتير ومعاها اسم المورد عشان تظهر في الجدول
            var invoices = await _context.PurchaseInvoice
                .Include(i => i.Supplier)
                .OrderByDescending(i => i.InvoiceDate)
                .ToListAsync();
            return View(invoices);
        }

        [HttpGet]
        public IActionResult Create()
        {
            // 1. هاتي الموردين والمنتجات من الداتابيز
            var suppliersList = _context.Supplier.ToList();
            var productsList = _context.Products.ToList();

            // 2. اطبعي في الـ Console عشان نتأكد إنهم موجودين (اختياري للتأكد)
            System.Diagnostics.Debug.WriteLine("عدد الموردين: " + suppliersList.Count);

            // 3. حطيهم في الـ ViewBag
            ViewBag.Suppliers = suppliersList;
            ViewBag.Products = productsList;

            return View();
        }

        // 3. استقبال الفاتورة وحفظها وتحديث المخزن (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(PurchaseInvoice invoice, List<PurchaseItem> Items)
        {
            if (Items == null || !Items.Any())
            {
                ModelState.AddModelError("", "يجب إضافة صنف واحد على الأقل للفاتورة.");
            }

            if (ModelState.IsValid)
            {
                using (var transaction = await _context.Database.BeginTransactionAsync())
                {
                    try
                    {
                        // أ- حساب إجمالي الفاتورة من الأصناف
                        invoice.TotalAmount = Items.Sum(i => i.Quantity * i.UnitPrice);
                        invoice.InvoiceDate = DateTime.Now;

                        // ب- حفظ رأس الفاتورة
                        _context.PurchaseInvoice.Add(invoice);
                        await _context.SaveChangesAsync();

                        // ج- لفة على كل صنف (Item) في الفاتورة
                        foreach (var item in Items)
                        {
                            // ربط الصنف بالفاتورة اللي لسه مخلوقة
                            item.PurchaseInvoiceId = invoice.Id;
                            _context.PurchaseItem.Add(item);

                            // د- التحديث السحري للمخزن (أهم خطوة)
                            var product = await _context.Products.FindAsync(item.ProductId);
                            if (product != null)
                            {
                                // بنزود الكمية الجديدة على اللي موجود
                                product.Quantity += item.Quantity;

                                // اختياري: تحديث سعر الشراء في جدول المنتجات لو حابة
                                // product.Price = item.UnitPrice * 1.2m; // مثلاً بنحط هامش ربح 20%
                            }
                        }

                        await _context.SaveChangesAsync();
                        await transaction.CommitAsync(); // تثبيت العملية في الداتابيز

                        TempData["Success"] = "تم حفظ الفاتورة وتحديث كميات المخزن بنجاح.";
                        return RedirectToAction(nameof(Index));
                    }
                    catch (Exception ex)
                    {
                        await transaction.RollbackAsync(); // لو حصل أي غلط، الغي كل حاجة عشان الداتابيز متلخبطش
                        ModelState.AddModelError("", "حدث خطأ أثناء الحفظ: " + ex.Message);
                    }
                }
            }

            // لو حصل مشكلة، بنرجع البيانات لليستات تاني عشان الصفحة متضربش
            ViewBag.Suppliers = _context.Supplier.ToList();
            ViewBag.Products = _context.Products.ToList();
            return View(invoice);
        }

        // 4. عرض تفاصيل فاتورة قديمة
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var invoice = await _context.PurchaseInvoice
                .Include(i => i.Supplier)
                .Include(i => i.PurchaseItems)
                    .ThenInclude(pi => pi.Product)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (invoice == null) return NotFound();

            return View(invoice);
        }

        [HttpPost]
         public async Task<IActionResult> Delete(int id)
   {
    var invoice = await _context.PurchaseInvoice
        .Include(i => i.PurchaseItems)
        .FirstOrDefaultAsync(m => m.Id == id);

    if (invoice == null) return Json(new { success = false, message = "الفاتورة غير موجودة" });

    // قبل ما نحذف، لازم ننقص الكميات اللي زودناها في المخزن
    foreach (var item in invoice.PurchaseItems)
    {
        var product = await _context.Products.FindAsync(item.ProductId);
        if (product != null)
        {
            product.Quantity -= item.Quantity; // بنرجع المخزن لأصله
        }
    }

    _context.PurchaseInvoice.Remove(invoice);
    await _context.SaveChangesAsync();
    
    return Json(new { success = true, message = "تم حذف الفاتورة وتعديل المخزن" });
}

        // 1. صفحة التعديل (GET)
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var invoice = await _context.PurchaseInvoice
                .Include(i => i.PurchaseItems)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (invoice == null) return NotFound();

            ViewBag.Suppliers = _context.Supplier.ToList();
            ViewBag.Products = _context.Products.ToList();

            return View(invoice);
        }

        // 2. معالجة التعديل (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, PurchaseInvoice invoice, List<PurchaseItem> Items)
        {
            if (id != invoice.Id) return NotFound();

            try
            {
                // أ- نجيب الفاتورة القديمة بالأصناف اللي فيها قبل ما تتعدل
                var oldInvoice = await _context.PurchaseInvoice
                    .Include(i => i.PurchaseItems)
                    .AsNoTracking() // مهم عشان ميعملش Conflict مع التعديل الجديد
                    .FirstOrDefaultAsync(m => m.Id == id);

                // ب- نرجع المخزن لأصله (نطرح الكميات القديمة)
                foreach (var oldItem in oldInvoice.PurchaseItems)
                {
                    var product = await _context.Products.FindAsync(oldItem.ProductId);
                    if (product != null) product.Quantity -= oldItem.Quantity;
                }

                // ج- نمسح الأصناف القديمة من جدول PurchaseItems
                var oldItems = _context.PurchaseItem.Where(x => x.PurchaseInvoiceId == id);
                _context.PurchaseItem.RemoveRange(oldItems);

                // د- نضيف الأصناف الجديدة ونحدث المخزن (نجمع الكميات الجديدة)
                invoice.TotalAmount = Items.Sum(i => i.Quantity * i.UnitPrice);
                foreach (var newItem in Items)
                {
                    newItem.PurchaseInvoiceId = id;
                    _context.PurchaseItem.Add(newItem);

                    var product = await _context.Products.FindAsync(newItem.ProductId);
                    if (product != null) product.Quantity += newItem.Quantity;
                }

                _context.Update(invoice);
                await _context.SaveChangesAsync();
                TempData["Success"] = "تم تعديل الفاتورة وتحديث المخزن بنجاح";
            }
            catch (Exception ex)
            {
                TempData["Error"] = "حدث خطأ: " + ex.Message;
            }

            return RedirectToAction(nameof(Index));
        }




    }


}

