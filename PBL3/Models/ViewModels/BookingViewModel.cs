using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace PBL3.Models.ViewModels
{
    public class BookingViewModel
    {
        [Required]
        public int FlightId { get; set; }
        public Flight? FlightInfo { get; set; }
        public List<SeatViewModel> SeatsLayout { get; set; } = new List<SeatViewModel>();
        public List<SectionInfoViewModel> FlightSections { get; set; } = new List<SectionInfoViewModel>();

        public List<PassengerBookingInfo> Passengers { get; set; } = new List<PassengerBookingInfo>();
        public decimal EstimatedTotalPrice { get; set; }

    }

    public class PassengerBookingInfo
    {
        [Required(ErrorMessage = "Vui lòng nhập họ tên.")]
        [Display(Name = "Họ và tên")]
        public string FullName { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập tuổi.")]
        [Range(0, 120, ErrorMessage = "Tuổi không hợp lệ.")]
        [Display(Name = "Tuổi")]
        public int Age { get; set; }

        [Display(Name = "Giới tính")]
        public string? Gender { get; set; }

        public string? SelectedSeatNumber { get; set; }
        public int? SelectedSeatId { get; set; }
    }
    public class SectionInfoViewModel
    {
        public string Name { get; set; }
        public decimal PriceMultiplier { get; set; }
    }
}