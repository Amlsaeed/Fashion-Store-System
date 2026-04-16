using Fashion_Store_System.Data;
using Fashion_Store_System.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Fashion_Store_System.Controllers
{
    public class SalesReturnsController : Controller
    {
        private readonly ApplicationDbContext _context;
        public SalesReturnsController(ApplicationDbContext context) { _context = context; }

        public IActionResult Index() => View(_context.SalesReturns.ToList());

        public IActionResult Create() => View();

        // دالة ذكية: بتجيب الأصناف اللي جوه فاتورة بيع معينة
        [HttpGet]
        public async Task<JsonResult> GetInvoiceDetails(int invoiceId)
        {
            var items = await _context.SalesItems
                .Where(i => i.SalesInvoiceId == invoiceId)
                .Select(i => new {
                    productId = i.ProductId,
                    productName = i.Product.Name,
                    quantitySold = i.Quantity,
                    unitPrice = i.UnitPrice
                }).ToListAsync();

            return Json(items);
        }

        [HttpPost]
        public async Task<IActionResult> Create(SalesReturn returnInvoice, List<SalesReturnItem> Items)
        {
            using (var transaction = await _context.Database.BeginTransactionAsync())
            {
                try
                {
                    // تصفية الأصناف: ناخد بس الحاجات اللي اليوزر كتب قدامها كمية مرتجع
                    var filteredItems = Items.Where(i => i.Quantity > 0).ToList();

                    returnInvoice.ReturnDate = DateTime.Now;
                    returnInvoice.TotalRefundAmount = filteredItems.Sum(i => i.Quantity * i.UnitPrice);

                    _context.SalesReturns.Add(returnInvoice);
                    await _context.SaveChangesAsync();

                    foreach (var item in filteredItems)
                    {
                        var product = await _context.Products.FindAsync(item.ProductId);
                        if (product != null) product.Quantity += item.Quantity; // زيادة المخزن

                        item.SalesReturnId = returnInvoice.Id;
                        _context.SalesReturnItems.Add(item);
                    }
                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();
                    return RedirectToAction("Index");
                }
                catch
                {
                    await transaction.RollbackAsync();
                    return View();
                }
            }
        }
    }
}
