using System.ComponentModel.DataAnnotations;

namespace Fashion_Store_System.Models
{
    public class PurchaseReturn
    {
        [Key]
        public int Id { get; set; }
        public int PurchaseInvoiceId { get; set; } // ربط بالفاتورة الأصلية
        public DateTime ReturnDate { get; set; } = DateTime.Now;
        public decimal TotalRefundAmount { get; set; }
        public List<PurchaseReturnItem> Items { get; set; } = new List<PurchaseReturnItem>();
    }
}
