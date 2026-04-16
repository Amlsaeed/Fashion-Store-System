using System.ComponentModel.DataAnnotations;

namespace Fashion_Store_System.Models
{
    public class PurchaseInvoice
    {

        [Key]
        public int Id { get; set; }

        [Display(Name = "تاريخ الفاتورة")]
        public DateTime InvoiceDate { get; set; } = DateTime.Now; // التاريخ

        [Display(Name = "المورد")]
        public int SupplierId { get; set; } // رقم المورد (Foreign Key)
        public Supplier? Supplier { get; set; } // ربط بجدول الموردين

        [Display(Name = "إجمالي الفاتورة")]
        public decimal TotalAmount { get; set; } // مجموع أسعار كل القطع اللي جوه

        public string? Notes { get; set; } // أي ملاحظات (مثلاً: الفاتورة مدفوعة كاش)

        // تفاصيل الفاتورة (الأصناف اللي جوه الورقة)
        public ICollection<PurchaseItem> PurchaseItems { get; set; } = new List<PurchaseItem>();
    }
}
