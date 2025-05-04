// Ticket.cs (Phần xác nhận)
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace PBL3.Models
{
    public class Ticket
    {
        [Key]
        public int TicketId { get; set; }

        // --- Khóa ngoại ---
        [Required]
        public string PassengerId { get; set; } // Khóa ngoại string, non-nullable

        [Required]
        public int FlightId { get; set; }

        public int? SectionId { get; set; }

        public string? BookingEmployeeId { get; set; } // Khóa ngoại string, nullable

        // --- Thuộc tính thông thường ---
        [Required]
        [Column(TypeName = "decimal(18,2)")]
        [Range(0.01, (double)decimal.MaxValue, ErrorMessage = "Giá vé phải lớn hơn 0.")]
        [Display(Name = "Giá vé thực trả")]
        public decimal Price { get; set; } // Giá vé cuối cùng

        [StringLength(10)]
        [Display(Name = "Số ghế")]
        public string SeatNumber { get; set; }

        [Required]
        [Display(Name = "Thời gian đặt")]
        public DateTime OrderTime { get; set; }

        [Required]
        [StringLength(20)]
        [Display(Name = "Trạng thái")]
        public string Status { get; set; } = "Booked";

        // --- Thuộc tính điều hướng ---
        [ForeignKey("PassengerId")]
        public virtual Passenger Passenger { get; set; }

        [ForeignKey("FlightId")]
        public virtual Flight Flight { get; set; }

        [ForeignKey("SectionId")]
        public virtual Section? Section { get; set; } // Cho phép null

        [ForeignKey("BookingEmployeeId")]
        public virtual Employee? BookingEmployee { get; set; } // Cho phép null
    }
}