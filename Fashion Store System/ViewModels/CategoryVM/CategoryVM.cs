namespace Fashion_Store_System.ViewModels.CategoryVM
{
    public class CategoryVM
    {
   

        public required string Name { get; set; }

        public IFormFile? ImageFile { get; set; }
        public string? ImageUrl { get; set; }

        public bool IsActive { get; set; } = true;


    }
}
