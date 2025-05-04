using System.ComponentModel.DataAnnotations;

namespace PBL3.Models.ViewModels
{
    public class EditEmployeeViewModel
    {
        [Required]
        public string Id { get; set; } // Cần Id để xác định user

        // Email thường không cho sửa trực tiếp ở form này
        [Display(Name = "Email (Không thể sửa)")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập họ tên.")]
        [StringLength(100)]
        [Display(Name = "Họ và tên")]
        public string FullName { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập tuổi.")]
        [Range(18, 70)] // Tuổi nhân viên
        [Display(Name = "Tuổi")]
        public int Age { get; set; }

        [StringLength(200)]
        [Display(Name = "Địa chỉ")]
        public string? Address { get; set; }
    }
}
