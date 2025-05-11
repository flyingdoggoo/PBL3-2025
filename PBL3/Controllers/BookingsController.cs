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
                                        .Where(t => t.Status == TicketStatus.Pending_Cancel)
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
            if (ticket.Status != TicketStatus.Pending_Cancel)
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
                                     .Include(t => t.Seat)
                                     .FirstOrDefaultAsync(t => t.TicketId == id);

            if (ticket == null) return NotFound();

            if (ticket.Status != TicketStatus.Pending_Cancel)
            {
                TempData["WarningMessage"] = "Vé này không ở trạng thái 'Chờ hủy' hoặc đã được xử lý.";
                return RedirectToAction(nameof(PendingCancellations));
            }

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                ticket.Status = TicketStatus.Cancelled; // Đổi trạng thái

                if (ticket.Seat != null)
                {
                    ticket.Seat.Status = "Available"; // Hoặc SeatStatus.Available nếu dùng enum
                    ticket.Seat.TicketId = null; // Bỏ liên kết TicketId khỏi Seat
                                                 // _context.Update(ticket.Seat); // Không cần nếu Seat đang được theo dõi
                    _logger.LogInformation($"Seat ID {ticket.Seat.SeatId} status updated to Available for cancelled Ticket ID {id}.");
                }
                else
                {
                    _logger.LogWarning($"Seat was null for Ticket ID {id} during cancellation. Cannot update seat status.");
                }

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

            if (ticket.Status != TicketStatus.Pending_Cancel)
            {
                TempData["WarningMessage"] = "Vé này không ở trạng thái 'Chờ hủy' hoặc đã được xử lý.";
                return RedirectToAction(nameof(PendingCancellations));
            }

            try
            {
                ticket.Status = TicketStatus.Booked; // Trả về trạng thái cũ
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


        //
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Employee")] // Chỉ Admin hoặc Employee được thực hiện
        public async Task<IActionResult> ConfirmBookingByEmployee(int id) // id là TicketId
        {
            var ticket = await _context.Tickets
                                     .Include(t => t.Flight) // Cần Flight để kiểm tra AvailableSeats
                                     .FirstOrDefaultAsync(t => t.TicketId == id);

            if (ticket == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy vé.";
                return RedirectToAction(nameof(Index)); // Hoặc trang Details của vé đó
            }

            // Chỉ xác nhận vé đang ở trạng thái chờ (Pending_Book hoặc trạng thái tương tự bạn định nghĩa)
            if (ticket.Status != TicketStatus.Pending_Book) // Sử dụng Enum
            {
                TempData["WarningMessage"] = $"Vé này không ở trạng thái chờ xác nhận (Trạng thái hiện tại: {ticket.Status}).";
                return RedirectToAction(nameof(Details), new { id = ticket.TicketId });
            }

            if (ticket.Flight == null)
            {
                TempData["ErrorMessage"] = "Lỗi: Không tìm thấy thông tin chuyến bay của vé này.";
                return RedirectToAction(nameof(Details), new { id = ticket.TicketId });
            }

            // Kiểm tra lại số ghế một lần nữa trước khi xác nhận (đề phòng trường hợp có người khác vừa đặt hết)
            if (ticket.Flight.AvailableSeats <= 0)
            {
                TempData["ErrorMessage"] = "Rất tiếc, chuyến bay này đã hết chỗ. Không thể xác nhận vé.";
                // Cân nhắc đổi trạng thái vé thành một trạng thái lỗi khác nếu cần
                return RedirectToAction(nameof(Details), new { id = ticket.TicketId });
            }

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // Chuyển trạng thái sang Đã đặt
                ticket.Status = TicketStatus.Booked; 
                // Optional: Ghi lại thông tin nhân viên xác nhận
                // ticket.BookingEmployeeId = _userManager.GetUserId(User); // Cần inject UserManager
                // ticket.ConfirmedByEmployeeAt = DateTime.UtcNow;
                // **QUAN TRỌNG:** Giảm số ghế trống của chuyến bay
                // Chỉ giảm nếu trước đó nó chưa được giảm (ví dụ, nếu khách tự đặt online thì AvailableSeats đã giảm)
                // Nếu quy trình của bạn là: Khách chọn -> Pending_Book -> Nhân viên xác nhận -> Booked
                // thì việc giảm AvailableSeats nên diễn ra ở bước này.
                // Tuy nhiên, nếu khách tự đặt và hệ thống tự chuyển sang Booked (sau khi thanh toán thành công)
                // thì AvailableSeats đã được giảm ở bước đó.
                // Ở đây, giả sử Pending_Book là trạng thái ban đầu và chưa trừ ghế.
                ticket.Flight.AvailableSeats -= 1;


                // _context.Update(ticket); // Không cần nếu ticket đang được DbContext theo dõi
                // _context.Update(ticket.Flight); // Không cần nếu flight đang được DbContext theo dõi

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                TempData["SuccessMessage"] = $"Đã xác nhận đặt vé thành công cho mã vé #{ticket.TicketId}.";
                _logger.LogInformation($"Employee/Admin confirmed booking for Ticket ID {ticket.TicketId}. Available seats for flight {ticket.Flight.FlightNumber} is now {ticket.Flight.AvailableSeats}.");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, $"Error confirming booking by employee for Ticket ID {ticket.TicketId}.");
                TempData["ErrorMessage"] = "Đã xảy ra lỗi trong quá trình xác nhận vé. Vui lòng thử lại.";
            }

            return RedirectToAction(nameof(Details), new { id = ticket.TicketId });
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize] // Yêu cầu đăng nhập
        public async Task<IActionResult> CancelPendingBookingByAnyone(int id) // id là TicketId
        {
           //var user = await _userManager.GetUserAsync(User);
            // if (user == null) return Challenge();

            var ticket = await _context.Tickets
                                     .Include(t => t.Flight)
                                     .Include(t => t.Seat)
                                     .FirstOrDefaultAsync(t => t.TicketId == id);

            if (ticket == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy vé.";
                // Quyết định redirect về đâu tùy theo ngữ cảnh gọi (ví dụ: nếu từ admin/employee thì về Bookings/Index)
                return User.IsInRole("Admin") || User.IsInRole("Employee") ? RedirectToAction(nameof(Index)) : RedirectToAction("Index", "BookingHistory");
            }

            // Kiểm tra quyền: Chỉ chủ vé hoặc Admin/Employee mới được hủy
           /* if (ticket.PassengerId != user.Id && !(User.IsInRole("Admin") || User.IsInRole("Employee")))
            {
                TempData["ErrorMessage"] = "Bạn không có quyền thực hiện hành động này với vé này.";
                return User.IsInRole("Admin") || User.IsInRole("Employee") ? RedirectToAction(nameof(Details), new { id = id }) : RedirectToAction("Index", "BookingHistory");
            } */

            // Chỉ cho phép hủy nếu vé đang ở trạng thái "Pending_Book"
            if (ticket.Status != TicketStatus.Pending_Book)
            {
                TempData["WarningMessage"] = $"Không thể hủy vé này vì nó không ở trạng thái chờ xác nhận (Trạng thái hiện tại: {ticket.Status.ToString().Replace("_", " ")}).";
                return User.IsInRole("Admin") || User.IsInRole("Employee") ? RedirectToAction(nameof(Details), new { id = id }) : RedirectToAction("Details", "BookingHistory", new { id = id });
            }

            // (Optional) Logic kiểm tra thời gian hủy có thể thêm ở đây

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                ticket.Status = TicketStatus.Cancelled;

                // Logic AvailableSeats:
                // Nếu quy trình là: Khách chọn -> Pending_Book (CHƯA TRỪ GHẾ) -> Nhân viên xác nhận (MỚI TRỪ GHẾ)
                // Thì khi hủy Pending_Book, KHÔNG CẦN cộng lại ghế.
                //
                // Nếu quy trình là: Khách chọn -> Pending_Book (ĐÃ TRỪ GHẾ để giữ chỗ)
                // Thì khi hủy Pending_Book, BẮT BUỘC PHẢI CỘNG LẠI GHẾ.
                // Chọn logic phù hợp với hệ thống của bạn. Giả sử ở đây là ĐÃ TRỪ GHẾ khi Pending_Book.
                if (ticket.Seat != null) 
                {
                    ticket.Seat.Status = "Available"; // Hoặc SeatStatus.Available
                    ticket.Seat.TicketId = null; // Quan trọng: Bỏ liên kết TicketId
                    _logger.LogInformation($"Seat ID {ticket.Seat.SeatId} status updated to Available for cancelled (Pending_Book) Ticket ID {id}.");
                }
                else
                {
                    _logger.LogInformation($"Ticket ID {id} (Pending_Book) did not have an associated seat or Seat object was null. No seat status to update.");
                }
                if (ticket.Flight != null)
                {
                    if (ticket.Flight.AvailableSeats < ticket.Flight.Capacity)
                    {
                        ticket.Flight.AvailableSeats += 1;
                        _logger.LogInformation($"Ticket ID {ticket.TicketId} (Pending_Book) cancelled. AvailableSeats for Flight ID {ticket.Flight.FlightId} incremented to {ticket.Flight.AvailableSeats}.");
                    }
                    else
                    {
                        _logger.LogWarning($"Ticket ID {ticket.TicketId} (Pending_Book) cancelled. AvailableSeats for Flight ID {ticket.Flight.FlightId} was already at capacity or flight was null.");
                    }
                }
                else
                {
                    _logger.LogError($"Flight details not found for Ticket ID {ticket.TicketId} while cancelling pending booking.");
                }


                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                TempData["SuccessMessage"] = $"Đã hủy yêu cầu đặt vé #{ticket.TicketId} thành công.";
                string cancelledBy = User.IsInRole("Admin") || User.IsInRole("Employee") ? "Staff" : "Passenger";
                _logger.LogInformation($"You cancelled pending booking for Ticket ID {ticket.TicketId}.");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, $"Error when you cancelling pending booking for Ticket ID {ticket.TicketId}.");
                TempData["ErrorMessage"] = "Đã xảy ra lỗi trong quá trình hủy vé. Vui lòng thử lại.";
            }

            // Điều hướng lại trang phù hợp
            if (User.IsInRole("Admin") || User.IsInRole("Employee"))
            {
                return RedirectToAction(nameof(Details), new { id = ticket.TicketId }); // Quay lại trang chi tiết vé của Admin/Emp
            }
            return RedirectToAction("Index", "BookingHistory"); // Khách hàng quay lại lịch sử đặt vé
        }
    }
}