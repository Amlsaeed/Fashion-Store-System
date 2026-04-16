using Fashion_Store_System.Models;

namespace Fashion_Store_System.ViewModels.PurchaseVM
{
    public class PurchaseVM
    {

        public int SupplierId { get; set; }
        public DateTime InvoiceDate { get; set; } = DateTime.Now;

        // قائمة الأصناف اللي هنشتريها (ممكن نشتري كذا صنف في فاتورة واحدة)
        public List<PurchaseItem> Items { get; set; } = new List<PurchaseItem>();
    }
}
