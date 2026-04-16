namespace Fashion_Store_System.ViewModels.CategoryVM
{
    public class CategoryVMDt
    {
        public int Id { get; set; }

        public required string Name { get; set; }

        public string? ImageUrl { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.Now;

    }
}
