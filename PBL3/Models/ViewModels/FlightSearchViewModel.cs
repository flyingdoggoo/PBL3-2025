using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;
using PBL3.Models; // Cần using này để tham chiếu đến class Flight

namespace PBL3.Models.ViewModels // *** SỬA NAMESPACE ***
{
    public class FlightSearchViewModel
    {
        public string TripType { get; set; } = "roundtrip";

        [Required(ErrorMessage = "Vui lòng chọn điểm khởi hành")]
        [Display(Name = "Từ")] // Thêm DisplayName
        public int DepartureAirportId { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn điểm đến")]
        [Display(Name = "Đến")] // Thêm DisplayName
        public int ArrivalAirportId { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn ngày đi")]
        [DataType(DataType.Date)]
        [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}", ApplyFormatInEditMode = true)]
        [Display(Name = "Ngày đi")] // Thêm DisplayName
        public DateTime DepartureDate { get; set; } = DateTime.Today; // *** THÊM GIÁ TRỊ MẶC ĐỊNH ***

        [DataType(DataType.Date)]
        [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}", ApplyFormatInEditMode = true)] // Thêm format
        [Display(Name = "Ngày về")] // Thêm DisplayName
        public DateTime? ReturnDate { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn số hành khách")]
        [Range(1, 10, ErrorMessage = "Số hành khách phải từ 1 đến 10")]
        [Display(Name = "Số hành khách")] // Thêm DisplayName
        public int PassengerCount { get; set; } = 1;

        public List<SelectListItem> Airports { get; set; } = new List<SelectListItem>();

        // Nên đặt tên rõ hơn là SearchResults hoặc tách ViewModel
        public List<Flight>? OutboundFlights { get; set; } = new List<Flight>(); // Cho phép null
        public List<Flight>? ReturnFlights { get; set; } = new List<Flight>(); // Cho phép null

        // Constructor để khởi tạo list có thể không cần nếu dùng nullable
        // public FlightSearchViewModel()
        // {
        //     OutboundFlights = new List<Flight>();
        //     ReturnFlights = new List<Flight>();
        // }
    }
}