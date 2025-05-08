// File: Models/ViewModels/BookingViewModel.cs
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace PBL3.Models.ViewModels
{
    public class BookingViewModel
    {
        [Required]
        public int FlightId { get; set; }

        // Thông tin chuyến bay (để hiển thị lại)
        public Flight? FlightInfo { get; set; } // Truyền từ Controller sang View

        // Thông tin người đặt vé (lấy từ user đăng nhập)
        public AppUser? BookerInfo { get; set; }

        // Danh sách thông tin cho từng hành khách (có thể bao gồm cả người đặt vé)
        [Required]
        [MinLength(1, ErrorMessage = "Phải có ít nhất 1 hành khách.")]
        public List<PassengerInfoViewModel> Passengers { get; set; } = new List<PassengerInfoViewModel>();

        // Danh sách tất cả ghế và trạng thái của chuyến bay (truyền từ Controller)
        public List<SeatViewModel> AvailableSeats { get; set; } = new List<SeatViewModel>();

        // Tổng tiền ước tính (tính trong Controller)
        public decimal EstimatedTotalPrice { get; set; }

        // Các thông tin khác nếu cần (vd: mã giảm giá)
    }

    // Thông tin cho mỗi hành khách trong danh sách đặt vé
    public class PassengerInfoViewModel
    {
        [Required(ErrorMessage = "Vui lòng nhập họ tên hành khách.")]
        [Display(Name = "Họ tên")]
        public string FullName { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập tuổi.")]
        [Range(0, 120)]
        [Display(Name = "Tuổi")]
        public int Age { get; set; }

        [Display(Name = "Giới tính")] // Ví dụ thêm giới tính
        public string? Gender { get; set; } // Male, Female, Other

        [Display(Name = "Số ghế đã chọn")]
        public string? SelectedSeat { get; set; } // Lưu số ghế dạng "1A", "10F"

        // Thêm các trường khác nếu cần (vd: thông tin liên hệ riêng)
    }

    // ViewModel để hiển thị thông tin ghế trên giao diện chọn
    public class SeatViewModel
    {
        public int SeatId { get; set; }
        public string SeatNumber { get; set; }
        public string Status { get; set; } // Available, Booked, Unavailable
        public string? SeatType { get; set; } // Window, Aisle, Standard
        public int Row { get; set; } // Số hàng (để dễ sắp xếp)
        public string Column { get; set; } // Chữ cái cột (để dễ sắp xếp)
    }
}