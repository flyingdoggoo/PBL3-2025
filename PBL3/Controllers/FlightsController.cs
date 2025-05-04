using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PBL3.Data; // Namespace chứa DbContext
using PBL3.Models;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using System.Linq;

[Authorize(Roles = "Admin,Employee")] // Admin và Employee có thể quản lý chuyến bay
public class FlightsController : Controller
{
    private readonly ApplicationDbContext _context;

    public FlightsController(ApplicationDbContext context)
    {
        _context = context;
    }

    // GET: Flights
    public async Task<IActionResult> Index(string sortOrder, string searchString)
    {
        ViewData["NumberSortParm"] = String.IsNullOrEmpty(sortOrder) ? "number_desc" : "";
        ViewData["DateSortParm"] = sortOrder == "Date" ? "date_desc" : "Date";
        ViewData["AirlineSortParm"] = sortOrder == "Airline" ? "airline_desc" : "Airline";
        ViewData["PriceSortParm"] = sortOrder == "Price" ? "price_desc" : "Price";
        ViewData["CurrentFilter"] = searchString;

        var flights = _context.Flights
                          .Include(f => f.DepartureAirport) // *** THÊM INCLUDE ***
                          .Include(f => f.ArrivalAirport)   // *** THÊM INCLUDE ***
                          .AsQueryable();

        if (!String.IsNullOrEmpty(searchString))
        {
            flights = flights.Where(f =>
            f.FlightNumber.Contains(searchString) || // Tìm theo số hiệu
            f.Airline.Contains(searchString) ||      // Tìm theo hãng
            (f.DepartureAirport != null &&           // Tìm theo tên/mã sân bay đi (kiểm tra null)
                (f.DepartureAirport.City.Contains(searchString) || f.DepartureAirport.Code.Contains(searchString))) ||
            (f.ArrivalAirport != null &&             // Tìm theo tên/mã sân bay đến (kiểm tra null)
                (f.ArrivalAirport.City.Contains(searchString) || f.ArrivalAirport.Code.Contains(searchString)))
        );
        }

        switch (sortOrder)
        {
            case "number_desc":
                flights = flights.OrderByDescending(f => f.FlightNumber);
                break;
            case "Date":
                flights = flights.OrderBy(f => f.StartingTime);
                break;
            case "date_desc":
                flights = flights.OrderByDescending(f => f.StartingTime);
                break;
            case "Airline":
                flights = flights.OrderBy(f => f.Airline);
                break;
            case "airline_desc":
                flights = flights.OrderByDescending(f => f.Airline);
                break;
            case "Price":
                flights = flights.OrderBy(f => f.Price);
                break;
            case "price_desc":
                flights = flights.OrderByDescending(f => f.Price);
                break;
            default: // Sort by FlightNumber ascending by default
                flights = flights.OrderBy(f => f.FlightNumber);
                break;
        }

        return View(await flights.AsNoTracking().ToListAsync()); // AsNoTracking() tối ưu cho đọc dữ liệu
    }

    // GET: Flights/Details/5
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null) return NotFound();
        var flight = await _context.Flights
                                     .Include(f => f.Sections) // Lấy thông tin Sections nếu cần hiển thị
                                     .AsNoTracking() // Tối ưu đọc
                                     .FirstOrDefaultAsync(m => m.FlightId == id);
        if (flight == null) return NotFound();
        return View(flight);
    }

    // GET: Flights/Create
    public IActionResult Create()
    {
        // Cung cấp giá trị mặc định hợp lý
        var flight = new Flight
        {
            StartingTime = DateTime.Now.Date.AddDays(1).AddHours(9), // Mặc định ngày mai lúc 9h
            ReachingTime = DateTime.Now.Date.AddDays(1).AddHours(11), // Mặc định ngày mai lúc 11h
            Capacity = 100, // Mặc định sức chứa
            AvailableSeats = 100 // Ban đầu số ghế trống bằng sức chứa
        };
        return View(flight);
    }

    // POST: Flights/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    // Bind các thuộc tính cần thiết và được phép từ form
    public async Task<IActionResult> Create([Bind("FlightNumber,Capacity,StartingTime,ReachingTime,StartingDestination,ReachingDestination,Airline,Price,AvailableSeats")] Flight flight)
    {
        // --- Kiểm tra validation logic nghiệp vụ ---
        if (flight.StartingTime >= flight.ReachingTime)
        {
            ModelState.AddModelError("ReachingTime", "Thời gian đến phải sau thời gian khởi hành.");
        }
        if (flight.AvailableSeats > flight.Capacity)
        {
            ModelState.AddModelError("AvailableSeats", "Số ghế còn lại không được lớn hơn tổng số ghế (sức chứa).");
        }
        // Có thể kiểm tra thêm thời gian khởi hành phải ở tương lai
        if (flight.StartingTime <= DateTime.Now)
        {
            ModelState.AddModelError("StartingTime", "Thời gian khởi hành phải ở tương lai.");
        }

        // Kiểm tra ModelState sau khi thêm lỗi nghiệp vụ
        if (ModelState.IsValid)
        {
            // Đảm bảo AvailableSeats không vượt Capacity khi tạo mới (dù đã có kiểm tra ở trên)
            flight.AvailableSeats = Math.Min(flight.AvailableSeats, flight.Capacity);

            _context.Add(flight);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Thêm chuyến bay mới thành công!";
            return RedirectToAction(nameof(Index));
        }
        // Nếu ModelState không hợp lệ, trả về View với dữ liệu đã nhập và lỗi
        return View(flight);
    }

    // GET: Flights/Edit/5
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null) return NotFound();
        var flight = await _context.Flights.FindAsync(id); // FindAsync lấy entity để theo dõi thay đổi
        if (flight == null) return NotFound();
        return View(flight);
    }

    // POST: Flights/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, [Bind("FlightId,FlightNumber,Capacity,StartingTime,ReachingTime,StartingDestination,ReachingDestination,Airline,Price,AvailableSeats")] Flight flight)
    {
        if (id != flight.FlightId) return NotFound();

        // --- Kiểm tra validation logic nghiệp vụ ---
        if (flight.StartingTime >= flight.ReachingTime)
        {
            ModelState.AddModelError("ReachingTime", "Thời gian đến phải sau thời gian khởi hành.");
        }
        if (flight.AvailableSeats > flight.Capacity)
        {
            ModelState.AddModelError("AvailableSeats", "Số ghế còn lại không được lớn hơn tổng số ghế (sức chứa).");
        }
        // Kiểm tra logic phức tạp hơn: Số ghế trống mới không được nhỏ hơn số ghế đã bán (đang không bị hủy)
        int currentlyBookedSeats = await _context.Tickets.CountAsync(t => t.FlightId == id && t.Status != "Cancelled");
        if (flight.Capacity < currentlyBookedSeats)
        {
            ModelState.AddModelError("Capacity", $"Không thể giảm sức chứa xuống dưới số vé đã bán ({currentlyBookedSeats}).");
        }
        if (flight.AvailableSeats < 0) // Đảm bảo không âm
        {
            ModelState.AddModelError("AvailableSeats", "Số ghế còn lại không được là số âm.");
        }

        // Kiểm tra ModelState sau khi thêm lỗi nghiệp vụ
        if (ModelState.IsValid)
        {
            try
            {
                // Chỉ cập nhật entity đã lấy từ DB để tránh ghi đè không mong muốn
                var flightToUpdate = await _context.Flights.FirstOrDefaultAsync(f => f.FlightId == id);
                if (flightToUpdate == null) return NotFound();

                // Cập nhật các thuộc tính từ model binding vào entity đang được theo dõi
                flightToUpdate.FlightNumber = flight.FlightNumber;
                flightToUpdate.Capacity = flight.Capacity;
                flightToUpdate.StartingTime = flight.StartingTime;
                flightToUpdate.ReachingTime = flight.ReachingTime;
                flightToUpdate.StartingDestination = flight.StartingDestination;
                flightToUpdate.ReachingDestination = flight.ReachingDestination;
                flightToUpdate.Airline = flight.Airline;
                flightToUpdate.Price = flight.Price;
                flightToUpdate.AvailableSeats = flight.AvailableSeats;
                // Đảm bảo AvailableSeats không vượt Capacity sau khi cập nhật
                flightToUpdate.AvailableSeats = Math.Min(flightToUpdate.AvailableSeats, flightToUpdate.Capacity);


                // _context.Update(flightToUpdate); // Không cần gọi Update nếu entity đã được theo dõi
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Cập nhật thông tin chuyến bay thành công!";
            }
            catch (DbUpdateConcurrencyException) // Xử lý lỗi trùng khớp dữ liệu
            {
                if (!FlightExists(flight.FlightId))
                {
                    return NotFound();
                }
                else
                {
                    ModelState.AddModelError(string.Empty, "Dữ liệu đã được người khác thay đổi. Vui lòng tải lại trang và thử lại.");
                    // Không return View(flight) vì flight có thể là dữ liệu cũ
                    var currentFlight = await _context.Flights.AsNoTracking().FirstOrDefaultAsync(f => f.FlightId == id);
                    if (currentFlight != null) return View(currentFlight); // Hiển thị dữ liệu mới nhất
                    else return NotFound(); // Hoặc báo not found nếu đã bị xóa
                }
            }
            return RedirectToAction(nameof(Index));
        }
        // Nếu ModelState không hợp lệ, trả về View với dữ liệu đã nhập và lỗi
        return View(flight);
    }

    // GET: Flights/Delete/5
    public async Task<IActionResult> Delete(int? id, bool? saveChangesError = false) // Thêm tham số báo lỗi
    {
        if (id == null) return NotFound();

        var flight = await _context.Flights
            .AsNoTracking() // Đọc để hiển thị, không cần theo dõi
            .FirstOrDefaultAsync(m => m.FlightId == id);
        if (flight == null) return NotFound();

        if (saveChangesError.GetValueOrDefault()) // Kiểm tra nếu có lỗi từ lần POST trước
        {
            ViewData["ErrorMessage"] = "Xóa thất bại. Hãy thử lại, hoặc liên hệ quản trị viên nếu lỗi tiếp diễn.";
        }

        // Kiểm tra trước xem có vé đang hoạt động không
        ViewData["HasActiveTickets"] = await _context.Tickets.AnyAsync(t => t.FlightId == id && t.Status != "Cancelled");


        return View(flight);
    }

    // POST: Flights/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        // Kiểm tra lại lần nữa trước khi xóa thực sự
        var hasActiveTickets = await _context.Tickets.AnyAsync(t => t.FlightId == id && t.Status != "Cancelled");
        if (hasActiveTickets)
        {
            TempData["ErrorMessage"] = "Không thể xóa chuyến bay vì vẫn còn vé đang hoạt động (chưa bị hủy).";
            return RedirectToAction(nameof(Delete), new { id = id }); // Quay lại trang Delete GET để hiển thị lỗi
        }

        var flight = await _context.Flights.FindAsync(id);
        if (flight == null)
        {
            TempData["ErrorMessage"] = "Không tìm thấy chuyến bay để xóa.";
            return RedirectToAction(nameof(Index));
        }

        try
        {
            // Cân nhắc xóa các Section liên quan nếu cần (phụ thuộc vào cấu hình FK)
            // var sections = await _context.Sections.Where(s => s.FlightId == id).ToListAsync();
            // if (sections.Any()) _context.Sections.RemoveRange(sections);

            _context.Flights.Remove(flight);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Xóa chuyến bay thành công!";
            return RedirectToAction(nameof(Index));
        }
        catch (DbUpdateException ex) // Bắt lỗi liên quan đến database
        {
            // Log lỗi (dùng thư viện logging)
            // Logger.LogError(ex, "Error deleting flight {FlightId}", id);
            TempData["ErrorMessage"] = "Đã xảy ra lỗi khi xóa chuyến bay. Vui lòng thử lại.";
            // Chuyển hướng về trang Delete GET với tham số báo lỗi
            return RedirectToAction(nameof(Delete), new { id = id, saveChangesError = true });
        }
    }

    private bool FlightExists(int id)
    {
        return (_context.Flights?.Any(e => e.FlightId == id)).GetValueOrDefault();
    }
}