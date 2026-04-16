using Fashion_Store_System.Data;
using Fashion_Store_System.Models;
using Microsoft.AspNetCore.Mvc;
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
            // بنجيب بس اللي فيه كمية عشان نعرف نرجعه للمورد
            ViewBag.Products = _context.Products.Where(p => p.Quantity > 0).ToList();
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
                        var product = await _context.Products.FindAsync(item.ProductId);
                        if (product != null)
                        {
                            // التأكد إن عندنا بضاعة كفاية نرجعها
                            if (product.Quantity >= item.Quantity)
                            {
                                product.Quantity -= item.Quantity; // نقصنا المخزن لأننا رجعناها للمورد
                            }
                            else
                            {
                                throw new Exception($"الكمية المتاحة من {product.Name} أقل من الكمية المراد إرجاعها!");
                            }
                        }

                        item.PurchaseReturnId = returnInvoice.Id;
                        _context.PurchaseReturnItems.Add(item);
                    }

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();
                    TempData["Success"] = "تم تسجيل مرتجع المشتريات وخصم المخزن بنجاح";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    ModelState.AddModelError("", ex.Message);
                    ViewBag.Products = _context.Products.ToList();
                    return View(returnInvoice);
                }
            }
        }
    }
}
