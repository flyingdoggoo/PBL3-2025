using static System.Collections.Specialized.BitVector32;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace PBL3.Models
{
    public class Flight
    {
        [Key]
        public int FlightId { get; set; } // Đổi từ String sang int


        [Required]
        [StringLength(20, ErrorMessage = "Số hiệu chuyến bay là bắt buộc và không quá 20 ký tự.")]
        public string FlightNumber { get; set; } // Thêm tên rõ ràng hơn (ví dụ: 'VN123')

        [Range(1, 1000)]
        public int Capacity { get; set; } // Tổng sức chứa (có thể tính từ các Section)

        [Required]
        public DateTime StartingTime { get; set; } // Thời gian khởi hành

        [Required]
        public DateTime ReachingTime { get; set; } // Thời gian đến nơi

        [ForeignKey("DepartureAirport")]
        public int StartingDestination { get; set; }

        [DeleteBehavior(DeleteBehavior.NoAction)]
        public virtual Airport DepartureAirport { get; set; }

        [ForeignKey("ArrivalAirport")]
        public int ReachingDestination { get; set; }

        [DeleteBehavior(DeleteBehavior.NoAction)]
        public virtual Airport ArrivalAirport { get; set; }

        [Range(0, int.MaxValue)]
        public int Distance { get; set; } 

        public int BasePrice { get; set; }
        // --- Thuộc tính điều hướng (Navigation Properties) ---

        // Một chuyến bay có nhiều khu vực chỗ ngồi (Mối quan hệ một-nhiều)
        public virtual ICollection<Section> Sections { get; set; } = new List<Section>();

        // Một chuyến bay liên quan đến nhiều vé (Mối quan hệ một-nhiều)
        public virtual ICollection<Ticket> Tickets { get; set; } = new List<Ticket>();
    }
}
