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

        // GET: Home/Index
        public async Task<IActionResult> Index(bool scrollToSearchForm = false)
        {
            // Cờ scrollToSearchForm sẽ được JavaScript trên View Index sử dụng
            // để cuộn đến form tìm kiếm nếu người dùng được redirect về đây do lỗi validation.
            if (scrollToSearchForm)
            {
                ViewData["ScrollToSearchForm"] = true;
            }

            var viewModel = new FlightSearchViewModel
            {
                TripType = "roundtrip", // Mặc định là khứ hồi
                DepartureDate = DateTime.Today.AddDays(1), // Mặc định ngày mai để tránh lỗi ngày quá khứ
                // ReturnDate = DateTime.Today.AddDays(8), // Tùy chọn: Mặc định ngày về
                PassengerCount = 1,
                Airports = await GetAirportsAsync()
            };
            return View(viewModel);
        }

        // POST: Home/Search
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Search(FlightSearchViewModel model)
        {
            // --- VALIDATION LOGIC ---
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
                    if (model.ReturnDate.Value.Date < model.DepartureDate.Date) // So sánh Date chính xác hơn
                    {
                        ModelState.AddModelError("ReturnDate", "Ngày về phải sau hoặc bằng ngày đi.");
                    }
                }
            }

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Search form validation failed.");
                model.Airports = await GetAirportsAsync(); // Nạp lại danh sách sân bay
                ViewData["ScrollToSearchForm"] = true; // Yêu cầu cuộn lại form trên view Index
                return View("Index", model); // Trả về view Index với các lỗi validation
            }

            _logger.LogInformation($"Searching flights: {model.DepartureAirportId} to {model.ArrivalAirportId} on {model.DepartureDate.ToShortDateString()}");

            // --- QUERY LOGIC ---
            var departureDateOnly = model.DepartureDate.Date;

            model.OutboundFlights = await _context.Flights
                .Include(f => f.DepartureAirport) // Include để hiển thị tên sân bay
                .Include(f => f.ArrivalAirport)
                .Where(f => f.StartingDestination == model.DepartureAirportId &&
                           f.ReachingDestination == model.ArrivalAirportId &&
                           f.StartingTime.Date == departureDateOnly && // Chỉ tìm trong ngày đi đã chọn
                           f.AvailableSeats >= model.PassengerCount)    // Kiểm tra đủ ghế
                .OrderBy(f => f.StartingTime) // Sắp xếp theo giờ khởi hành
                .AsNoTracking() // Tối ưu cho query chỉ đọc
                .ToListAsync();

            _logger.LogInformation($"Found {model.OutboundFlights.Count} outbound flights.");

            if (model.TripType == "roundtrip" && model.ReturnDate.HasValue)
            {
                var returnDateOnly = model.ReturnDate.Value.Date;
                model.ReturnFlights = await _context.Flights
                    .Include(f => f.DepartureAirport)
                    .Include(f => f.ArrivalAirport)
                    .Where(f => f.StartingDestination == model.ArrivalAirportId && // Đảo ngược điểm đi/đến
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
                model.ReturnFlights = new List<Flight>(); // Khởi tạo rỗng nếu là một chiều
            }

            // Không cần load lại model.Airports ở đây vì SearchResults ViewModel đã có nó
            // model.Airports = await GetAirportsAsync();
            return View("SearchResults", model);
        }

        // Helper function để lấy danh sách sân bay
        private async Task<List<SelectListItem>> GetAirportsAsync()
        {
            // Cache danh sách sân bay nếu có thể để tăng hiệu suất
            // Ở đây làm đơn giản là query mỗi lần
            var airports = await _context.Airports
                // .Where(a => a.Country == "Việt Nam") // Bỏ lọc theo quốc gia nếu muốn hiển thị tất cả
                .OrderBy(a => a.City)
                .ThenBy(a => a.Name)
                .AsNoTracking()
                .ToListAsync();

            return airports.Select(a => new SelectListItem
            {
                Value = a.Id.ToString(),
                Text = $"{a.City} ({a.Code}) - {a.Name}" // Hiển thị rõ ràng hơn
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