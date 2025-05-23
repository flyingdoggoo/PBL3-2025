using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PBL3.Models
{

    public class Seat
    {
        [Key]
        public int SeatId { get; set; }

        [Required]
        public int SectionId { get; set; }

        [Required]
        [StringLength(5)]
        [Display(Name = "Số ghế")]
        public string SeatNumber { get; set; }

        [Display(Name = "Hàng")]
        public int Row { get; set; }

        [StringLength(1)]
        [Display(Name = "Cột")]
        public string Column { get; set; }

        [Required]
        [Display(Name = "Trạng thái")]
        public String Status { get; set; } = "Available";

        [ForeignKey("SectionId")]
        public virtual Section? Section { get; set; }

        public int? TicketId { get; set; }

        [ForeignKey("TicketId")]
        public virtual Ticket? Ticket { get; set; }
    }
}
