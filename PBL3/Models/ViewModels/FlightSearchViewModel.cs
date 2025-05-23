using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc.Rendering;
using PBL3.Models;

namespace PBL3.Models.ViewModels
{
    public class FlightSearchViewModel
    {
        public string TripType { get; set; } = "roundtrip";

        [Required(ErrorMessage = "Vui lòng chọn điểm khởi hành")]
        [Display(Name = "Từ")]
        public int DepartureAirportId { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn điểm đến")]
        [Display(Name = "Đến")]
        public int ArrivalAirportId { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn ngày đi")]
        [DataType(DataType.Date)]
        [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}", ApplyFormatInEditMode = true)]
        [Display(Name = "Ngày đi")]
        public DateTime DepartureDate { get; set; } = DateTime.Today;

        [DataType(DataType.Date)]
        [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}", ApplyFormatInEditMode = true)]
        [Display(Name = "Ngày về")]
        public DateTime? ReturnDate { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn số hành khách")]
        [Range(1, 10, ErrorMessage = "Số hành khách phải từ 1 đến 10")]
        [Display(Name = "Số hành khách")]
        public int PassengerCount { get; set; } = 1;

        public List<SelectListItem> Airports { get; set; } = new List<SelectListItem>();
        public List<Flight>? OutboundFlights { get; set; } = new List<Flight>();
        public List<Flight>? ReturnFlights { get; set; } = new List<Flight>();
    }
}