using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Collections.Generic; // Cho ICollection

namespace PBL3.Models
{
    public class Section
    {
        [Key]
        public int SectionId { get; set; }

        [Required]
        [StringLength(50)]
        [Display(Name = "Tên khu vực")]
        public string SectionName { get; set; } // Ví dụ: "Thương gia", "Phổ thông"

        [Required]
        [Range(1, 500)]
        [Display(Name = "Sức chứa khu vực")]
        public int Capacity { get; set; }

        // Hệ số nhân giá cho khu vực này so với giá cơ sở của chuyến bay
        [Required]
        [Range(0.1, 10.0)] // Ví dụ: 1.0 cho Phổ thông, 1.8-2.5 cho Thương gia
        [Column(TypeName = "decimal(5,2)")] // Cho phép lưu số thập phân như 1.80
        [Display(Name = "Hệ số giá")]
        public decimal PriceMultiplier { get; set; } = 1.0m; // Mặc định là 1.0

        [Required]
        public int FlightId { get; set; }

        [ForeignKey("FlightId")]
        public virtual Flight? Flight { get; set; }

        // Quan hệ một-nhiều với Seat (nếu bạn muốn quản lý từng ghế riêng)
        // Hoặc bạn có thể không cần DbSet<Seat> nếu chỉ quản lý số lượng
        public virtual ICollection<Seat> Seats { get; set; } = new List<Seat>(); // Thêm Seats
    }
}