using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity; // Thêm nếu cần UserManager/RoleManager ở đây
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PBL3.Data;
using PBL3.Models;
using PBL3.Utils;
using System; // Thêm cho Exception
using System.Linq;
using System.Threading.Tasks;

namespace PBL3.Controllers
{
    [Authorize(Roles = "Admin,Employee")] // Quyền truy cập cho quản lý
    public class BookingsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<BookingsController> _logger; // Thêm logger

        // Inject UserManager nếu cần lấy thông tin người duyệt hủy
        // private readonly UserManager<AppUser> _userManager;

        public BookingsController(ApplicationDbContext context, ILogger<BookingsController> logger /*, UserManager<AppUser> userManager*/)
        {
            _context = context;
            _logger = logger;
            // _userManager = userManager;
        }

        // GET: Bookings/Index (Quản lý danh sách vé - Giữ nguyên code trước)
        public async Task<IActionResult> Index(string sortOrder, string currentFilter, string searchString, int? pageNumber)
        {
            // ... (Code phân trang, tìm kiếm, sắp xếp đã có) ...
            ViewData["CurrentSort"] = sortOrder;
            ViewData["PassengerSortParm"] = String.IsNullOrEmpty(sortOrder) ? "passenger_desc" : "";
            ViewData["FlightSortParm"] = sortOrder == "Flight" ? "flight_desc" : "Flight";
            ViewData["DateSortParm"] = sortOrder == "Date" ? "date_desc" : "Date";
            ViewData["StatusSortParm"] = sortOrder == "Status" ? "status_desc" : "Status";

            if (searchString != null) pageNumber = 1;
            else searchString = currentFilter;
            ViewData["CurrentFilter"] = searchString;

            var ticketsQuery = _context.Tickets
                                    .Include(t => t.Passenger)
                                    .Include(t => t.Flight)
                                    .AsQueryable();

            if (!String.IsNullOrEmpty(searchString))
            {
                ticketsQuery = ticketsQuery.Where(t => t.TicketId.ToString().Contains(searchString)
                                                    || (t.Passenger != null && (t.Passenger.FullName.Contains(searchString) || t.Passenger.Email.Contains(searchString)))
                                                    || (t.Flight != null && t.Flight.FlightNumber.Contains(searchString)));
            }
            // Sắp xếp (Thêm sắp xếp theo trạng thái)
            switch (sortOrder)
            {
                case "passenger_desc": ticketsQuery = ticketsQuery.OrderByDescending(t => t.Passenger.FullName); break;
                case "Flight": ticketsQuery = ticketsQuery.OrderBy(t => t.Flight.FlightNumber); break;
                case "flight_desc": ticketsQuery = ticketsQuery.OrderByDescending(t => t.Flight.FlightNumber); break;
                case "Date": ticketsQuery = ticketsQuery.OrderBy(t => t.OrderTime); break;
                case "date_desc": ticketsQuery = ticketsQuery.OrderByDescending(t => t.OrderTime); break;
                case "Status": ticketsQuery = ticketsQuery.OrderBy(t => t.Status); break;
                case "status_desc": ticketsQuery = ticketsQuery.OrderByDescending(t => t.Status); break;
                default: ticketsQuery = ticketsQuery.OrderByDescending(t => t.OrderTime); break; // Mặc định mới nhất
            }

            int pageSize = 10;
            var paginatedTickets = await PaginatedList<Ticket>.CreateAsync(ticketsQuery.AsNoTracking(), pageNumber ?? 1, pageSize);

            return View(paginatedTickets); // Trả về Views/Bookings/Index.cshtml
        }

        // GET: Bookings/Details/5 (Admin xem chi tiết)
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var ticket = await _context.Tickets
                        .Include(t => t.Passenger)
                        .Include(t => t.Flight).ThenInclude(f => f.DepartureAirport)
                        .Include(t => t.Flight).ThenInclude(f => f.ArrivalAirport)
                        .Include(t => t.Section) // <<< THÊM INCLUDE NÀY NẾU DÙNG SECTION
                        .Include(t => t.BookingEmployee)
                        .Include(t => t.Seat)    // <<< ĐẢM BẢO CÓ INCLUDE NÀY
                        .AsNoTracking()
                        .FirstOrDefaultAsync(m => m.TicketId == id);

            if (ticket == null) return NotFound();

            return View(ticket); // Trả về Views/Bookings/Details.cshtml
        }

        // GET: Bookings/PendingCancellations (Admin xem danh sách chờ hủy)
        [HttpGet]
        public async Task<IActionResult> PendingCancellations(int? pageNumber)
        {
            var pendingTicketsQuery = _context.Tickets
                                        .Where(t => t.Status == "Pending Cancellation")
                                        .Include(t => t.Passenger)
                                        .Include(t => t.Flight)
                                        .OrderBy(t => t.OrderTime); // Sắp xếp theo thời gian yêu cầu

            int pageSize = 10;
            var paginatedTickets = await PaginatedList<Ticket>.CreateAsync(pendingTicketsQuery.AsNoTracking(), pageNumber ?? 1, pageSize);

            return View(paginatedTickets); // Trả về Views/Bookings/PendingCancellations.cshtml
        }

        // GET: Bookings/ConfirmCancel/5 (Admin xem thông tin trước khi xác nhận hủy)
        [HttpGet]
        public async Task<IActionResult> ConfirmCancel(int? id)
        {
            if (id == null) return NotFound();

            var ticket = await _context.Tickets
                                    .Include(t => t.Passenger)
                                    .Include(t => t.Flight)
                                    .FirstOrDefaultAsync(m => m.TicketId == id); // Cần theo dõi để update

            if (ticket == null) return NotFound();

            // Chỉ xử lý vé đang chờ hủy
            if (ticket.Status != "Pending Cancellation")
            {
                TempData["WarningMessage"] = "Vé này không ở trạng thái 'Chờ hủy'.";
                return RedirectToAction(nameof(PendingCancellations));
            }

            return View(ticket); // Trả về Views/Bookings/ConfirmCancel.cshtml
        }

        // POST: Bookings/ConfirmCancelPost/5 (Admin xác nhận hủy)
        [HttpPost]
        [ValidateAntiForgeryToken]
        [ActionName("ConfirmCancel")] // Dùng chung action name với GET cho form submit
        public async Task<IActionResult> ConfirmCancelPost(int id)
        {
            var ticket = await _context.Tickets
                                     .Include(t => t.Flight) // Cần include Flight để cập nhật AvailableSeats
                                     .FirstOrDefaultAsync(t => t.TicketId == id);

            if (ticket == null) return NotFound();

            if (ticket.Status != "Pending Cancellation")
            {
                TempData["WarningMessage"] = "Vé này không ở trạng thái 'Chờ hủy' hoặc đã được xử lý.";
                return RedirectToAction(nameof(PendingCancellations));
            }

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                ticket.Status = "Cancelled"; // Đổi trạng thái

                if (ticket.Flight != null)
                {
                    if (ticket.Flight.AvailableSeats < ticket.Flight.Capacity) // Chỉ tăng nếu chưa max
                    {
                        ticket.Flight.AvailableSeats += 1;
                    }
                    else
                    {
                        _logger.LogWarning($"AvailableSeats for Flight ID {ticket.FlightId} is already at Capacity ({ticket.Flight.Capacity}) when cancelling Ticket ID {id}.");
                    }
                }
                else
                {
                    _logger.LogError($"Flight not found for Ticket ID {id} during cancellation confirmation.");
                    // Cân nhắc có nên rollback không nếu không tìm thấy chuyến bay?
                }

                // Không cần gọi _context.Update() vì ticket và flight đang được theo dõi

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                _logger.LogInformation($"Admin confirmed cancellation for Ticket ID {id}.");
                TempData["SuccessMessage"] = $"Đã xác nhận hủy vé {id} thành công.";
                // TODO (Nâng cao): Gửi email xác nhận hủy cho khách hàng

                return RedirectToAction(nameof(PendingCancellations)); // Quay lại danh sách chờ hủy
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, $"Error confirming cancellation for Ticket ID {id}.");
                TempData["ErrorMessage"] = "Đã xảy ra lỗi khi xác nhận hủy vé.";
                // Quay lại trang xác nhận hủy với lỗi
                return RedirectToAction(nameof(ConfirmCancel), new { id = id });
            }
        }

        // POST: Bookings/RejectCancel/5 (Admin từ chối hủy - Tùy chọn)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectCancel(int id)
        {
            var ticket = await _context.Tickets.FirstOrDefaultAsync(t => t.TicketId == id);
            if (ticket == null) return NotFound();

            if (ticket.Status != "Pending Cancellation")
            {
                TempData["WarningMessage"] = "Vé này không ở trạng thái 'Chờ hủy' hoặc đã được xử lý.";
                return RedirectToAction(nameof(PendingCancellations));
            }

            try
            {
                ticket.Status = "Booked"; // Trả về trạng thái cũ
                _context.Update(ticket);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Admin rejected cancellation request for Ticket ID {id}.");
                TempData["InfoMessage"] = $"Đã từ chối yêu cầu hủy vé {id}. Vé đã được khôi phục trạng thái 'Đã đặt'.";
                // TODO (Nâng cao): Gửi email thông báo từ chối cho khách hàng
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error rejecting cancellation for Ticket ID {id}.");
                TempData["ErrorMessage"] = "Đã xảy ra lỗi khi từ chối yêu cầu hủy vé.";
            }

            return RedirectToAction(nameof(PendingCancellations));
        }

        // Action Cancel cũ của Admin (nếu còn) có thể bỏ đi hoặc sửa lại cho phù hợp
        // vì giờ luồng hủy là do khách yêu cầu -> admin duyệt
    }
}