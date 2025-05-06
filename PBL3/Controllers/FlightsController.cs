using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PBL3.Data;
using PBL3.Models;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using System.Linq;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;
using PBL3.Models.ViewModels; // **THÊM USING VIEWMODEL**
using System; // Thêm cho DateTime, Math

[Authorize(Roles = "Admin,Employee")]
public class FlightsController : Controller
{
    private readonly ApplicationDbContext _context;

    // Danh sách hãng bay (có thể chuyển ra ngoài nếu muốn tái sử dụng nhiều nơi)
    private static readonly List<(string Prefix, string Name)> AirlinesInfo = new List<(string, string)>
    {
        ("VN", "Vietnam Airlines"), ("VJ", "Vietjet Air"), ("QH", "Bamboo Airways"),
        ("BL", "Pacific Airlines"), ("VU", "Vietravel Airlines")
    };

    public FlightsController(ApplicationDbContext context)
    {
        _context = context;
    }

    // --- Helper Populate Dropdowns ---
    private async Task PopulateDropdownsAsync(FlightViewModel viewModel)
    {
        // Sân bay
        var airportsQuery = _context.Airports.OrderBy(a => a.City).ThenBy(a => a.Code);
        viewModel.AirportsList = await airportsQuery
            .Select(a => new SelectListItem { Value = a.Id.ToString(), Text = $"{a.Name} ({a.Code})" })
            .ToListAsync();

        // Hãng bay (Value là Prefix)
        viewModel.AirlinesList = AirlinesInfo
            .Select(a => new SelectListItem { Value = a.Prefix, Text = $"{a.Name} ({a.Prefix})" })
            .OrderBy(a => a.Text)
            .ToList();
    }

    // GET: Flights
    public async Task<IActionResult> Index(string sortOrder, string searchString)
    {
        // Code Index giữ nguyên, không cần thay đổi vì nó làm việc trực tiếp với Flight model
        ViewData["NumberSortParm"] = String.IsNullOrEmpty(sortOrder) ? "number_desc" : "";
        ViewData["DateSortParm"] = sortOrder == "Date" ? "date_desc" : "Date";
        ViewData["AirlineSortParm"] = sortOrder == "Airline" ? "airline_desc" : "Airline";
        ViewData["PriceSortParm"] = sortOrder == "Price" ? "price_desc" : "Price";
        ViewData["CurrentFilter"] = searchString;

        var flights = _context.Flights
                              .Include(f => f.DepartureAirport)
                              .Include(f => f.ArrivalAirport)
                              .AsQueryable();

        if (!String.IsNullOrEmpty(searchString))
        {
            flights = flights.Where(f =>
               f.FlightNumber.Contains(searchString) ||
               f.Airline.Contains(searchString) ||
               (f.DepartureAirport != null && (f.DepartureAirport.City.Contains(searchString) || f.DepartureAirport.Code.Contains(searchString))) ||
               (f.ArrivalAirport != null && (f.ArrivalAirport.City.Contains(searchString) || f.ArrivalAirport.Code.Contains(searchString)))
           );
        }
        //...(switch case sắp xếp)
        switch (sortOrder)
        {
            case "number_desc": flights = flights.OrderByDescending(f => f.FlightNumber); break;
            case "Date": flights = flights.OrderBy(f => f.StartingTime); break;
            case "date_desc": flights = flights.OrderByDescending(f => f.StartingTime); break;
            case "Airline": flights = flights.OrderBy(f => f.Airline); break;
            case "airline_desc": flights = flights.OrderByDescending(f => f.Airline); break;
            case "Price": flights = flights.OrderBy(f => f.Price); break;
            case "price_desc": flights = flights.OrderByDescending(f => f.Price); break;
            default: flights = flights.OrderBy(f => f.FlightNumber); break;
        }


        return View(await flights.AsNoTracking().ToListAsync());
    }

    // GET: Flights/Details/5
    public async Task<IActionResult> Details(int? id)
    {
        // Code Details giữ nguyên, làm việc với Flight model
        if (id == null) return NotFound();
        var flight = await _context.Flights
                                     .Include(f => f.DepartureAirport)
                                     .Include(f => f.ArrivalAirport)
                                     .Include(f => f.Sections)
                                     .AsNoTracking()

                                     .FirstOrDefaultAsync(m => m.FlightId == id);
        if (flight == null) return NotFound();
        return View(flight);
    }

    // GET: Flights/Create
    public async Task<IActionResult> Create()
    {
        var viewModel = new FlightViewModel
        {
            // Đặt giá trị mặc định nếu cần
            StartingTime = DateTime.Now.AddHours(9),
            ReachingTime = DateTime.Now.AddHours(11),
            Capacity = 150,
            Price = 1000000 

        };

        await PopulateDropdownsAsync(viewModel); // Truyền ViewModel vào để populate list
        return View(viewModel); // **TRẢ VỀ VIEWMODEL**
    }

    // POST: Flights/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(FlightViewModel viewModel) // **NHẬN VIEWMODEL**
    {
        // --- SERVER-SIDE VALIDATION THÊM ---
        if (viewModel.StartingDestination == viewModel.ReachingDestination)
            ModelState.AddModelError("ReachingDestination", "Sân bay đến không được trùng với sân bay đi.");
        if (viewModel.StartingTime >= viewModel.ReachingTime)
            ModelState.AddModelError("ReachingTime", "Thời gian đến phải sau thời gian khởi hành.");
        if (viewModel.StartingTime < DateTime.Now) // Chỉ check giờ nếu ngày là hôm nay
            ModelState.AddModelError("StartingTime", "Không thể chọn thời gian khởi hành trong quá khứ.");

        // Tạo số hiệu đầy đủ để kiểm tra trùng
        string fullFlightNumber = viewModel.SelectedAirlinePrefix + viewModel.FlightNumberSuffix;
        bool flightNumberExists = await _context.Flights.AnyAsync(f =>
            f.FlightNumber == fullFlightNumber &&
            f.StartingTime.Date == viewModel.StartingTime.Date);

        if (flightNumberExists)
            ModelState.AddModelError("FlightNumberSuffix", $"Số hiệu chuyến bay '{fullFlightNumber}' đã tồn tại trong ngày {viewModel.StartingTime:dd/MM/yyyy}.");
        // --- KẾT THÚC VALIDATION THÊM ---

        if (ModelState.IsValid) // Kiểm tra cả validation từ ViewModel và custom validation
        {
            // Map từ ViewModel sang Model Flight
            var flight = new Flight
            {
                FlightNumber = fullFlightNumber, // Kết hợp prefix và suffix
                Airline = AirlinesInfo.FirstOrDefault(a => a.Prefix == viewModel.SelectedAirlinePrefix).Name ?? "Không xác định", // Lấy tên hãng đầy đủ
                StartingDestination = viewModel.StartingDestination,
                ReachingDestination = viewModel.ReachingDestination,
                StartingTime = viewModel.StartingTime,
                ReachingTime = viewModel.ReachingTime,
                Capacity = viewModel.Capacity,
                Price = viewModel.Price,
                AvailableSeats = viewModel.Capacity // Set AvailableSeats = Capacity
                // Distance có thể tính hoặc bỏ qua
            };

            _context.Add(flight);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Thêm chuyến bay mới thành công!";
            return RedirectToAction(nameof(Index));
        }

        // Nếu không hợp lệ, populate lại dropdown và trả về View với ViewModel lỗi
        await PopulateDropdownsAsync(viewModel);
        return View(viewModel);
    }

    // GET: Flights/Edit/5
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null) return NotFound();
        var flight = await _context.Flights.FindAsync(id); // Lấy Flight gốc
        if (flight == null) return NotFound();

        // Map từ Flight sang ViewModel
        var viewModel = new FlightViewModel
        {
            FlightId = flight.FlightId,
            // Tách Prefix và Suffix từ FlightNumber
            SelectedAirlinePrefix = flight.FlightNumber.Length >= 2 ? flight.FlightNumber.Substring(0, 2) : "",
            FlightNumberSuffix = flight.FlightNumber.Length > 2 ? flight.FlightNumber.Substring(2) : "",
            StartingDestination = flight.StartingDestination,
            ReachingDestination = flight.ReachingDestination,
            StartingTime = flight.StartingTime,
            ReachingTime = flight.ReachingTime,
            Capacity = flight.Capacity,
            Price = flight.Price
            // Không cần map AvailableSeats vào ViewModel
        };

        await PopulateDropdownsAsync(viewModel); // Populate dropdowns cho ViewModel
        // ViewBag không cần thiết nữa nếu dùng ViewModel.AirlinesList/AirportsList

        return View(viewModel); // **TRẢ VỀ VIEWMODEL**
    }

    // POST: Flights/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, FlightViewModel viewModel) // **NHẬN VIEWMODEL**
    {
        if (id != viewModel.FlightId) return NotFound();

        // --- SERVER-SIDE VALIDATION THÊM (Tương tự Create) ---
        if (viewModel.StartingDestination == viewModel.ReachingDestination) ModelState.AddModelError("ReachingDestination", "Sân bay đến không được trùng với sân bay đi.");
        if (viewModel.StartingTime >= viewModel.ReachingTime) ModelState.AddModelError("ReachingTime", "Thời gian đến phải sau thời gian khởi hành.");
        // Kiểm tra ngày quá khứ có thể không cần nếu datepicker client-side hoạt động tốt
        // if (viewModel.StartingTime < DateTime.Now) ModelState.AddModelError("StartingTime", "Không thể chọn thời gian khởi hành trong quá khứ.");

        string fullFlightNumber = viewModel.SelectedAirlinePrefix + viewModel.FlightNumberSuffix;
        bool flightNumberExists = await _context.Flights.AnyAsync(f =>
            f.FlightNumber == fullFlightNumber &&
            f.StartingTime.Date == viewModel.StartingTime.Date &&
            f.FlightId != viewModel.FlightId); // Loại trừ chính nó

        if (flightNumberExists) ModelState.AddModelError("FlightNumberSuffix", $"Số hiệu chuyến bay '{fullFlightNumber}' đã tồn tại trong ngày {viewModel.StartingTime:dd/MM/yyyy}.");
        // --- KẾT THÚC VALIDATION THÊM ---


        if (ModelState.IsValid)
        {
            try
            {
                var flightToUpdate = await _context.Flights.FirstOrDefaultAsync(f => f.FlightId == id);
                if (flightToUpdate == null) return NotFound();

                // Map các thay đổi từ ViewModel sang Entity gốc
                flightToUpdate.FlightNumber = fullFlightNumber;
                flightToUpdate.Airline = AirlinesInfo.FirstOrDefault(a => a.Prefix == viewModel.SelectedAirlinePrefix).Name ?? flightToUpdate.Airline; // Giữ lại giá trị cũ nếu prefix lỗi
                flightToUpdate.StartingDestination = viewModel.StartingDestination;
                flightToUpdate.ReachingDestination = viewModel.ReachingDestination;
                flightToUpdate.StartingTime = viewModel.StartingTime;
                flightToUpdate.ReachingTime = viewModel.ReachingTime;
                flightToUpdate.Capacity = viewModel.Capacity;
                flightToUpdate.Price = viewModel.Price;
                // Cập nhật AvailableSeats dựa trên Capacity mới nếu cần (ví dụ: nếu Capacity giảm)
                flightToUpdate.AvailableSeats = Math.Min(flightToUpdate.AvailableSeats, viewModel.Capacity);

                //_context.Update(flightToUpdate); // Không cần thiết khi entity được theo dõi
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Cập nhật thông tin chuyến bay thành công!";
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!FlightExists(viewModel.FlightId)) return NotFound();
                else
                {
                    ModelState.AddModelError(string.Empty, "Dữ liệu bị thay đổi bởi người khác. Tải lại?");
                    await PopulateDropdownsAsync(viewModel); // Populate lại dropdown
                    return View(viewModel);
                }
            }
            return RedirectToAction(nameof(Index));
        }

        // Nếu không hợp lệ, populate lại dropdown và trả về View với ViewModel lỗi
        await PopulateDropdownsAsync(viewModel);
        return View(viewModel);
    }

    // GET: Flights/Delete/5
    public async Task<IActionResult> Delete(int? id, bool? saveChangesError = false)
    {
        // Code Delete giữ nguyên, làm việc với Flight model
        if (id == null) return NotFound();
        var flight = await _context.Flights
                                     .Include(f => f.DepartureAirport)
                                     .Include(f => f.ArrivalAirport)
                                     .AsNoTracking()
                                     .FirstOrDefaultAsync(m => m.FlightId == id);
        if (flight == null) return NotFound();
        if (saveChangesError.GetValueOrDefault()) ViewData["ErrorMessage"] = "Xóa thất bại...";
        ViewData["HasActiveTickets"] = await _context.Tickets.AnyAsync(t => t.FlightId == id && t.Status != "Cancelled");
        return View(flight);
    }

    // POST: Flights/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        // Code DeleteConfirmed giữ nguyên, làm việc với Flight model
        var hasActiveTickets = await _context.Tickets.AnyAsync(t => t.FlightId == id && t.Status != "Cancelled");
        if (hasActiveTickets)
        {
            TempData["ErrorMessage"] = "Không thể xóa chuyến bay vì vẫn còn vé đang hoạt động.";
            return RedirectToAction(nameof(Delete), new { id = id });
        }
        var flight = await _context.Flights.FindAsync(id);
        if (flight == null)
        {
            TempData["ErrorMessage"] = "Không tìm thấy chuyến bay để xóa.";
            return RedirectToAction(nameof(Index));
        }
        try
        {
            _context.Flights.Remove(flight);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Xóa chuyến bay thành công!";
            return RedirectToAction(nameof(Index));
        }
        catch (DbUpdateException)
        {
            TempData["ErrorMessage"] = "Đã xảy ra lỗi khi xóa chuyến bay...";
            return RedirectToAction(nameof(Delete), new { id = id, saveChangesError = true });
        }
    }


    private bool FlightExists(int id) => (_context.Flights?.Any(e => e.FlightId == id)).GetValueOrDefault();
}