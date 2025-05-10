// File: Controllers/FlightSearchController.cs (Sửa lỗi)
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PBL3.Data;
using PBL3.Models;
using PBL3.Models.ViewModels; // *** Đảm bảo using đúng ViewModel ***
using System; // Thêm using System
using System.Linq;
using System.Threading.Tasks;

namespace PBL3.Controllers
{
    public class FlightSearchController : Controller
    {
        private readonly ApplicationDbContext _context;

        public FlightSearchController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        // *** THAY ĐỔI THAM SỐ VÀ LOGIC ĐẦU ***
        public async Task<IActionResult> Index(FlightSearchViewModel searchModel)
        {
            // Không cần kiểm tra IsNullOrEmpty nữa, kiểm tra ID > 0 nếu cần
            if (searchModel.DepartureAirportId <= 0 ||
                searchModel.ArrivalAirportId <= 0 ||
                searchModel.DepartureDate == default(DateTime))
            {
                return RedirectToAction("Index", "Home", new { needsSearch = true });
            }

            // --- SỬA QUERY ---
            var query = _context.Flights
                                .Where(f => f.StartingDestination == searchModel.DepartureAirportId && // So sánh ID
                                            f.ReachingDestination == searchModel.ArrivalAirportId && // So sánh ID
                                            f.StartingTime.Date == searchModel.DepartureDate.Date &&
                                            f.AvailableSeats > 0) // *** LỖI NÀY SẼ FIX Ở VẤN ĐỀ 3 ***
                                .OrderBy(f => f.StartingTime);

            var flights = await query.AsNoTracking().ToListAsync();

            // Truyền lại searchModel để hiển thị lại thông tin tìm kiếm
            ViewData["SearchCriteria"] = searchModel; // Vẫn có thể dùng ViewData nếu muốn

            return View(flights);
        }

        // ... (Action Details giữ nguyên, nhưng sẽ gặp lỗi AvailableSeats)
        // GET: FlightSearch/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var flight = await _context.Flights
                .Include(f => f.Sections)
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.FlightId == id);

            if (flight == null) return NotFound();

            // *** LỖI NÀY SẼ FIX Ở VẤN ĐỀ 3 ***
            // if (flight.AvailableSeats <= 0)
            // {
            //     TempData["ErrorMessage"] = "Chuyến bay này hiện đã hết chỗ.";
            // }

            return View(flight);
        }
    }
}