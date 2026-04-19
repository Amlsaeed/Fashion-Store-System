using System.ComponentModel.DataAnnotations;

namespace Fashion_Store_System.Models
{
    public class SalesReturn
    {
        [Key]
        public int Id { get; set; }
        public int ProductVariantId { get; set; }
        public ProductVariant? ProductVariant { get; set; }
        public int SalesInvoiceId { get; set; } // ربط بالفاتورة الأصلية
        public DateTime ReturnDate { get; set; } = DateTime.Now;
        public decimal TotalRefundAmount { get; set; }
        public List<SalesReturnItem> Items { get; set; } = new List<SalesReturnItem>();
    }
}
