using System.ComponentModel.DataAnnotations;

namespace Fashion_Store_System.Models
{
    public class SalesItem
    {
        [Key]
        public int Id { get; set; }

        // ربط الصنف بالفاتورة الأم
        public int ProductVariantId { get; set; }
        public ProductVariant? ProductVariant { get; set; }
        public int SalesInvoiceId { get; set; }
        public SalesInvoice? SalesInvoice { get; set; }

        // ربط الصنف بالمنتج اللي موجود في المخزن
        public int ProductId { get; set; }
        public Product? Product { get; set; }

        public int Quantity { get; set; } // الكمية المباعة
        public decimal UnitPrice { get; set; } // سعر البيع للقطعة وقتها
    }
}
