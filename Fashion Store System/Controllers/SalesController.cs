using Fashion_Store_System.Data;
using Fashion_Store_System.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Fashion_Store_System.Controllers
{
    public class SalesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public SalesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // 1. عرض قائمة فواتير المبيعات
        public async Task<IActionResult> Index()
        {
            var sales = await _context.SalesInvoices
                .OrderByDescending(s => s.SalesDate)
                .ToListAsync();
            return View(sales);
        }

        // 2. صفحة عمل فاتورة بيع جديدة (GET)
        public IActionResult Create()
        {
            // بنجيب المنتجات اللي ليها كمية أكبر من صفر بس عشان نبيع منها
            ViewBag.Products = _context.Products.Where(p => p.Quantity > 0).ToList();
            return View();
        }

        // 3. حفظ الفاتورة ونقص الكمية من المخزن (POST)
        //[HttpPost]
        //[ValidateAntiForgeryToken]
        //public async Task<IActionResult> Create(SalesInvoice invoice, List<SalesItem> Items)
        //{
        //    if (Items == null || !Items.Any())
        //    {
        //        ModelState.AddModelError("", "يجب إضافة صنف واحد على الأقل للفاتورة.");
        //    }

        //    if (ModelState.IsValid)
        //    {
        //        // استخدام Transaction عشان نضمن إن الخصم من المخزن والفاتورة يتموا مع بعض
        //        using (var transaction = await _context.Database.BeginTransactionAsync())
        //        {
        //            try
        //            {
        //                // أ- حساب إجمالي الفاتورة وتاريخها
        //                invoice.TotalAmount = Items.Sum(i => i.Quantity * i.UnitPrice);
        //                invoice.SalesDate = DateTime.Now;

        //                _context.SalesInvoices.Add(invoice);
        //                await _context.SaveChangesAsync(); // حفظ الفاتورة عشان ناخد ID

        //                // ب- معالجة الأصناف المتباعة
        //                foreach (var item in Items)
        //                {
        //                    var product = await _context.Products.FindAsync(item.ProductId);

        //                    if (product != null)
        //                    {
        //                        // ج- التأكد من توفر الكمية في المخزن
        //                        if (product.Quantity >= item.Quantity)
        //                        {
        //                            // خصم الكمية من المخزن
        //                            product.Quantity -= item.Quantity;

        //                            // ربط الصنف بالفاتورة وحفظه
        //                            item.SalesInvoiceId = invoice.Id;
        //                            _context.SalesItems.Add(item);
        //                        }
        //                        else
        //                        {
        //                            // لو الكمية المطلوبة أكبر من اللي في المخزن نوقف العملية
        //                            throw new Exception($"عذراً، الكمية المتاحة من {product.Name} هي {product.Quantity} فقط.");
        //                        }
        //                    }
        //                }

        //                await _context.SaveChangesAsync();
        //                await transaction.CommitAsync();

        //                TempData["Success"] = "تم تسجيل عملية البيع وتحديث المخزن.";
        //                return RedirectToAction(nameof(Index));
        //            }
        //            catch (Exception ex)
        //            {
        //                await transaction.RollbackAsync();
        //                ModelState.AddModelError("", ex.Message);
        //            }
        //        }
        //    }

        //    // لو حصل خطأ نرجع المنتجات تاني للـ View
        //    ViewBag.Products = _context.Products.Where(p => p.Quantity > 0).ToList();
        //    return View(invoice);
        //}

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(SalesInvoice invoice, List<SalesItem> Items)
        {
            if (Items == null || !Items.Any())
            {
                ModelState.AddModelError("", "يجب إضافة صنف واحد على الأقل للفاتورة.");
                ViewBag.Products = _context.Products.Where(p => p.Quantity > 0).ToList();
                return View(invoice);
            }

            using (var transaction = await _context.Database.BeginTransactionAsync())
            {
                try
                {
                    invoice.SalesDate = DateTime.Now;
                    invoice.TotalAmount = 0; // هنصفره ونحسبه جوه الـ Loop بناءً على سعر المخزن

                    _context.SalesInvoices.Add(invoice);
                    await _context.SaveChangesAsync();

                    foreach (var item in Items)
                    {
                        // بنجيب المنتج من الداتابيز عشان ناخد السعر الحقيقي والكمية
                        var product = await _context.Products.FindAsync(item.ProductId);

                        if (product != null)
                        {
                            if (product.Quantity >= item.Quantity)
                            {
                                // 1. تثبيت سعر البيع من جدول المنتجات (مش من الفيو)
                                item.UnitPrice = product.Price;

                                // 2. خصم الكمية من المخزن
                                product.Quantity -= item.Quantity;

                                // 3. ربط الصنف بالفاتورة
                                item.SalesInvoiceId = invoice.Id;
                                _context.SalesItems.Add(item);

                                // 4. إضافة قيمة الصنف لإجمالي الفاتورة
                                invoice.TotalAmount += (item.Quantity * product.Price);
                            }
                            else
                            {
                                throw new Exception($"عذراً، الكمية المتاحة من {product.Name} هي {product.Quantity} فقط.");
                            }
                        }
                    }

                    // تحديث إجمالي الفاتورة النهائي بعد الحساب
                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    TempData["Success"] = "تم تسجيل البيع بنجاح بسعر المخزن الحالي.";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    ModelState.AddModelError("", ex.Message);
                    ViewBag.Products = _context.Products.Where(p => p.Quantity > 0).ToList();
                    return View(invoice);
                }
            }
        }

        // 4. عرض تفاصيل فاتورة بيع
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var invoice = await _context.SalesInvoices
                .Include(s => s.SalesItems)
                .ThenInclude(si => si.Product)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (invoice == null) return NotFound();

            return View(invoice);
        }

        // 5. حذف فاتورة بيع (وإرجاع الكمية للمخزن)
        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var invoice = await _context.SalesInvoices
                .Include(s => s.SalesItems)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (invoice == null) return Json(new { success = false });

            foreach (var item in invoice.SalesItems)
            {
                var product = await _context.Products.FindAsync(item.ProductId);
                if (product != null)
                {
                    product.Quantity += item.Quantity; // بنرجع البضاعة للمخزن تاني
                }
            }

            _context.SalesInvoices.Remove(invoice);
            await _context.SaveChangesAsync();

            return Json(new { success = true });
        }

        // 1. صفحة التعديل (GET)
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var invoice = await _context.SalesInvoices
                .Include(s => s.SalesItems)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (invoice == null) return NotFound();

            // بنجيب كل المنتجات عشان لو حب يغير صنف بصنف تاني
            ViewBag.Products = _context.Products.ToList();

            return View(invoice);
        }

        // 2. معالجة التعديل (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, SalesInvoice invoice, List<SalesItem> Items)
        {
            if (id != invoice.Id) return NotFound();

            using (var transaction = await _context.Database.BeginTransactionAsync())
            {
                try
                {
                    // أ- نجيب الفاتورة القديمة بالأصناف اللي فيها (قبل التعديل)
                    var oldInvoice = await _context.SalesInvoices
                        .Include(s => s.SalesItems)
                        .AsNoTracking() // مهمة جداً عشان ميعملش Conflict
                        .FirstOrDefaultAsync(m => m.Id == id);

                    // ب- إرجاع المخزن لأصله (بنزود الكميات اللي كانت متباعة قبل كدة)
                    foreach (var oldItem in oldInvoice.SalesItems)
                    {
                        var product = await _context.Products.FindAsync(oldItem.ProductId);
                        if (product != null)
                        {
                            product.Quantity += oldItem.Quantity; // رجعنا البضاعة المخزن مؤقتاً
                        }
                    }

                    // ج- مسح الأصناف القديمة من جدول المبيعات
                    var oldItems = _context.SalesItems.Where(x => x.SalesInvoiceId == id);
                    _context.SalesItems.RemoveRange(oldItems);

                    // د- إضافة الأصناف الجديدة والخصم من المخزن من جديد
                    invoice.TotalAmount = Items.Sum(i => i.Quantity * i.UnitPrice);
                    foreach (var newItem in Items)
                    {
                        var product = await _context.Products.FindAsync(newItem.ProductId);

                        if (product != null)
                        {
                            if (product.Quantity >= newItem.Quantity)
                            {
                                product.Quantity -= newItem.Quantity; // خصم الكمية الجديدة
                                newItem.SalesInvoiceId = id;
                                _context.SalesItems.Add(newItem);
                            }
                            else
                            {
                                throw new Exception($"الكمية المطلوبة من {product.Name} غير متوفرة في المخزن!");
                            }
                        }
                    }

                    _context.Update(invoice);
                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    TempData["Success"] = "تم تعديل فاتورة البيع وتحديث المخزن بنجاح";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    ModelState.AddModelError("", "حدث خطأ: " + ex.Message);

                    ViewBag.Products = _context.Products.ToList();
                    return View(invoice);
                }
            }
        }
    }
}

