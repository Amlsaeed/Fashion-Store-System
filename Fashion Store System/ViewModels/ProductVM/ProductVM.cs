using Fashion_Store_System.Models;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations.Schema;

namespace Fashion_Store_System.ViewModels.ProductVM
{
    public class ProductVM
    {

        public int Id { get; set; }
        public  string? Name { get; set; }

        public string? Description { get; set; }
        public decimal Price { get; set; }

        public int Quantity { get; set; }
        public decimal Discount { get; set; }
        public IFormFile? ImageFile { get; set; }

        public int CategoryId { get; set; }

  

        public bool IsActive { get; set; } = true;

        public int SelectedColorId { get; set; }

        // قائمة الألوان اللي هتظهر لليوزر يختار منها (أسود، أحمر، إلخ)
        public IEnumerable<SelectListItem>? ColorList { get; set; }

        // ونفس الكلام للمقاسات
        public int SelectedSizeId { get; set; }
        public IEnumerable<SelectListItem>? SizeList { get; set; }

        public List<int> SelectedColorIds { get; set; } = new List<int>();
        public List<int> SelectedSizeIds { get; set; } = new List<int>();
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
