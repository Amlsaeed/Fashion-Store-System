using Fashion_Store_System.Models;
using System.ComponentModel.DataAnnotations.Schema;

namespace Fashion_Store_System.ViewModels.ProductVM
{
    public class ProductVM
    {


        public required string Name { get; set; }

        public string? Description { get; set; }
        public decimal Price { get; set; }

        public int Quantity { get; set; }
        public decimal Discount { get; set; }
        public IFormFile? ImageFile { get; set; }

        public int CategoryId { get; set; }

  

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
