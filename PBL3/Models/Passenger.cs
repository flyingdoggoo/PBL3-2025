using System.ComponentModel.DataAnnotations;
using System.Net.Sockets;

namespace PBL3.Models
{
    public class Passenger : AppUser
    {
        //[Key] // Đánh dấu đây là khóa chính
        //public int PassengerId { get; set; } // Đổi từ String sang int để làm khóa chính thông thường

        // Tùy chọn: Liên kết tới người dùng ASP.NET Core Identity
        // public string ApplicationUserId { get; set; }
        // [ForeignKey("ApplicationUserId")]
        // public virtual ApplicationUser IdentityUser { get; set; } // Giả sử ApplicationUser là lớp người dùng Identity của bạn

        //[Required(ErrorMessage = "Tên hành khách là bắt buộc.")] // Ràng buộc không được để trống
        //[StringLength(100, ErrorMessage = "Tên hành khách không được vượt quá 100 ký tự.")] // Ràng buộc độ dài chuỗi
        //public string PassengerName { get; set; }

        //[Range(0, 120, ErrorMessage = "Tuổi phải nằm trong khoảng từ 0 đến 120.")] // Ràng buộc khoảng giá trị
        //public int PassengerAge { get; set; }

        //[StringLength(200)]
        //public string Address { get; set; } // Địa chỉ

        //[EmailAddress(ErrorMessage = "Định dạng email không hợp lệ.")] // Ràng buộc định dạng email
        //[StringLength(100)]
        //public string PassengerEmail { get; set; }

        // Thông tin nhạy cảm như CreditCardNumber thường không lưu trực tiếp. Bỏ qua trong model đơn giản này.
        // public int CreditCardNumber { get; set; }

        [StringLength(50)]
        public string PassportNumber { get; set; } // Số hộ chiếu

        // --- Thuộc tính điều hướng (Navigation Properties) ---

        // Một hành khách có thể có nhiều vé (Mối quan hệ một-nhiều)
        public virtual ICollection<Ticket> Tickets { get; set; } = new List<Ticket>();

    }
}
