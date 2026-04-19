using System.ComponentModel.DataAnnotations;

namespace Fashion_Store_System.Models
{
    public class ProductColor
    {
        public int Id { get; set; }
        [Required]
        public required string Name { get; set; } 
    }
}
