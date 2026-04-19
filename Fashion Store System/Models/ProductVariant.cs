namespace Fashion_Store_System.Models
{
    public class ProductVariant
    {
        public int Id { get; set; }

        // ربط بالمنتج
        public int ProductId { get; set; }
        public  Product? Product { get; set; }

        // ربط بلون المنتج (ProductColor)
        public int ProductColorId { get; set; }
        public  ProductColor? ProductColor { get; set; }

        // ربط بمقاس المنتج (ProductSize)
        public int ProductSizeId { get; set; }
        public  ProductSize? ProductSize { get; set; }

        public int Quantity { get; set; }
    }
}
