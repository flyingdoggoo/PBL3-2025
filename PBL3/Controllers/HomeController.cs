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
        public async Task<IActionResult> Index(bool scrollToSearchForm = false)
        {
            if (scrollToSearchForm)
            {
                ViewData["ScrollToSearchForm"] = true;
            }

            var viewModel = new FlightSearchViewModel
            {
                TripType = "roundtrip",
                DepartureDate = DateTime.Today.AddDays(1),
                PassengerCount = 1,
                Airports = await GetAirportsAsync()
            };
            return View(viewModel);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Search(FlightSearchViewModel model)
        {
            if (model.DepartureAirportId == model.ArrivalAirportId && model.DepartureAirportId != 0)
            {
                ModelState.AddModelError("ArrivalAirportId", "Điểm đến không được trùng với điểm đi.");
            }

            if (model.DepartureDate.Date < DateTime.Today)
            {
                ModelState.AddModelError("DepartureDate", "Ngày đi không được chọn ngày trong quá khứ.");
            }

            if (model.TripType == "roundtrip")
            {
                if (!model.ReturnDate.HasValue)
                {
                    ModelState.AddModelError("ReturnDate", "Vui lòng chọn ngày về cho chuyến bay khứ hồi.");
                }
                else
                {
                    if (model.ReturnDate.Value.Date < DateTime.Today)
                    {
                        ModelState.AddModelError("ReturnDate", "Ngày về không được chọn ngày trong quá khứ.");
                    }
                    if (model.ReturnDate.Value.Date < model.DepartureDate.Date)
                    {
                        ModelState.AddModelError("ReturnDate", "Ngày về phải sau hoặc bằng ngày đi.");
                    }
                }
            }

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Search form validation failed.");
                model.Airports = await GetAirportsAsync();
                ViewData["ScrollToSearchForm"] = true;
                return View("Index", model);
            }

            _logger.LogInformation($"Searching flights: {model.DepartureAirportId} to {model.ArrivalAirportId} on {model.DepartureDate.ToShortDateString()}");
            var departureDateOnly = model.DepartureDate.Date;

            model.OutboundFlights = await _context.Flights
                .Include(f => f.DepartureAirport)
                .Include(f => f.ArrivalAirport)
                .Where(f => f.StartingDestination == model.DepartureAirportId &&
                           f.ReachingDestination == model.ArrivalAirportId &&
                           f.StartingTime.Date == departureDateOnly &&
                           f.AvailableSeats >= model.PassengerCount)
                .OrderBy(f => f.StartingTime)
                .AsNoTracking()
                .ToListAsync();

            _logger.LogInformation($"Found {model.OutboundFlights.Count} outbound flights.");

            if (model.TripType == "roundtrip" && model.ReturnDate.HasValue)
            {
                var returnDateOnly = model.ReturnDate.Value.Date;
                model.ReturnFlights = await _context.Flights
                    .Include(f => f.DepartureAirport)
                    .Include(f => f.ArrivalAirport)
                    .Where(f => f.StartingDestination == model.ArrivalAirportId &&
                               f.ReachingDestination == model.DepartureAirportId &&
                               f.StartingTime.Date == returnDateOnly &&
                               f.AvailableSeats >= model.PassengerCount)
                    .OrderBy(f => f.StartingTime)
                    .AsNoTracking()
                    .ToListAsync();
                _logger.LogInformation($"Found {model.ReturnFlights.Count} return flights.");
            }
            else
            {
                model.ReturnFlights = new List<Flight>();
            }
            var departureAirport = await _context.Airports.AsNoTracking()
                                    .FirstOrDefaultAsync(a => a.Id == model.DepartureAirportId);
            var arrivalAirport = await _context.Airports.AsNoTracking()
                                          .FirstOrDefaultAsync(a => a.Id == model.ArrivalAirportId);
            ViewData["DepartureCityName"] = departureAirport?.City;
            ViewData["ArrivalCityName"] = arrivalAirport?.City;
            return View("SearchResults", model);
        }
        private async Task<List<SelectListItem>> GetAirportsAsync()
        {
            var airports = await _context.Airports
                .OrderBy(a => a.City)
                .ThenBy(a => a.Name)
                .AsNoTracking()
                .ToListAsync();

            return airports.Select(a => new SelectListItem
            {
                Value = a.Id.ToString(),
                Text = $"{a.City} ({a.Code}) - {a.Name}"
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