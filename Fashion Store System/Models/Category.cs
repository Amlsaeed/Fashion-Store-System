using System.ComponentModel.DataAnnotations;

namespace Fashion_Store_System.Models
{
    public class Category
    {
        [Key]
        public int Id { get; set; }

        public required string Name { get; set; }

        public string? ImageUrl { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Navigation Property
        public ICollection<Product>? Products { get; set; }
    }
}
