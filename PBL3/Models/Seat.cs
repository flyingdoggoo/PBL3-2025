using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PBL3.Models
{

    public class Seat
    {
        [Key]
        public int SeatId { get; set; }

        [Required]
        public int SectionId { get; set; } // Thuộc về Section nào

        [Required]
        [StringLength(5)] // Ví dụ: "1A", "23C"
        [Display(Name = "Số ghế")]
        public string SeatNumber { get; set; } // "1A", "1B", "2C", ...

        [Display(Name = "Hàng")]
        public int Row { get; set; } // Số hàng

        [StringLength(1)]
        [Display(Name = "Cột")]
        public string Column { get; set; } // Chữ cái cột (A, B, C)

        [Required]
        [Display(Name = "Trạng thái")]
        public String Status { get; set; } = "Available";

        // Khóa ngoại
        [ForeignKey("SectionId")]
        public virtual Section? Section { get; set; }

        // Optional: Nếu muốn biết vé nào đã đặt ghế này
        public int? TicketId { get; set; }

        [ForeignKey("TicketId")]
        public virtual Ticket? Ticket { get; set; }
    }
}