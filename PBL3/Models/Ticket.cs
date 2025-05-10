// File: Models/Ticket.cs (Sửa đổi)
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PBL3.Models
{
    public enum TicketStatus
    {
        Pending_Book,
        Booked,
        Pending_Cancel,
        Cancelled,
        Completed
    }
    public class Ticket
    {
        [Key]
        public int TicketId { get; set; }

        [Required]
        public string PassengerId { get; set; } // FK đến AppUser.Id

        [Required]
        public int FlightId { get; set; } // FK đến Flight.FlightId

        [Required(ErrorMessage = "Phải chọn một ghế cho vé.")] // **Quan trọng: Vé phải có ghế**
        public int SeatId { get; set; } // **THAY THẾ SeatNumber bằng SeatId (FK)**

        public int? SectionId { get; set; } // Có thể giữ hoặc bỏ nếu Seat đã có SectionId
        public string? BookingEmployeeId { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Price { get; set; }

        [Required]
        public DateTime OrderTime { get; set; } = DateTime.UtcNow;

        [Required]
        [StringLength(20)]
        public TicketStatus Status { get; set; } = TicketStatus.Pending_Book;

        // --- Navigation Properties ---
        [ForeignKey("PassengerId")]
        public virtual Passenger? Passenger { get; set; }

        [ForeignKey("FlightId")]
        public virtual Flight? Flight { get; set; }

        [ForeignKey("SeatId")] // **Thêm ForeignKey cho Seat**
        public virtual Seat? Seat { get; set; } // **Thêm Navigation Property đến Seat**

        [ForeignKey("SectionId")]
        public virtual Section? Section { get; set; }

        [ForeignKey("BookingEmployeeId")]
        public virtual Employee? BookingEmployee { get; set; }
    }
}