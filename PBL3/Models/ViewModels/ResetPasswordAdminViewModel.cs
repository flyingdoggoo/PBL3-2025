using System.ComponentModel.DataAnnotations;

namespace PBL3.Models.ViewModels
{
    public class ResetPasswordAdminViewModel
    {
        [Required]
        public string UserId { get; set; }

        [Display(Name = "Email Nhân viên")]
        public string Email { get; set; } 

        [Required(ErrorMessage = "Vui lòng nhập mật khẩu mới.")]
        [StringLength(100, ErrorMessage = "{0} phải dài ít nhất {2} ký tự.", MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "Mật khẩu mới")]
        public string NewPassword { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "Xác nhận mật khẩu mới")]
        [Compare("NewPassword", ErrorMessage = "Mật khẩu mới và xác nhận không khớp.")]
        public string ConfirmPassword { get; set; }
    }
}
