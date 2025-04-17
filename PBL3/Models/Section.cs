using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace PBL3.Models
{
    public class Section
    {
        [Key]
        public int SectionId { get; set; } // Đổi từ String sang int

        [Required]
        [StringLength(50)]
        public string SectionName { get; set; } // Ví dụ: "Economy", "Business", "First Class" (thay thế SectionA/B/C)

        [Range(1, 500)]
        public int Capacity { get; set; } // Sức chứa của khu vực này

        // --- Khóa ngoại (Foreign Keys) ---
        [Required]
        public int FlightId { get; set; } // Khóa ngoại tham chiếu đến Flight

        // --- Thuộc tính điều hướng (Navigation Properties) ---

        // Một Section thuộc về một Flight (Mối quan hệ nhiều-một)
        [ForeignKey("FlightId")] // Chỉ rõ ràng khóa ngoại cho thuộc tính điều hướng bên dưới
        public virtual Flight Flight { get; set; }

        // Một Section có thể có nhiều vé được đặt trong đó (Mối quan hệ một-nhiều, tùy chọn)
        // public virtual ICollection<Ticket> TicketsInSection { get; set; } = new List<Ticket>();
    }
}
