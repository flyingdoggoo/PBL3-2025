using System.ComponentModel.DataAnnotations;
using PBL3.Models;

namespace PBL3.Models.ViewModels
{
    public class EditPassengerViewModel
    {
        [Required]
        public string Id { get; set; }

        [Display(Name = "Email (Không thể sửa)")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập họ tên.")]
        [StringLength(100)]
        [Display(Name = "Họ và tên")]
        public string FullName { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập tuổi.")]
        [Range(0, 120, ErrorMessage = "Tuổi không hợp lệ.")]
        [Display(Name = "Tuổi")]
        public int Age { get; set; }

        [StringLength(200)]
        [Display(Name = "Địa chỉ")]
        public string? Address { get; set; }

        [StringLength(50)]
        [Display(Name = "Số hộ chiếu")]
        public string? PassportNumber { get; set; }
    }
}