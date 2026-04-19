using Fashion_Store_System.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Fashion_Store_System.Data
{
    public class ApplicationDbContext : IdentityDbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // تعطيل الحذف التلقائي المتكرر في المشتريات
            modelBuilder.Entity<PurchaseItem>()
                .HasOne(p => p.ProductVariant)
                .WithMany()
                .HasForeignKey(p => p.ProductVariantId)
                .OnDelete(DeleteBehavior.NoAction); // هنا كسرنا الدائرة

            // تعطيل الحذف التلقائي المتكرر في المبيعات
            modelBuilder.Entity<SalesItem>()
                .HasOne(s => s.ProductVariant)
                .WithMany()
                .HasForeignKey(s => s.ProductVariantId)
                .OnDelete(DeleteBehavior.NoAction);

            // تحديد دقة أرقام الـ Decimal (عشان الـ Warnings اللي ظهرت لك)
            foreach (var property in modelBuilder.Model.GetEntityTypes()
                .SelectMany(t => t.GetProperties())
                .Where(p => p.ClrType == typeof(decimal) || p.ClrType == typeof(decimal?)))
            {
                property.SetColumnType("decimal(18,2)");
            }
        }
        public DbSet<Category> Category { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<ProductColor> ProductColors { get; set; }
        public DbSet<ProductSize> ProductSizes { get; set; }
        public DbSet<ProductVariant> ProductVariants { get; set; }
        public DbSet<Stock> Stocks { get; set; }
        public DbSet<Supplier> Supplier { get; set; }
        public DbSet<PurchaseInvoice> PurchaseInvoice { get; set; }
        public DbSet<PurchaseItem> PurchaseItem { get; set; }
        public DbSet<SalesInvoice> SalesInvoices { get; set; }
        public DbSet<SalesItem> SalesItems { get; set; }
        public DbSet<SalesReturn> SalesReturns { get; set; }
        public DbSet<SalesReturnItem> SalesReturnItems { get; set; }
        public DbSet<PurchaseReturn> PurchaseReturns { get; set; }
        public DbSet<PurchaseReturnItem> PurchaseReturnItems { get; set; }
    }
}
