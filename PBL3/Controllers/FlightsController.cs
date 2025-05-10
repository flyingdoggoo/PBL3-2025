using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PBL3.Data;
using PBL3.Models;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using System.Linq;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;
using PBL3.Models.ViewModels; // Đảm bảo using ViewModel
using System;
using PBL3.Utils;
using Microsoft.Extensions.Logging;

[Authorize(Roles = "Admin,Employee")]
public class FlightsController : Controller
{
    private readonly ApplicationDbContext _context;

    // Danh sách hãng bay (có thể chuyển ra config hoặc DB nếu phức tạp hơn)
    private static readonly List<(string Prefix, string Name)> AirlinesListInfo = new List<(string, string)>
    {
        ("VN", "Vietnam Airlines"), ("VJ", "Vietjet Air"), ("QH", "Bamboo Airways"),
        ("BL", "Pacific Airlines"), ("VU", "Vietravel Airlines")
        // Thêm các hãng khác nếu cần
    };

    public FlightsController(ApplicationDbContext context)
    {
        _context = context;
    }

    // --- Helper để populate dropdowns cho ViewModel ---
    private async Task PopulateDropdownsAsync(FlightViewModel viewModel)
    {
        viewModel.AirportsList = await _context.Airports
            .OrderBy(a => a.City).ThenBy(a => a.Name)
            .Select(a => new SelectListItem
            {
                Value = a.Id.ToString(),
                Text = $"{a.City} - {a.Name} ({a.Code})"
            }).ToListAsync();

        viewModel.AirlinesList = AirlinesListInfo
            .Select(a => new SelectListItem { Value = a.Prefix, Text = $"{a.Name} ({a.Prefix})" })
            .OrderBy(a => a.Text)
            .ToList();
    }

    // GET: Flights
    public async Task<IActionResult> Index(string sortOrder, string searchString, int? pageNumber) // Thêm pageNumber
    {
        ViewData["CurrentSort"] = sortOrder; // Để giữ lại trạng thái sort khi phân trang
        ViewData["NumberSortParm"] = String.IsNullOrEmpty(sortOrder) ? "number_desc" : "";
        ViewData["DateSortParm"] = sortOrder == "Date" ? "date_desc" : "Date";
        ViewData["AirlineSortParm"] = sortOrder == "Airline" ? "airline_desc" : "Airline";
        ViewData["PriceSortParm"] = sortOrder == "Price" ? "price_desc" : "Price";
        ViewData["CurrentFilter"] = searchString;

        var flightsQuery = _context.Flights
                              .Include(f => f.DepartureAirport)
                              .Include(f => f.ArrivalAirport)
                              .AsQueryable();

        if (!String.IsNullOrEmpty(searchString))
        {
            flightsQuery = flightsQuery.Where(f =>
               f.FlightNumber.Contains(searchString) ||
               f.Airline.Contains(searchString) ||
               (f.DepartureAirport != null && (f.DepartureAirport.City.Contains(searchString) || f.DepartureAirport.Code.Contains(searchString) || f.DepartureAirport.Name.Contains(searchString))) ||
               (f.ArrivalAirport != null && (f.ArrivalAirport.City.Contains(searchString) || f.ArrivalAirport.Code.Contains(searchString) || f.ArrivalAirport.Name.Contains(searchString)))
           );
        }

        switch (sortOrder)
        {
            case "number_desc": flightsQuery = flightsQuery.OrderByDescending(f => f.FlightNumber); break;
            case "Date": flightsQuery = flightsQuery.OrderBy(f => f.StartingTime); break;
            case "date_desc": flightsQuery = flightsQuery.OrderByDescending(f => f.StartingTime); break;
            case "Airline": flightsQuery = flightsQuery.OrderBy(f => f.Airline); break;
            case "airline_desc": flightsQuery = flightsQuery.OrderByDescending(f => f.Airline); break;
            case "Price": flightsQuery = flightsQuery.OrderBy(f => f.Price); break;
            case "price_desc": flightsQuery = flightsQuery.OrderByDescending(f => f.Price); break;
            default: flightsQuery = flightsQuery.OrderBy(f => f.FlightNumber); break;
        }

        int pageSize = 10; // Số chuyến bay trên mỗi trang
        var paginatedFlights = await PaginatedList<Flight>.CreateAsync(flightsQuery.AsNoTracking(), pageNumber ?? 1, pageSize);
        return View(paginatedFlights);
    }

    // GET: Flights/Details/5
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null) return NotFound();
        var flight = await _context.Flights
                                     .Include(f => f.DepartureAirport)
                                     .Include(f => f.ArrivalAirport)
                                     .Include(f => f.Sections) // Quan trọng: Include Sections
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
            StartingTime = DateTime.Now.Date.AddDays(1).AddHours(9), // Mặc định ngày mai, 9h sáng
            ReachingTime = DateTime.Now.Date.AddDays(1).AddHours(11), // Mặc định ngày mai, 11h sáng
            Capacity = 150,
            Price = 1000000m, // Giá mặc định, dùng m cho decimal
            CreateSections = true // Mặc định tạo section
        };
        await PopulateDropdownsAsync(viewModel);
        return View(viewModel);
    }

    // POST: Flights/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(FlightViewModel viewModel)
    {
        // --- SERVER-SIDE VALIDATION ---
        if (viewModel.StartingDestination == viewModel.ReachingDestination)
            ModelState.AddModelError("ReachingDestination", "Sân bay đến không được trùng với sân bay đi.");

        if (viewModel.StartingTime >= viewModel.ReachingTime)
            ModelState.AddModelError("ReachingTime", "Thời gian đến phải sau thời gian khởi hành.");

        if (viewModel.StartingTime < DateTime.Now.AddMinutes(30)) // Ít nhất 30 phút kể từ bây giờ
            ModelState.AddModelError("StartingTime", "Thời gian khởi hành phải ở tương lai và cách ít nhất 30 phút.");

        string fullFlightNumber = (viewModel.SelectedAirlinePrefix ?? "") + (viewModel.FlightNumberSuffix ?? "");
        if (string.IsNullOrWhiteSpace(fullFlightNumber) || fullFlightNumber.Length < 3) // Kiểm tra độ dài cơ bản
        {
            ModelState.AddModelError("FlightNumberSuffix", "Số hiệu chuyến bay không hợp lệ.");
        }
        else
        {
            bool flightNumberExists = await _context.Flights.AnyAsync(f =>
                f.FlightNumber == fullFlightNumber &&
                f.StartingTime.Date == viewModel.StartingTime.Date);
            if (flightNumberExists)
                ModelState.AddModelError("FlightNumberSuffix", $"Số hiệu '{fullFlightNumber}' đã tồn tại trong ngày {viewModel.StartingTime:dd/MM/yyyy}.");
        }
        // --- KẾT THÚC VALIDATION ---

        if (ModelState.IsValid)
        {
            var airlineInfo = AirlinesListInfo.FirstOrDefault(a => a.Prefix == viewModel.SelectedAirlinePrefix);
            var flight = new Flight
            {
                FlightNumber = fullFlightNumber,
                Airline = airlineInfo.Name ?? "Không xác định", // Lấy tên đầy đủ của hãng
                StartingDestination = viewModel.StartingDestination,
                ReachingDestination = viewModel.ReachingDestination,
                StartingTime = viewModel.StartingTime,
                ReachingTime = viewModel.ReachingTime,
                Capacity = viewModel.Capacity,
                Price = viewModel.Price,
                AvailableSeats = viewModel.Capacity, // Ban đầu ghế trống bằng tổng sức chứa
                // Distance sẽ được tính nếu bạn có logic
            };

            _context.Flights.Add(flight);
            await _context.SaveChangesAsync(); // Lưu Flight để có FlightId

            // --- TẠO SECTIONS NẾU ĐƯỢC CHỌN ---
            if (viewModel.CreateSections && flight.FlightId > 0)
            {
                // Số ghế hạng thương gia (30% làm tròn xuống)
                int businessClassSeats = (int)Math.Floor(viewModel.Capacity * 0.30);
                // Số ghế hạng phổ thông
                int economyClassSeats = viewModel.Capacity - businessClassSeats;

                var sectionsToAdd = new List<Section>();
                if (businessClassSeats > 0)
                {
                    sectionsToAdd.Add(new Section
                    {
                        FlightId = flight.FlightId,
                        SectionName = "Thương gia",
                        Capacity = businessClassSeats,
                        PriceMultiplier = 1.8m // Ví dụ: Giá gấp 1.8 lần
                        // Có thể thêm giá riêng cho section này nếu model Section có SectionPrice
                    });
                }
                if (economyClassSeats > 0)
                {
                    sectionsToAdd.Add(new Section
                    {
                        FlightId = flight.FlightId,
                        SectionName = "Phổ thông",
                        Capacity = economyClassSeats,
                        PriceMultiplier = 1.0m // Ví dụ: Giá gấp 1 lần
                    });
                }

                var allNewSeats = new List<Seat>();
                if (sectionsToAdd.Any())
                {
                    _context.Sections.AddRange(sectionsToAdd);
                    await _context.SaveChangesAsync(); // Lưu Sections
                    int startingRow = 1; // Bắt đầu từ hàng 1 cho mỗi section
                    foreach (var newSection in sectionsToAdd)
                    {
                        startingRow = SeatGenerator.GenerateSeatsForSection(allNewSeats, newSection, startingRow);
                    }
                }

                if (allNewSeats.Any())
                {
                    _context.Seats.AddRange(allNewSeats);
                    _context.SaveChanges(); // Lưu tất cả ghế
                }
            }

            TempData["SuccessMessage"] = $"Thêm chuyến bay {flight.FlightNumber} thành công!";
            return RedirectToAction(nameof(Index));
        }

        await PopulateDropdownsAsync(viewModel); // Populate lại nếu model không hợp lệ
        return View(viewModel);
    }

    // GET: Flights/Edit/5
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null) return NotFound();
        var flight = await _context.Flights.Include(f => f.Sections).FirstOrDefaultAsync(f => f.FlightId == id); // Include Sections
        if (flight == null) return NotFound();

        var viewModel = new FlightViewModel
        {
            FlightId = flight.FlightId,
            // Tách Prefix từ tên Hãng bay lưu trong DB, và Suffix từ FlightNumber
            // Giả định FlightNumber lưu dạng "VN123", Airline lưu "Vietnam Airlines"
            SelectedAirlinePrefix = AirlinesListInfo.FirstOrDefault(a => a.Name == flight.Airline).Prefix ??
                                     (flight.FlightNumber.Length >= 2 ? flight.FlightNumber.Substring(0, 2) : ""), // Dự phòng nếu tên hãng không khớp
            FlightNumberSuffix = flight.FlightNumber.Length > 2 && (AirlinesListInfo.Any(a => a.Prefix == flight.FlightNumber.Substring(0, 2)))
                                    ? flight.FlightNumber.Substring(2)
                                    : flight.FlightNumber, // Nếu không tách được prefix thì lấy cả
            StartingDestination = flight.StartingDestination,
            ReachingDestination = flight.ReachingDestination,
            StartingTime = flight.StartingTime,
            ReachingTime = flight.ReachingTime,
            Capacity = flight.Capacity,
            Price = flight.Price,
            CreateSections = flight.Sections.Any() // Check xem đã có section chưa để hiển thị checkbox
        };

        await PopulateDropdownsAsync(viewModel);
        return View(viewModel);
    }

    // POST: Flights/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, FlightViewModel viewModel)
    {
        if (id != viewModel.FlightId) return NotFound();

        // --- SERVER-SIDE VALIDATION (Tương tự Create) ---
        if (viewModel.StartingDestination == viewModel.ReachingDestination) ModelState.AddModelError("ReachingDestination", "Sân bay đến không được trùng với sân bay đi.");
        if (viewModel.StartingTime >= viewModel.ReachingTime) ModelState.AddModelError("ReachingTime", "Thời gian đến phải sau thời gian khởi hành.");

        string fullFlightNumber = (viewModel.SelectedAirlinePrefix ?? "") + (viewModel.FlightNumberSuffix ?? "");
        if (string.IsNullOrWhiteSpace(fullFlightNumber) || fullFlightNumber.Length < 3)
        {
            ModelState.AddModelError("FlightNumberSuffix", "Số hiệu chuyến bay không hợp lệ.");
        }
        else
        {
            bool flightNumberExists = await _context.Flights.AnyAsync(f =>
               f.FlightNumber == fullFlightNumber &&
               f.StartingTime.Date == viewModel.StartingTime.Date &&
               f.FlightId != viewModel.FlightId); // Loại trừ chính nó
            if (flightNumberExists) ModelState.AddModelError("FlightNumberSuffix", $"Số hiệu '{fullFlightNumber}' đã tồn tại trong ngày {viewModel.StartingTime:dd/MM/yyyy}.");
        }
        // --- KẾT THÚC VALIDATION ---

        if (ModelState.IsValid)
        {
            try
            {
                var flightToUpdate = await _context.Flights.Include(f => f.Sections).FirstOrDefaultAsync(f => f.FlightId == id);
                if (flightToUpdate == null) return NotFound();

                var airlineInfo = AirlinesListInfo.FirstOrDefault(a => a.Prefix == viewModel.SelectedAirlinePrefix);

                flightToUpdate.FlightNumber = fullFlightNumber;
                flightToUpdate.Airline = airlineInfo.Name ?? flightToUpdate.Airline;
                flightToUpdate.StartingDestination = viewModel.StartingDestination;
                flightToUpdate.ReachingDestination = viewModel.ReachingDestination;
                flightToUpdate.StartingTime = viewModel.StartingTime;
                flightToUpdate.ReachingTime = viewModel.ReachingTime;
                flightToUpdate.Capacity = viewModel.Capacity;
                flightToUpdate.Price = viewModel.Price;

                // Tính lại AvailableSeats dựa trên Capacity mới và số vé đã bán
                int bookedTickets = await _context.Tickets.CountAsync(t => t.FlightId == flightToUpdate.FlightId && t.Status != TicketStatus.Cancelled);
                flightToUpdate.AvailableSeats = viewModel.Capacity - bookedTickets;
                if (flightToUpdate.AvailableSeats < 0) flightToUpdate.AvailableSeats = 0; // Đảm bảo không âm


                // --- XỬ LÝ SECTIONS KHI EDIT ---
                // Nếu người dùng chọn tạo/cập nhật sections và trước đó chưa có, hoặc muốn thay đổi
                if (viewModel.CreateSections)
                {
                    // Xóa sections cũ nếu có (để tạo lại cho đơn giản, hoặc bạn có thể cập nhật từng section)
                    if (flightToUpdate.Sections.Any())
                    {
                        _context.Sections.RemoveRange(flightToUpdate.Sections);
                        // Cần SaveChangesAsync ở đây nếu bạn muốn xóa ngay trước khi thêm mới
                        // Hoặc EF Core sẽ tự xử lý thứ tự khi SaveChangesAsync cuối cùng
                    }

                    int businessClassSeats = (int)Math.Floor(viewModel.Capacity * 0.30);
                    int economyClassSeats = viewModel.Capacity - businessClassSeats;
                    var newSections = new List<Section>();

                    if (businessClassSeats > 0) newSections.Add(new Section { FlightId = flightToUpdate.FlightId, SectionName = "Thương gia", Capacity = businessClassSeats });
                    if (economyClassSeats > 0) newSections.Add(new Section { FlightId = flightToUpdate.FlightId, SectionName = "Phổ thông", Capacity = economyClassSeats });

                    if (newSections.Any()) _context.Sections.AddRange(newSections);
                }
                else // Nếu không chọn tạo sections nữa và flight đã có sections -> xóa đi
                {
                    if (flightToUpdate.Sections.Any())
                    {
                        _context.Sections.RemoveRange(flightToUpdate.Sections);
                    }
                }

                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Cập nhật thông tin chuyến bay thành công!";
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!FlightExists(viewModel.FlightId)) return NotFound();
                else
                {
                    ModelState.AddModelError(string.Empty, "Dữ liệu đã được người khác thay đổi. Vui lòng tải lại trang và thử lại.");
                    await PopulateDropdownsAsync(viewModel);
                    return View(viewModel);
                }
            }
            return RedirectToAction(nameof(Index));
        }
        await PopulateDropdownsAsync(viewModel);
        return View(viewModel);
    }


    // GET: Flights/Delete/5
    public async Task<IActionResult> Delete(int? id, bool? saveChangesError = false)
    {
        if (id == null) return NotFound();
        var flight = await _context.Flights
                                     .Include(f => f.DepartureAirport)
                                     .Include(f => f.ArrivalAirport)
                                     .Include(f => f.Sections) // Include Sections để hiển thị nếu cần
                                     .AsNoTracking()
                                     .FirstOrDefaultAsync(m => m.FlightId == id);
        if (flight == null) return NotFound();
        if (saveChangesError.GetValueOrDefault()) ViewData["ErrorMessage"] = "Xóa thất bại. Hãy thử lại, hoặc liên hệ quản trị viên nếu lỗi tiếp diễn.";
        ViewData["HasActiveTickets"] = await _context.Tickets.AnyAsync(t => t.FlightId == id && t.Status != TicketStatus.Cancelled);
        return View(flight);
    }

    // POST: Flights/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var hasActiveTickets = await _context.Tickets.AnyAsync(t => t.FlightId == id && t.Status != TicketStatus.Cancelled);
        if (hasActiveTickets)
        {
            TempData["ErrorMessage"] = "Không thể xóa chuyến bay vì vẫn còn vé đang hoạt động (chưa bị hủy).";
            return RedirectToAction(nameof(Delete), new { id = id });
        }

        var flight = await _context.Flights.Include(f => f.Sections).FirstOrDefaultAsync(f => f.FlightId == id); // Include Sections để xóa
        if (flight == null)
        {
            TempData["ErrorMessage"] = "Không tìm thấy chuyến bay để xóa.";
            return RedirectToAction(nameof(Index));
        }
        try
        {
            // Xóa Sections liên quan trước (nếu không có OnDelete Cascade từ DB)
            if (flight.Sections.Any())
            {
                _context.Sections.RemoveRange(flight.Sections);
            }
            _context.Flights.Remove(flight);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Xóa chuyến bay thành công!";
            return RedirectToAction(nameof(Index));
        }
        catch (DbUpdateException ex)
        {
            // Log lỗi ex
            TempData["ErrorMessage"] = "Đã xảy ra lỗi khi xóa chuyến bay. Có thể do còn dữ liệu liên quan (vé,...).";
            return RedirectToAction(nameof(Delete), new { id = id, saveChangesError = true });
        }
    }

    private bool FlightExists(int id) => (_context.Flights?.Any(e => e.FlightId == id)).GetValueOrDefault();
}