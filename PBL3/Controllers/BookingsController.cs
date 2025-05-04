using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PBL3.Data;
using PBL3.Models;
using PBL3.Utils; // Namespace chứa PaginatedList (nếu tách file)
using System;
using System.Linq;
using System.Threading.Tasks;

[Authorize(Roles = "Admin,Employee")] // Admin và Employee có thể quản lý đặt vé
public class BookingsController : Controller
{
    private readonly ApplicationDbContext _context;

    public BookingsController(ApplicationDbContext context)
    {
        _context = context;
    }

    // GET: Bookings
    public async Task<IActionResult> Index(string sortOrder, string currentFilter, string searchString, int? pageNumber)
    {
        ViewData["CurrentSort"] = sortOrder;
        ViewData["PassengerSortParm"] = String.IsNullOrEmpty(sortOrder) ? "passenger_desc" : "";
        ViewData["FlightSortParm"] = sortOrder == "Flight" ? "flight_desc" : "Flight";
        ViewData["DateSortParm"] = sortOrder == "Date" ? "date_desc" : "Date";
        ViewData["StatusSortParm"] = sortOrder == "Status" ? "status_desc" : "Status";


        if (searchString != null)
        {
            pageNumber = 1; // Reset về trang 1 nếu có tìm kiếm mới
        }
        else
        {
            searchString = currentFilter; // Giữ lại bộ lọc hiện tại nếu không có tìm kiếm mới
        }

        ViewData["CurrentFilter"] = searchString;

        var ticketsQuery = _context.Tickets
                                .Include(t => t.Passenger) // Lấy thông tin Passenger (FullName, Email)
                                .Include(t => t.Flight) // Lấy thông tin Flight (FlightNumber)
                                .AsQueryable();

        if (!String.IsNullOrEmpty(searchString))
        {
            // Tìm theo ID vé, tên/email khách, số hiệu chuyến bay
            ticketsQuery = ticketsQuery.Where(t => t.TicketId.ToString().Contains(searchString)
                                                || t.Passenger.FullName.Contains(searchString)
                                                || t.Passenger.Email.Contains(searchString)
                                                || t.Flight.FlightNumber.Contains(searchString));
        }

        switch (sortOrder)
        {
            case "passenger_desc":
                ticketsQuery = ticketsQuery.OrderByDescending(t => t.Passenger.FullName);
                break;
            case "Flight":
                ticketsQuery = ticketsQuery.OrderBy(t => t.Flight.FlightNumber);
                break;
            case "flight_desc":
                ticketsQuery = ticketsQuery.OrderByDescending(t => t.Flight.FlightNumber);
                break;
            case "Date":
                ticketsQuery = ticketsQuery.OrderBy(t => t.OrderTime);
                break;
            case "date_desc":
                ticketsQuery = ticketsQuery.OrderByDescending(t => t.OrderTime);
                break;
            case "Status":
                ticketsQuery = ticketsQuery.OrderBy(t => t.Status);
                break;
            case "status_desc":
                ticketsQuery = ticketsQuery.OrderByDescending(t => t.Status);
                break;
            default: // Sắp xếp theo Passenger Name mặc định
                ticketsQuery = ticketsQuery.OrderBy(t => t.Passenger.FullName);
                break;
        }

        int pageSize = 10; // Số lượng vé trên mỗi trang
        var paginatedTickets = await PaginatedList<Ticket>.CreateAsync(ticketsQuery.AsNoTracking(), pageNumber ?? 1, pageSize);

        return View(paginatedTickets); // Truyền PaginatedList vào View
    }

    // GET: Bookings/Details/5
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null) return NotFound();

        var ticket = await _context.Tickets
            .Include(t => t.Passenger)
            .Include(t => t.Flight)
                 .ThenInclude(f => f.Sections) // Lấy section của chuyến bay nếu cần
            .Include(t => t.Section) // Lấy section cụ thể của vé (nếu có)
            .Include(t => t.BookingEmployee) // Lấy nhân viên đặt vé hộ (nếu có)
            .AsNoTracking() // Tối ưu đọc
            .FirstOrDefaultAsync(m => m.TicketId == id);

        if (ticket == null) return NotFound();

        return View(ticket);
    }


    // GET: Bookings/Cancel/5
    public async Task<IActionResult> Cancel(int? id)
    {
        if (id == null) return NotFound();
        var ticket = await _context.Tickets
                                     .Include(t => t.Passenger)
                                     .Include(t => t.Flight) // Cần Flight để hiển thị thông tin và cập nhật AvailableSeats
                                     .FirstOrDefaultAsync(m => m.TicketId == id); // Theo dõi để cập nhật

        if (ticket == null) return NotFound();

        // Kiểm tra xem vé đã bị hủy chưa
        if (ticket.Status == "Cancelled")
        {
            TempData["InfoMessage"] = "Vé này đã được hủy trước đó.";
            // Có thể chuyển về Details hoặc Index
            return RedirectToAction(nameof(Details), new { id = id });
        }

        // Kiểm tra xem chuyến bay đã cất cánh chưa (logic tùy chọn)
        // if (ticket.Flight.StartingTime <= DateTime.Now)
        // {
        //     TempData["ErrorMessage"] = "Không thể hủy vé cho chuyến bay đã khởi hành.";
        //      return RedirectToAction(nameof(Details), new { id = id });
        // }


        return View(ticket); // Hiển thị view xác nhận hủy
    }

    // POST: Bookings/Cancel/5
    [HttpPost, ActionName("Cancel")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CancelConfirmed(int id)
    {
        // Lấy lại vé và chuyến bay liên quan để cập nhật
        // Dùng FirstOrDefaultAsync để lấy entity đang được theo dõi
        var ticket = await _context.Tickets
                                     .Include(t => t.Flight)
                                     .FirstOrDefaultAsync(t => t.TicketId == id);

        if (ticket == null) return NotFound();

        // Kiểm tra lại trạng thái trước khi thực hiện
        if (ticket.Status == "Cancelled")
        {
            TempData["InfoMessage"] = "Vé này đã được hủy trước đó.";
            return RedirectToAction(nameof(Index));
        }

        // Kiểm tra lại chuyến bay đã cất cánh chưa (nếu có logic này)
        // if (ticket.Flight.StartingTime <= DateTime.Now)
        // {
        //     TempData["ErrorMessage"] = "Không thể hủy vé cho chuyến bay đã khởi hành.";
        //      return RedirectToAction(nameof(Details), new { id = id });
        // }


        // Sử dụng transaction để đảm bảo cả 2 thao tác (cập nhật vé, cập nhật chuyến bay) thành công hoặc thất bại cùng nhau
        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            // 1. Cập nhật trạng thái vé
            ticket.Status = "Cancelled";
            // _context.Update(ticket); // Không cần nếu ticket đang được theo dõi

            // 2. Cập nhật số ghế trống của chuyến bay
            if (ticket.Flight != null)
            {
                // Chỉ tăng AvailableSeats nếu nó chưa đạt Capacity tối đa
                if (ticket.Flight.AvailableSeats < ticket.Flight.Capacity)
                {
                    ticket.Flight.AvailableSeats += 1;
                    // _context.Update(ticket.Flight); // Không cần nếu flight được include và theo dõi
                }
                else
                {
                    // Log cảnh báo nếu AvailableSeats đã bằng Capacity mà vẫn hủy vé? (logic lạ)
                }
            }
            else
            {
                // Xử lý trường hợp không tìm thấy chuyến bay (lỗi dữ liệu?)
                await transaction.RollbackAsync(); // Hủy transaction
                TempData["ErrorMessage"] = "Lỗi: Không tìm thấy chuyến bay liên kết với vé này.";
                return RedirectToAction(nameof(Index));
            }


            // 3. Lưu tất cả thay đổi vào database
            await _context.SaveChangesAsync();

            // 4. Commit transaction nếu mọi thứ thành công
            await transaction.CommitAsync();

            TempData["SuccessMessage"] = $"Hủy vé {ticket.TicketId} thành công. Số ghế trống của chuyến bay đã được cập nhật.";
            return RedirectToAction(nameof(Index)); // Hoặc về trang Details của vé vừa hủy
        }
        catch (DbUpdateException ex) // Bắt lỗi cụ thể từ database
        {
            await transaction.RollbackAsync(); // Quan trọng: Rollback khi có lỗi
            // Log lỗi (sử dụng thư viện logging)
            // Logger.LogError(ex, "Error cancelling ticket {TicketId}", id);
            TempData["ErrorMessage"] = "Đã xảy ra lỗi database khi cố gắng hủy vé. Vui lòng thử lại.";
            // Quay lại trang Cancel GET để người dùng thử lại hoặc xem lỗi
            return RedirectToAction(nameof(Cancel), new { id = id });
        }
        catch (Exception ex) // Bắt các lỗi khác
        {
            await transaction.RollbackAsync();
            // Logger.LogError(ex, "Generic error cancelling ticket {TicketId}", id);
            TempData["ErrorMessage"] = "Đã xảy ra lỗi không xác định khi hủy vé.";
            return RedirectToAction(nameof(Cancel), new { id = id });
        }
    }

    // --- Helper Class for Pagination (Đặt ở cuối file hoặc Utils/PaginatedList.cs) ---
    // class PaginatedList<T> : List<T> { ... } // Giữ nguyên như trước
}


// Tạo file Utils/PaginatedList.cs nếu muốn tách ra
namespace PBL3.Utils
{
    using Microsoft.EntityFrameworkCore;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    public class PaginatedList<T> : List<T>
    {
        public int PageIndex { get; private set; }
        public int TotalPages { get; private set; }
        public int TotalCount { get; private set; } // Thêm tổng số record

        public PaginatedList(List<T> items, int count, int pageIndex, int pageSize)
        {
            PageIndex = pageIndex;
            TotalCount = count; // Lưu tổng số record
            TotalPages = (int)Math.Ceiling(count / (double)pageSize);
            this.AddRange(items);
        }

        public bool HasPreviousPage => PageIndex > 1;
        public bool HasNextPage => PageIndex < TotalPages;

        // Phương thức tạo tĩnh
        public static async Task<PaginatedList<T>> CreateAsync(IQueryable<T> source, int pageIndex, int pageSize)
        {
            var count = await source.CountAsync(); // Đếm tổng số item
            // Đảm bảo pageIndex hợp lệ
            pageIndex = Math.Max(1, pageIndex);
            // Đảm bảo pageSize hợp lệ
            pageSize = Math.Max(1, pageSize);
            // Tính toán lại TotalPages dựa trên count và pageSize thực tế
            int totalPages = (int)Math.Ceiling(count / (double)pageSize);
            // Đảm bảo pageIndex không vượt quá totalPages (trừ khi không có item nào)
            pageIndex = Math.Min(pageIndex, totalPages > 0 ? totalPages : 1);

            var items = await source.Skip((pageIndex - 1) * pageSize).Take(pageSize).ToListAsync(); // Lấy dữ liệu cho trang hiện tại
            return new PaginatedList<T>(items, count, pageIndex, pageSize);
        }
    }
}