using System;
using System.Collections.Generic;
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

        [Required(ErrorMessage = "Vui lòng nhập sân bay đi.")]
        [Display(Name = "Sân bay đi")]
        public int StartingDestination { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập sân bay đến.")]
        [Display(Name = "Sân bay đến")]
        public int ReachingDestination { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập thời gian khởi hành.")]
        [Display(Name = "Thời gian khởi hành")]
        public DateTime StartingTime { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập thời gian đến.")]
        [Display(Name = "Thời gian đến")]
        public DateTime ReachingTime { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập sức chứa.")]
        [Range(1, 1000, ErrorMessage = "Sức chứa phải từ 1 đến 1000.")]
        [Display(Name = "Tổng số ghế")]
        public int Capacity { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập giá vé cơ bản.")]
        [Column(TypeName = "decimal(18, 2)")]
        [Range(0.01, (double)decimal.MaxValue, ErrorMessage = "Giá vé phải lớn hơn 0.")]
        [Display(Name = "Giá vé (từ)")]
        public decimal Price { get; set; }
        [StringLength(100)]
        [Display(Name = "Hãng bay")]
        public string Airline { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập số ghế còn trống.")]
        [Range(0, 1000, ErrorMessage = "Số ghế trống phải từ 0.")]
        [Display(Name = "Số ghế còn trống")]
        public int AvailableSeats { get; set; }

        [Display(Name = "Khoảng cách (km)")]
        public int Distance { get; set; }
        [ForeignKey("StartingDestination")]
        public virtual Airport? DepartureAirport { get; set; }

        [ForeignKey("ReachingDestination")]
        public virtual Airport? ArrivalAirport { get; set; }

        public virtual ICollection<Section> Sections { get; set; } = new List<Section>();
        public virtual ICollection<Ticket> Tickets { get; set; } = new List<Ticket>();
    }
}
