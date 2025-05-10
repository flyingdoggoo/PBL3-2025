using System;
using System.Collections.Generic;

namespace PBL3.Models.ViewModels
{
    public class FlightDetailsViewModel
    {
        public int FlightId { get; set; } 
        public string FlightNumber { get; set; } 
        public string AirlineName { get; set; }
        public string DepartureAirportName { get; set; }
        public string ArrivalAirportName { get; set; }   
        public DateTime DepartureTime { get; set; }
        public DateTime ArrivalTime { get; set; }
    }

    public class TicketPaymentViewModel
    {
        public int TicketId { get; set; }
        public string PassengerName { get; set; }
        public string SeatNumber { get; set; }
        public int SeatId { get; set; } 
        public decimal Price { get; set; }
        public string Section { get; set; } 
    }

    public class PaymentViewModel
    {
        public FlightDetailsViewModel FlightInfo { get; set; }

        public List<TicketPaymentViewModel> Tickets { get; set; }

        public decimal Total { get; set; }

        public string BookerName { get; set; }
        public string BookerEmail { get; set; }

        public PaymentViewModel()
        {
            Tickets = new List<TicketPaymentViewModel>();
            FlightInfo = new FlightDetailsViewModel();
        }
    }
}