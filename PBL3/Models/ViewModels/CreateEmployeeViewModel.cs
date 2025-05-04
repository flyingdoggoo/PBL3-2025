using System.ComponentModel.DataAnnotations; // Cần cho Data Annotations

namespace PBL3.Models.ViewModels // Đảm bảo đúng namespace
{
    public class CreateEmployeeViewModel
    {
        [Required(ErrorMessage = "Vui lòng nhập Email.")]
        [EmailAddress(ErrorMessage = "Định dạng email không hợp lệ.")]
        [Display(Name = "Email nhân viên")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập mật khẩu.")]
        [StringLength(100, ErrorMessage = "{0} phải dài ít nhất {2} và tối đa {1} ký tự.", MinimumLength = 6)] // Đặt độ dài tối thiểu nếu cần
        [DataType(DataType.Password)]
        [Display(Name = "Mật khẩu")]
        public string Password { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "Xác nhận mật khẩu")]
        [Compare("Password", ErrorMessage = "Mật khẩu và xác nhận mật khẩu không khớp.")] // So sánh với thuộc tính Password
        public string ConfirmPassword { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập họ tên nhân viên.")]
        [StringLength(100, ErrorMessage = "Họ tên không được vượt quá 100 ký tự.")]
        [Display(Name = "Họ và tên")]
        public string FullName { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập tuổi nhân viên.")]
        [Range(18, 70, ErrorMessage = "Tuổi nhân viên phải từ 18 đến 70.")] // Giả sử tuổi làm việc
        [Display(Name = "Tuổi")]
        public int Age { get; set; }

        [StringLength(200, ErrorMessage = "Địa chỉ không được vượt quá 200 ký tự.")]
        [Display(Name = "Địa chỉ")]
        public string? Address { get; set; } // Cho phép địa chỉ null (?)
    }
}