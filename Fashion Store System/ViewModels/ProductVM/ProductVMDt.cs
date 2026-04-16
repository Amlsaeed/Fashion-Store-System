using Fashion_Store_System.Models;
using System.ComponentModel.DataAnnotations.Schema;

namespace Fashion_Store_System.ViewModels.ProductVM
{
    public class ProductVMDt
    {
        public int Id { get; set; }

        public required string Name { get; set; }

        public string? Description { get; set; }
        [Column(TypeName = "decimal(18,2)")]
        public decimal Price { get; set; }

        public int Quantity { get; set; }
        [Column(TypeName = "decimal(18,2)")]
        public decimal Discount { get; set; }

        public string? ImageUrl { get; set; }

        
        public string? CategoryName { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
