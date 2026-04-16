using System.ComponentModel.DataAnnotations;

namespace Fashion_Store_System.Models
{
    public class SalesReturnItem
    {
        [Key]
        public int Id { get; set; }
        public int SalesReturnId { get; set; }
        public int ProductId { get; set; }
        public int Quantity { get; set; } // الكمية المرتجعة
        public decimal UnitPrice { get; set; }
    }
}
