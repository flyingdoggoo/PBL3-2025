using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace PBL3.Models
{
    public class Ticket
    {
        [Key]
        public int TicketId { get; set; } // Đổi từ String sang int

        // --- Khóa ngoại (Foreign Keys) ---
        [Required]
        public String? PassengerId { get; set; } // Khóa ngoại tham chiếu đến Passenger

        [Required]
        public int FlightId { get; set; } // Khóa ngoại tham chiếu đến Flight

        // Tùy chọn: Khóa ngoại tham chiếu đến Section nếu việc gán ghế là theo Section
        public int? SectionId { get; set; } // Dấu ? cho biết khóa ngoại này có thể là NULL (optional relationship)

        // Tùy chọn: Khóa ngoại tham chiếu đến Employee nếu một nhân viên đã hỗ trợ đặt vé
        public String? BookingEmployeeId { get; set; } // Dấu ? cho biết khóa ngoại này có thể là NULL

        // --- Thuộc tính thông thường ---
        [Required]
        [Column(TypeName = "decimal(18,2)")] // Kiểu dữ liệu phù hợp cho tiền tệ
        [Range(0, (double)decimal.MaxValue)]
        public decimal Price { get; set; } // Giá vé (đổi từ int sang decimal)

        [StringLength(10)] // Ví dụ: "14A", "22B"
        public string SeatNumber { get; set; } // Số ghế (đổi từ int sang string để linh hoạt hơn)

        [Required]
        public DateTime OrderTime { get; set; } // Thời gian đặt vé

        [Required]
        [StringLength(20)]
        public string Status { get; set; } = "Booked"; // Trạng thái vé (ví dụ: Booked, Cancelled, CheckedIn)

        // --- Thuộc tính điều hướng (Navigation Properties) ---

        // Một vé thuộc về một hành khách (Mối quan hệ nhiều-một)
        [ForeignKey("PassengerId")]
        public virtual Passenger Passenger { get; set; }

        // Một vé thuộc về một chuyến bay (Mối quan hệ nhiều-một)
        [ForeignKey("FlightId")]
        public virtual Flight Flight { get; set; }

        // Một vé có thể thuộc về một Section (Mối quan hệ nhiều-một, tùy chọn)
        [ForeignKey("SectionId")]
        public virtual Section Section { get; set; }

        // Một vé có thể được xử lý bởi một nhân viên (Mối quan hệ nhiều-một, tùy chọn)
        [ForeignKey("BookingEmployeeId")]
        public virtual Employee BookingEmployee { get; set; }
    }
}
