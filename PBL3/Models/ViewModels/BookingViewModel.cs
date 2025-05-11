using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace PBL3.Models.ViewModels
{
    public class BookingViewModel
    {
        [Required]
        public int FlightId { get; set; }
        public Flight? FlightInfo { get; set; } // Thông tin chuyến bay (tên, giờ,...)

        // Danh sách các ghế có sẵn với thông tin giá và section
        public List<SeatViewModel> SeatsLayout { get; set; } = new List<SeatViewModel>();

        // Danh sách các section của chuyến bay với thông tin của chúng
        public List<SectionInfoViewModel> FlightSections { get; set; } = new List<SectionInfoViewModel>();


        public List<PassengerBookingInfo> Passengers { get; set; } = new List<PassengerBookingInfo>();
        public decimal EstimatedTotalPrice { get; set; } // Sẽ được tính bằng JS

        // Thêm các thông tin khác cần thiết cho việc hiển thị sơ đồ ghế
        public int? MaxRowBusiness { get; set; } // Hàng cuối cùng của hạng Thương gia
        public int? MaxRowEconomy { get; set; } // Hàng cuối cùng của hạng Phổ thông
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
        public string? Gender { get; set; } // Male, Female, Other

        public string? SelectedSeatNumber { get; set; } // Số ghế hành khách này chọn
        public int? SelectedSeatId { get; set; } // Id của ghế đã chọn (nếu dùng model Seat)
    }

    // ViewModel phụ để chứa thông tin section cần thiết cho view
    public class SectionInfoViewModel
    {
        public string Name { get; set; }
        public decimal PriceMultiplier { get; set; }
    }
}