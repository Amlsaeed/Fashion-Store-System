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
            var invoices = await _context.PurchaseInvoice
                .Include(i => i.Supplier)
                .OrderByDescending(i => i.InvoiceDate)
                .ToListAsync();
            return View(invoices);
        }

        [HttpGet]
        public IActionResult Create()
        {
            ViewBag.Suppliers = _context.Supplier.ToList();

            // تعديل: نجيب الـ Variants عشان نختار المقاس واللون في الشراء
            ViewBag.Products = _context.ProductVariants
                .Include(v => v.Product)
                .Select(v => new {
                    Id = v.Id,
                    Name = v.Product.Name + " - " + v.ProductColor.Name + " (" + v.ProductSize.Name + ")"
                }).ToList();

            return View();
        }

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
                        invoice.TotalAmount = Items.Sum(i => i.Quantity * i.UnitPrice);
                        invoice.InvoiceDate = DateTime.Now;

                        _context.PurchaseInvoice.Add(invoice);
                        await _context.SaveChangesAsync();

                        foreach (var item in Items)
                        {
                            item.PurchaseInvoiceId = invoice.Id;
                            _context.PurchaseItem.Add(item);

                            // تعديل: التعامل مع الـ Variant بدلاً من Product
                            var variant = await _context.ProductVariants.FindAsync(item.ProductVariantId);
                            if (variant != null)
                            {
                                variant.Quantity += item.Quantity;
                                _context.Update(variant);
                            }
                        }

                        await _context.SaveChangesAsync();
                        await transaction.CommitAsync();

                        TempData["Success"] = "تم تسجيل المشتريات وتحديث المخزن.";
                        return RedirectToAction(nameof(Index));
                    }
                    catch (Exception ex)
                    {
                        await transaction.RollbackAsync();
                        ModelState.AddModelError("", "حدث خطأ: " + ex.Message);
                    }
                }
            }

            ViewBag.Suppliers = _context.Supplier.ToList();
            ViewBag.Products = _context.ProductVariants.Include(v => v.Product)
                .Select(v => new { Id = v.Id, Name = v.Product.Name + " - " + v.ProductColor.Name }).ToList();
            return View(invoice);
        }

        // تعديل أكشن الحذف ليتعامل مع الـ Variants
        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var invoice = await _context.PurchaseInvoice
                .Include(i => i.PurchaseItems)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (invoice == null) return Json(new { success = false });

            foreach (var item in invoice.PurchaseItems)
            {
                var variant = await _context.ProductVariants.FindAsync(item.ProductVariantId);
                if (variant != null)
                {
                    variant.Quantity -= item.Quantity; // بنرجع المخزن لأصله
                    _context.Update(variant);
                }
            }

            _context.PurchaseInvoice.Remove(invoice);
            await _context.SaveChangesAsync();
            return Json(new { success = true });
        }

        // تعديل أكشن التعديل (Edit) ليتعامل مع الـ Variants
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, PurchaseInvoice invoice, List<PurchaseItem> Items)
        {
            if (id != invoice.Id) return NotFound();

            using (var transaction = await _context.Database.BeginTransactionAsync())
            {
                try
                {
                    var oldInvoice = await _context.PurchaseInvoice
                        .Include(i => i.PurchaseItems)
                        .AsNoTracking()
                        .FirstOrDefaultAsync(m => m.Id == id);

                    // 1. عكس الكميات القديمة من الـ Variants
                    foreach (var oldItem in oldInvoice.PurchaseItems)
                    {
                        var variant = await _context.ProductVariants.FindAsync(oldItem.ProductVariantId);
                        if (variant != null)
                        {
                            variant.Quantity -= oldItem.Quantity;
                            _context.Update(variant);
                        }
                    }

                    // 2. مسح الأصناف القديمة
                    var oldItems = _context.PurchaseItem.Where(x => x.PurchaseInvoiceId == id);
                    _context.PurchaseItem.RemoveRange(oldItems);

                    // 3. إضافة الجديدة وتحديث المخزن
                    invoice.TotalAmount = Items.Sum(i => i.Quantity * i.UnitPrice);
                    foreach (var newItem in Items)
                    {
                        newItem.PurchaseInvoiceId = id;
                        _context.PurchaseItem.Add(newItem);

                        var variant = await _context.ProductVariants.FindAsync(newItem.ProductVariantId);
                        if (variant != null)
                        {
                            variant.Quantity += newItem.Quantity;
                            _context.Update(variant);
                        }
                    }

                    _context.Update(invoice);
                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    return View(invoice);
                }
            }
        }
    }
}