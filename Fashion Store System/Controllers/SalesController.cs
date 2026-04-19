using Fashion_Store_System.Data;
using Fashion_Store_System.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
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
            var availableVariants = _context.ProductVariants
         .Include(v => v.Product)
         .Include(v => v.ProductColor)
         .Include(v => v.ProductSize)
         .Where(v => v.Quantity > 0)
         .Select(v => new {
             Id = v.Id,
             DisplayName = $"{v.Product.Name} - {v.ProductColor.Name} - {v.ProductSize.Name} (المتاح: {v.Quantity})"
         }).ToList();

            ViewBag.Products = new SelectList(availableVariants, "Id", "DisplayName");
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
                return View(invoice);
            }

            using (var transaction = await _context.Database.BeginTransactionAsync())
            {
                try
                {
                    invoice.SalesDate = DateTime.Now;
                    invoice.TotalAmount = 0;

                    _context.SalesInvoices.Add(invoice);
                    await _context.SaveChangesAsync();

                    foreach (var item in Items)
                    {
                        // بنجيب الـ Variant المحدد (باللون والمقاس)
                        var variant = await _context.ProductVariants
                            .Include(v => v.Product)
                            .FirstOrDefaultAsync(v => v.Id == item.ProductVariantId); // اتأكدي إن الاسم في الموديل ProductVariantId

                        if (variant != null)
                        {
                            if (variant.Quantity >= item.Quantity)
                            {
                                // 1. تثبيت السعر من البرودكت الأساسي
                                item.UnitPrice = variant.Product.Price;

                                // 2. خصم الكمية من الـ Variant الصح
                                variant.Quantity -= item.Quantity;

                                // 3. ربط الصنف بالفاتورة
                                item.SalesInvoiceId = invoice.Id;
                                _context.SalesItems.Add(item);

                                // 4. الحساب الإجمالي
                                invoice.TotalAmount += (item.Quantity * variant.Product.Price);
                            }
                            else
                            {
                                throw new Exception($"الكمية المتاحة من {variant.Product.Name} ({variant.ProductColor?.Name}) هي {variant.Quantity} فقط.");
                            }
                        }
                    }

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    TempData["Success"] = "تمت عملية البيع بنجاح.";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    ModelState.AddModelError("", ex.Message);
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
            // 1. نجيب الفاتورة ومعاها الأصناف (SalesItems)
            var invoice = await _context.SalesInvoices
                .Include(s => s.SalesItems)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (invoice == null) return Json(new { success = false, message = "الفاتورة غير موجودة" });

            // 2. لفة على كل صنف في الفاتورة عشان نرجع الكمية لمكانها الصح
            foreach (var item in invoice.SalesItems)
            {
                // بنستخدم ProductVariantId اللي إنتي لسه ضايفاه في الموديل
                var variant = await _context.ProductVariants.FindAsync(item.ProductVariantId);

                if (variant != null)
                {
                    // بنرجع البضاعة للـ (اللون والمقاس) المحدد
                    variant.Quantity += item.Quantity;
                    _context.Update(variant);
                }
            }

            // 3. نحذف الفاتورة نفسها (هتحذف الأصناف معاها أوتوماتيك لو فيه Cascade Delete)
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
                    // أ- نجيب الفاتورة القديمة بالأصناف اللي فيها
                    var oldInvoice = await _context.SalesInvoices
                        .Include(s => s.SalesItems)
                        .AsNoTracking()
                        .FirstOrDefaultAsync(m => m.Id == id);

                    if (oldInvoice == null) return NotFound();

                    // ب- إرجاع المخزن المطور لأصله (بنزود كميات الألوان والمقاسات القديمة)
                    foreach (var oldItem in oldInvoice.SalesItems)
                    {
                        var variant = await _context.ProductVariants.FindAsync(oldItem.ProductVariantId);
                        if (variant != null)
                        {
                            variant.Quantity += oldItem.Quantity; // رجعنا كل لون ومقاس لمكانه
                            _context.Update(variant);
                        }
                    }

                    // ج- مسح الأصناف القديمة من جدول SalesItems عشان هنضيف الجديد
                    var oldItemsRange = _context.SalesItems.Where(x => x.SalesInvoiceId == id);
                    _context.SalesItems.RemoveRange(oldItemsRange);

                    // د- إضافة الأصناف الجديدة والخصم من الـ Variants من جديد
                    invoice.TotalAmount = 0; // هنحسبه من جديد

                    foreach (var newItem in Items)
                    {
                        var variant = await _context.ProductVariants
                            .Include(v => v.Product)
                            .FirstOrDefaultAsync(v => v.Id == newItem.ProductVariantId);

                        if (variant != null)
                        {
                            if (variant.Quantity >= newItem.Quantity)
                            {
                                // 1. خصم الكمية الجديدة من اللون والمقاس الصح
                                variant.Quantity -= newItem.Quantity;

                                // 2. تثبيت السعر والربط
                                newItem.UnitPrice = variant.Product.Price;
                                newItem.SalesInvoiceId = id;

                                _context.SalesItems.Add(newItem);

                                // 3. تحديث الإجمالي
                                invoice.TotalAmount += (newItem.Quantity * variant.Product.Price);
                            }
                            else
                            {
                                throw new Exception($"الكمية المطلوبة من {variant.Product.Name} غير متوفرة!");
                            }
                        }
                    }

                    _context.Update(invoice);
                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    TempData["Success"] = "تم تعديل الفاتورة وتحديث مخزن الألوان والمقاسات.";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    ModelState.AddModelError("", "حدث خطأ: " + ex.Message);

                    // إعادة تعبئة المنتجات للـ View في حالة الخطأ
                    ViewBag.Products = _context.ProductVariants
                        .Include(v => v.Product).Include(v => v.ProductColor).Include(v => v.ProductSize)
                        .Select(v => new { Id = v.Id, DisplayName = v.Product.Name + " - " + v.ProductColor.Name })
                        .ToList();

                    return View(invoice);
                }
            }
        }

    }
}

