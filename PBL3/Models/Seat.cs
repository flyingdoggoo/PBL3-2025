// File: Models/Seat.cs (Cập nhật)
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PBL3.Models
{
    public class Seat
    {
        [Key]
        public int SeatId { get; set; }

        [Required]
        [StringLength(10)]
        public string SeatNumber { get; set; }

        [Required]
        public int FlightId { get; set; }

        public int? SectionId { get; set; }

        [Required]
        [StringLength(20)]
        public string Status { get; set; } = "Available"; // Available, Booked, Unavailable

        public string? SeatType { get; set; } // Window, Aisle, Standard
        public string? Notes { get; set; }

        // --- Navigation Properties ---
        [ForeignKey("FlightId")]
        public virtual Flight? Flight { get; set; }

        [ForeignKey("SectionId")]
        public virtual Section? Section { get; set; }

        // --- BỎ KHÓA NGOẠI ĐẾN TICKET Ở ĐÂY ---
        // public int? TicketId { get; set; }
        // [ForeignKey("TicketId")] // Không cần nếu Ticket giữ FK đến Seat
        // public virtual Ticket? Ticket { get; set; }
    }
}