
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
        public string PassengerId { get; set; }

        [Required]
        public int FlightId { get; set; }

        [Required(ErrorMessage = "Phải chọn một ghế cho vé.")]
        public int SeatId { get; set; }

        public int? SectionId { get; set; }
        public string? BookingEmployeeId { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Price { get; set; }

        [Required]
        public DateTime OrderTime { get; set; } = DateTime.UtcNow;

        [Required]
        [StringLength(20)]
        public TicketStatus Status { get; set; } = TicketStatus.Pending_Book;
        [ForeignKey("PassengerId")]
        public virtual Passenger? Passenger { get; set; }

        [ForeignKey("FlightId")]
        public virtual Flight? Flight { get; set; }

        [ForeignKey("SeatId")]
        public virtual Seat? Seat { get; set; }

        [ForeignKey("SectionId")]
        public virtual Section? Section { get; set; }

        [ForeignKey("BookingEmployeeId")]
        public virtual Employee? BookingEmployee { get; set; }
    }
}
