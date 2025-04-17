using static System.Collections.Specialized.BitVector32;
using System.ComponentModel.DataAnnotations;

namespace PBL3.Models
{
    public class Flight
    {
        [Key]
        public int FlightId { get; set; } // Đổi từ String sang int

        // Có thể dùng String nếu mã chuyến bay là dạng 'VN123'
        // [Key]
        // [StringLength(10)]
        // public string FlightNumber { get; set; }

        [Required]
        [StringLength(20, ErrorMessage = "Số hiệu chuyến bay là bắt buộc và không quá 20 ký tự.")]
        public string FlightNumber { get; set; } // Thêm tên rõ ràng hơn (ví dụ: 'VN123')

        [Range(1, 1000)]
        public int Capacity { get; set; } // Tổng sức chứa (có thể tính từ các Section)

        [Required]
        public DateTime StartingTime { get; set; } // Thời gian khởi hành

        [Required]
        public DateTime ReachingTime { get; set; } // Thời gian đến nơi

        [Required]
        [StringLength(100)]
        public string StartingDestination { get; set; } // Điểm khởi hành

        [Required]
        [StringLength(100)]
        public string ReachingDestination { get; set; } // Điểm đến

        [Range(0, int.MaxValue)]
        public int Distance { get; set; } // Khoảng cách (ví dụ: km)

        // --- Thuộc tính điều hướng (Navigation Properties) ---

        // Một chuyến bay có nhiều khu vực chỗ ngồi (Mối quan hệ một-nhiều)
        public virtual ICollection<Section> Sections { get; set; } = new List<Section>();

        // Một chuyến bay liên quan đến nhiều vé (Mối quan hệ một-nhiều)
        public virtual ICollection<Ticket> Tickets { get; set; } = new List<Ticket>();
    }
}
