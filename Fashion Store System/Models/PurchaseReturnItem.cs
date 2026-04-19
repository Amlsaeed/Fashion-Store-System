using System.ComponentModel.DataAnnotations;

namespace Fashion_Store_System.Models
{
    public class PurchaseReturnItem
    {

        [Key]
        public int Id { get; set; }
        public int ProductVariantId { get; set; }
        public int PurchaseReturnId { get; set; }
        public int ProductId { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
    }
}
