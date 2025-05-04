using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace PBL3.Models
{
    // Lớp này kế thừa IdentityUser, chứa các thuộc tính chung
    public class AppUser : IdentityUser
    {
        [Required(ErrorMessage = "Họ tên là bắt buộc.")]
        [StringLength(100, ErrorMessage = "Họ tên không được vượt quá 100 ký tự.")]
        [Display(Name = "Họ và tên")]
        public string FullName { get; set; }

        [Range(0, 120, ErrorMessage = "Tuổi phải nằm trong khoảng từ 0 đến 120.")]
        [Display(Name = "Tuổi")]
        public int Age { get; set; }

        [StringLength(200, ErrorMessage = "Địa chỉ không quá 200 ký tự.")]
        [Display(Name = "Địa chỉ")]
        public string? Address { get; set; } // Cho phép null

        // KHÔNG cần thuộc tính Role ở đây, Identity quản lý riêng
    }
}