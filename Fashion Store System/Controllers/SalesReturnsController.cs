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


    }
}
