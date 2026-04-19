using System.ComponentModel.DataAnnotations;

namespace Fashion_Store_System.Models
{
    public class PurchaseItem
    {
        public int Id { get; set; }
        public int ProductVariantId { get; set; } // تأكدي من وجود هذا السطر
        public ProductVariant? ProductVariant { get; set; }

        public int PurchaseInvoiceId { get; set; } // تتبع أنهي فاتورة
        public PurchaseInvoice? PurchaseInvoice { get; set; }

        [Display(Name = "المنتج")]
        public int ProductId { get; set; } // المنتج اللي اشتريناه (Foreign Key لجدول المنتجات)
        public Product? Product { get; set; }

        [Display(Name = "الكمية")]
        public int Quantity { get; set; } // الكمية اللي دخلت المخزن جديد

        [Display(Name = "سعر التكلفة")]
        public decimal UnitPrice { get; set; } // السعر اللي اشترينا بيه (عشان نحسب المكسب بعدين)
    }
}
