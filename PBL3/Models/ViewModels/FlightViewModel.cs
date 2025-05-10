using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering; // Cần cho SelectListItem

namespace PBL3.Models.ViewModels
{
    public class FlightViewModel
    {
        public int FlightId { get; set; } // Chỉ cần cho Edit

        [Required(ErrorMessage = "Vui lòng chọn hãng bay.")]
        [Display(Name = "Hãng bay")]
        public string SelectedAirlinePrefix { get; set; } // Lưu Prefix được chọn (VD: "VN", "VJ")

        [Required(ErrorMessage = "Vui lòng nhập số hiệu chuyến bay (chỉ số).")]
        [RegularExpression(@"^\d{1,5}$", ErrorMessage = "Số hiệu chỉ được chứa 1 đến 5 chữ số.")] // Chỉ cho phép nhập số
        [Display(Name = "Số hiệu (Nhập số)")]
        public string FlightNumberSuffix { get; set; } // Lưu phần số (VD: "123", "9011")

        [Required(ErrorMessage = "Vui lòng chọn sân bay đi.")]
        [Display(Name = "Sân bay đi")]
        public int StartingDestination { get; set; } // Lưu Airport ID

        [Required(ErrorMessage = "Vui lòng chọn sân bay đến.")]
        [Display(Name = "Sân bay đến")]
        public int ReachingDestination { get; set; } // Lưu Airport ID

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
        [Range(0.01, (double)decimal.MaxValue, ErrorMessage = "Giá vé phải lớn hơn 0.")]
        [Display(Name = "Giá vé (từ)")]
        [DataType(DataType.Currency)] // Giúp định dạng nếu cần
        public decimal Price { get; set; }

        // Thuộc tính để chứa danh sách cho dropdowns
        public List<SelectListItem> AirlinesList { get; set; } = new List<SelectListItem>();
        public List<SelectListItem> AirportsList { get; set; } = new List<SelectListItem>();

        // Constructor (không bắt buộc, nhưng có thể dùng để khởi tạo list)

        [Display(Name = "Tự động tạo Khu vực Ghế (Thương gia & Phổ thông)")]
        public bool CreateSections { get; set; } = true;

        public FlightViewModel()
        {
            AirlinesList = new List<SelectListItem>();
            AirportsList = new List<SelectListItem>();
        }
    }
}