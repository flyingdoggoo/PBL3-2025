// Flight.cs (Phần bổ sung/sửa đổi)
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PBL3.Models
{
    public class Flight
    {
        [Key]
        public int FlightId { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập số hiệu chuyến bay.")]
        [StringLength(20, ErrorMessage = "Số hiệu chuyến bay không quá 20 ký tự.")]
        [Display(Name = "Số hiệu chuyến bay")]
        public string FlightNumber { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập sức chứa.")]
        [Range(1, 1000, ErrorMessage = "Sức chứa phải từ 1 đến 1000.")]
        [Display(Name = "Tổng số ghế (Sức chứa)")]
        public int Capacity { get; set; } // Đây là tổng số ghế

        [Required(ErrorMessage = "Vui lòng nhập thời gian khởi hành.")]
        [Display(Name = "Thời gian khởi hành")]
        public DateTime StartingTime { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập thời gian đến.")]
        [Display(Name = "Thời gian đến")]
        public DateTime ReachingTime { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập điểm đi.")]
        [StringLength(100)]
        [Display(Name = "Điểm đi")]
        public string StartingDestination { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập điểm đến.")]
        [StringLength(100)]
        [Display(Name = "Điểm đến")]
        public string ReachingDestination { get; set; }

        // --- Thuộc tính mới/quan trọng ---
        [Required(ErrorMessage = "Vui lòng nhập hãng bay.")]
        [StringLength(100)]
        [Display(Name = "Hãng bay")]
        public string Airline { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập giá vé cơ bản.")]
        [Column(TypeName = "decimal(18, 2)")]
        [Range(0.01, (double)decimal.MaxValue, ErrorMessage = "Giá vé phải lớn hơn 0.")]
        [Display(Name = "Giá vé cơ bản (Từ)")]
        public decimal Price { get; set; } // Giá tham chiếu

        [Required(ErrorMessage = "Vui lòng nhập số ghế còn trống.")]
        [Range(0, 1000, ErrorMessage = "Số ghế trống phải từ 0 đến 1000.")] // Nên <= Capacity
        [Display(Name = "Số ghế còn trống")]
        public int AvailableSeats { get; set; }

        // Thuộc tính Distance có thể giữ hoặc bỏ nếu không dùng
        // [Range(0, int.MaxValue)]
        // public int Distance { get; set; }

        // --- Thuộc tính điều hướng ---
        public virtual ICollection<Section> Sections { get; set; } = new List<Section>();
        public virtual ICollection<Ticket> Tickets { get; set; } = new List<Ticket>();
    }
}