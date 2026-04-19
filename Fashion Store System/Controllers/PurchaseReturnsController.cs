using Fashion_Store_System.Data;
using Fashion_Store_System.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace Fashion_Store_System.Controllers
{
    public class PurchaseReturnsController : Controller
    {
        private readonly ApplicationDbContext _context;
        public PurchaseReturnsController(ApplicationDbContext context) { _context = context; }

        public async Task<IActionResult> Index()
        {
            var returns = await _context.PurchaseReturns.OrderByDescending(r => r.ReturnDate).ToListAsync();
            return View(returns);
        }

        public IActionResult Create()
        {
            var availableProducts = _context.ProductVariants
         .Include(v => v.Product)
         .Include(v => v.ProductColor)
         .Include(v => v.ProductSize)
         .Where(v => v.Quantity > 0)
         .Select(v => new {
             Id = v.Id, // ده الـ Id بتاع الـ Variant مش المنتج الأساسي
             DisplayName = $"{v.Product.Name} - {v.ProductColor.Name} - {v.ProductSize.Name} (المتاح: {v.Quantity})"
         })
         .ToList();

            ViewBag.ProductVariants = new SelectList(availableProducts, "Id", "DisplayName");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(PurchaseReturn returnInvoice, List<PurchaseReturnItem> Items)
        {
            using (var transaction = await _context.Database.BeginTransactionAsync())
            {
                try
                {
                    returnInvoice.ReturnDate = DateTime.Now;
                    returnInvoice.TotalRefundAmount = Items.Sum(i => i.Quantity * i.UnitPrice);

                    _context.PurchaseReturns.Add(returnInvoice);
                    await _context.SaveChangesAsync();

                    foreach (var item in Items)
                    {
                        // التعديل السحري هنا: بنبحث في جدول الـ Variants
                        // المفروض الـ item هنا يكون جاي فيه الـ ProductVariantId من الفيو
                        var variant = await _context.ProductVariants
                            .Include(v => v.Product) // عشان نجيب الاسم لو هنطلعه في رسالة الخطأ
                            .FirstOrDefaultAsync(v => v.Id == item.ProductVariantId);

                        if (variant != null)
                        {
                            // التأكد إن عندنا بضاعة كفاية من "اللون والمقاس" ده بالذات
                            if (variant.Quantity >= item.Quantity)
                            {
                                variant.Quantity -= item.Quantity; // بننقص المخزن الحقيقي
                                _context.Update(variant);
                            }
                            else
                            {
                                throw new Exception($"الكمية المتاحة من {variant.Product.Name} (لون/مقاس معين) أقل من الكمية المراد إرجاعها!");
                            }
                        }

                        item.PurchaseReturnId = returnInvoice.Id;
                        _context.PurchaseReturnItems.Add(item);
                    }

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    TempData["Success"] = "تم تسجيل المرتجع وتحديث مخزن الألوان والمقاسات بنجاح";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    ModelState.AddModelError("", ex.Message);

                    // إعادة تعبئة القائمة بالـ Variants عشان المنيو تظهر تاني في الـ View
                    ViewBag.ProductVariants = _context.ProductVariants
                        .Include(v => v.Product)
                        .Select(v => new { Id = v.Id, Name = v.Product.Name + " - " + v.Id })
                        .ToList();

                    return View(returnInvoice);
                }
            }
        }
    }
}
