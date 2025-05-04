using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PBL3.Data; // Namespace DbContext
using PBL3.Models;
using PBL3.Models.ViewModels; // Namespace cho ViewModel tìm kiếm
using System.ComponentModel.DataAnnotations;
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

        // GET: FlightSearch/Index hoặc FlightSearch/Search (khi submit form)
        [HttpGet] // Nhận cả request GET trực tiếp hoặc từ form GET
        public async Task<IActionResult> Index(FlightSearchViewModel searchModel)
        {
            // Chỉ thực hiện tìm kiếm nếu có đủ thông tin cơ bản
            if (string.IsNullOrEmpty(searchModel.DepartureCity) ||
                string.IsNullOrEmpty(searchModel.ArrivalCity) ||
                searchModel.DepartureDate == default(DateTime))
            {
                // Nếu chưa tìm kiếm, chỉ hiển thị form (hoặc quay về Home)
                // return View(new List<Flight>()); // Trả về danh sách rỗng
                return RedirectToAction("Index", "Home", new { needsSearch = true }); // Quay về Home báo cần tìm kiếm
            }

            // --- Thực hiện Query ---
            var query = _context.Flights
                                .Where(f => f.StartingDestination.Contains(searchModel.DepartureCity) &&
                                            f.ReachingDestination.Contains(searchModel.ArrivalCity) &&
                                            f.StartingTime.Date == searchModel.DepartureDate.Date && // So sánh phần Date
                                            f.AvailableSeats > 0) // Chỉ hiển thị chuyến bay còn chỗ
                                .OrderBy(f => f.StartingTime); // Sắp xếp theo giờ khởi hành

            var flights = await query.AsNoTracking().ToListAsync();

            // Truyền lại searchModel để hiển thị lại thông tin tìm kiếm trên View kết quả
            ViewData["SearchCriteria"] = searchModel;

            return View(flights); // Truyền danh sách kết quả vào View
        }


        // GET: FlightSearch/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var flight = await _context.Flights
                .Include(f => f.Sections) // Lấy thông tin sections nếu có
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.FlightId == id);

            if (flight == null)
            {
                return NotFound();
            }
            // Kiểm tra lại xem còn chỗ không trước khi hiển thị chi tiết cho khách
            if (flight.AvailableSeats <= 0)
            {
                TempData["ErrorMessage"] = "Chuyến bay này hiện đã hết chỗ.";
                // Có thể redirect về trang tìm kiếm hoặc hiển thị thông báo trên trang details
            }

            return View(flight);
        }
    }
}

// --- ViewModel cho tìm kiếm (trong Models/ViewModels) ---
namespace PBL3.Models.ViewModels
{
    public class FlightSearchViewModel
    {
        [Required(ErrorMessage = "Vui lòng nhập điểm đi.")]
        [Display(Name = "Điểm đi")]
        public string DepartureCity { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập điểm đến.")]
        [Display(Name = "Điểm đến")]
        public string ArrivalCity { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn ngày đi.")]
        [DataType(DataType.Date)]
        [Display(Name = "Ngày đi")]
        public DateTime DepartureDate { get; set; } = DateTime.Today; // Giá trị mặc định

        // Thêm các tiêu chí khác nếu cần (vd: Số hành khách)
        // [Range(1, 10, ErrorMessage = "Số hành khách từ 1 đến 10.")]
        // [Display(Name = "Số hành khách")]
        // public int Passengers { get; set; } = 1;
    }
}