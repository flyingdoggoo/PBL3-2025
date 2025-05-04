using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PBL3.Models
{
    public class Ticket
    {
        [Key]
        public int TicketId { get; set; }

        [Required(ErrorMessage = "Vé phải thuộc về một hành khách.")]
        public string PassengerId { get; set; } // FK đến AppUser.Id (string)

        [Required(ErrorMessage = "Vé phải thuộc về một chuyến bay.")]
        public int FlightId { get; set; } // FK đến Flight.FlightId (int)

        public int? SectionId { get; set; } // FK đến Section.Id (int), nullable
        public string? BookingEmployeeId { get; set; } // FK đến AppUser.Id (string), nullable

        [Required(ErrorMessage = "Vui lòng nhập giá vé.")]
        [Column(TypeName = "decimal(18,2)")]
        [Range(0.01, (double)decimal.MaxValue)]
        [Display(Name = "Giá vé thực trả")]
        public decimal Price { get; set; }

        [StringLength(10)]
        [Display(Name = "Số ghế")]
        public string? SeatNumber { get; set; } // Cho phép null

        [Required]
        [Display(Name = "Thời gian đặt")]
        public DateTime OrderTime { get; set; } = DateTime.UtcNow; // Giá trị mặc định

        [Required(ErrorMessage = "Trạng thái vé là bắt buộc.")]
        [StringLength(20)]
        [Display(Name = "Trạng thái")]
        public string Status { get; set; } = "Booked"; // Giá trị mặc định

        // --- Navigation Properties ---
        [ForeignKey("PassengerId")]
        public virtual Passenger? Passenger { get; set; } // Nên là Passenger thay vì AppUser

        [ForeignKey("FlightId")]
        public virtual Flight? Flight { get; set; }

        [ForeignKey("SectionId")]
        public virtual Section? Section { get; set; }

        [ForeignKey("BookingEmployeeId")]
        public virtual Employee? BookingEmployee { get; set; }
    }
}