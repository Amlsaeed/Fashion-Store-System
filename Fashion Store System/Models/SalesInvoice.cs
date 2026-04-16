using System.ComponentModel.DataAnnotations;

namespace Fashion_Store_System.Models
{
    public class SalesInvoice
    {
        [Key]
        public int Id { get; set; }

        [Display(Name = "تاريخ البيع")]
        public DateTime SalesDate { get; set; } = DateTime.Now;

        [Display(Name = "اسم العميل")]
        public string? CustomerName { get; set; } // اختياري

        [Display(Name = "إجمالي الفاتورة")]
        public decimal TotalAmount { get; set; }

        public List<SalesItem> SalesItems { get; set; } = new List<SalesItem>();
    }
}
