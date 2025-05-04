using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PBL3.Data;
using PBL3.Models;
using PBL3.Models.ViewModels;

namespace PBL3.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ApplicationDbContext _context;

        public HomeController(ILogger<HomeController> logger, ApplicationDbContext context)
        {
            _logger = logger;
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var viewModel = new FlightSearchViewModel
            {
                TripType = "roundtrip", // Mặc định là khứ hồi
                DepartureDate = DateTime.Now,
                PassengerCount = 1,
                Airports = await GetAirportsAsync()
            };
            return View(viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> Search(FlightSearchViewModel model)
        {
            

            if (model.DepartureAirportId == model.ArrivalAirportId)
            {
                ModelState.AddModelError("ArrivalAirportId", "Điểm đến không được trùng với điểm đi");
                model.Airports = await GetAirportsAsync();
                return View("Index", model);
            }

            // Kiểm tra ngày đi phải trước ngày về (nếu là khứ hồi)
            if (model.TripType == "roundtrip" && model.ReturnDate.HasValue && model.DepartureDate > model.ReturnDate.Value)
            {
                ModelState.AddModelError("ReturnDate", "Ngày về phải sau ngày đi");
                model.Airports = await GetAirportsAsync();
                return View("Index", model);
            }

            // Mở rộng khoảng ngày tìm kiếm
            var departureStart = model.DepartureDate.Date;
            var departureEnd = model.DepartureDate.Date.AddDays(2);


            // Tìm kiếm chuyến bay đi 
            model.OutboundFlights = await _context.Flights
                .Include(f => f.DepartureAirport)
                .Include(f => f.ArrivalAirport)
                .Where(f => f.StartingDestination == model.DepartureAirportId &&
                           f.ReachingDestination == model.ArrivalAirportId &&
                           f.StartingTime.Date >= departureStart &&
                           f.StartingTime.Date <= departureEnd)
                .ToListAsync();

            // Tìm kiếm chuyến bay về (nếu là khứ hồi)
            if (model.TripType == "roundtrip" && model.ReturnDate.HasValue)
            {
                var returnStart = model.ReturnDate.Value.Date.AddDays(-1);
                var returnEnd = model.ReturnDate.Value.Date.AddDays(1);

                model.ReturnFlights = await _context.Flights
                    .Include(f => f.DepartureAirport)
                    .Include(f => f.ArrivalAirport)
                    .Where(f => f.StartingDestination == model.ArrivalAirportId &&
                               f.ReachingDestination == model.DepartureAirportId &&
                               f.StartingTime.Date >= returnStart &&
                               f.StartingTime.Date <= returnEnd)
                    .ToListAsync();
            }

            model.Airports = await GetAirportsAsync();
            return View("SearchResults", model);
        }
        private async Task<List<SelectListItem>> GetAirportsAsync()
        {
            var airports = await _context.Airports
                .Where(a => a.Country == "Việt Nam")
                .OrderBy(a => a.City)
                .ToListAsync();


            return airports.Select(a => new SelectListItem
            {
                Value = a.Id.ToString(),
                Text = $"{a.City} ({a.Code}), {a.Country}"
            }).ToList();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}