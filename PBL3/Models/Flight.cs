// Flight.cs (Phần bổ sung/sửa đổi)
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace PBL3.Models
{
    public class Flight
    {
        [Key]
        public int FlightId { get; set; }

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

        // Thuộc tính Distance có thể giữ hoặc bỏ nếu không dùng
        // [Range(0, int.MaxValue)]
        // public int Distance { get; set; }

        // --- Thuộc tính điều hướng ---
        public virtual ICollection<Section> Sections { get; set; } = new List<Section>();
        public virtual ICollection<Ticket> Tickets { get; set; } = new List<Ticket>();
    }
}