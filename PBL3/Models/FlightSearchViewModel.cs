using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace PBL3.Models
{
    public class FlightSearchViewModel
    {
        // Loại chuyến bay (Khứ hồi, Một chiều)
        public string TripType { get; set; } = "roundtrip";

        // Sân bay đi
        [Required(ErrorMessage = "Vui lòng chọn điểm khởi hành")]
        public int DepartureAirportId { get; set; }

        // Sân bay đến
        [Required(ErrorMessage = "Vui lòng chọn điểm đến")]
        public int ArrivalAirportId { get; set; }

        // Ngày đi
        [Required(ErrorMessage = "Vui lòng chọn ngày đi")]
        [DataType(DataType.Date)]
        [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}", ApplyFormatInEditMode = true)]
public DateTime DepartureDate { get; set; }


        // Ngày về (chỉ áp dụng cho khứ hồi)
        [DataType(DataType.Date)]
        public DateTime? ReturnDate { get; set; }

        // Số hành khách
        [Required(ErrorMessage = "Vui lòng chọn số hành khách")]
        [Range(1, 10, ErrorMessage = "Số hành khách phải từ 1 đến 10")]
        public int PassengerCount { get; set; } = 1;

        // Danh sách sân bay để hiển thị trong dropdown
        public List<SelectListItem> Airports { get; set; } = new List<SelectListItem>();

        // Kết quả tìm kiếm chuyến bay đi
        public List<Flight> OutboundFlights { get; set; }

        // Kết quả tìm kiếm chuyến bay về (nếu là khứ hồi)
        public List<Flight> ReturnFlights { get; set; }
        public FlightSearchViewModel()
        {
            OutboundFlights = new List<Flight>();
            ReturnFlights = new List<Flight>();
        }
    }
}