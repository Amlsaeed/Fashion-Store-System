using System.ComponentModel.DataAnnotations;

namespace Fashion_Store_System.Models
{
    public class Supplier
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "اسم المورد مطلوب")]
        public required string Name { get; set; } // اسم المصنع أو التاجر

        public string? Phone { get; set; } // رقم تليفونه عشان نكلمه

        public string? Address { get; set; } // عنوانه

        // قائمة الفواتير اللي جت من المورد ده (علاقة)
    }
}
