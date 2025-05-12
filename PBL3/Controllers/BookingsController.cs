using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity; // For UserManager
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PBL3.Data;
using PBL3.Models; // Assuming AppUser is in PBL3.Models
using PBL3.Utils;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace PBL3.Controllers
{
    //[Authorize(Roles = "Admin,Employee")] // Default authorization for this controller
    public class BookingsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<BookingsController> _logger;
        private readonly UserManager<AppUser> _userManager; // Inject UserManager


        public BookingsController(ApplicationDbContext context, ILogger<BookingsController> logger, UserManager<AppUser> userManager)
        {
            _context = context;
            _logger = logger;
            _userManager = userManager; // Assign injected UserManager
        }

        // GET: Bookings/Index (Quản lý danh sách vé)
        // Inherits [Authorize(Roles = "Admin,Employee")] - Correct
        [Authorize(Roles = "Admin,Employee")]
        public async Task<IActionResult> Index(string sortOrder, string currentFilter, string searchString, int? pageNumber)
        {
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
            switch (sortOrder)
            {
                case "passenger_desc": ticketsQuery = ticketsQuery.OrderByDescending(t => t.Passenger.FullName); break;
                case "Flight": ticketsQuery = ticketsQuery.OrderBy(t => t.Flight.FlightNumber); break;
                case "flight_desc": ticketsQuery = ticketsQuery.OrderByDescending(t => t.Flight.FlightNumber); break;
                case "Date": ticketsQuery = ticketsQuery.OrderBy(t => t.OrderTime); break;
                case "date_desc": ticketsQuery = ticketsQuery.OrderByDescending(t => t.OrderTime); break;
                case "Status": ticketsQuery = ticketsQuery.OrderBy(t => t.Status); break;
                case "status_desc": ticketsQuery = ticketsQuery.OrderByDescending(t => t.Status); break;
                default: ticketsQuery = ticketsQuery.OrderByDescending(t => t.OrderTime); break;
            }

            int pageSize = 10;
            var paginatedTickets = await PaginatedList<Ticket>.CreateAsync(ticketsQuery.AsNoTracking(), pageNumber ?? 1, pageSize);

            return View(paginatedTickets);
        }

        // GET: Bookings/Details/5 (Admin xem chi tiết)
        // Inherits [Authorize(Roles = "Admin,Employee")] - Correct
        [Authorize(Roles = "Admin,Employee")]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var ticket = await _context.Tickets
                        .Include(t => t.Passenger)
                        .Include(t => t.Flight).ThenInclude(f => f.DepartureAirport)
                        .Include(t => t.Flight).ThenInclude(f => f.ArrivalAirport)
                        .Include(t => t.Section)
                        .Include(t => t.BookingEmployee) // To see which employee booked/confirmed
                        .Include(t => t.Seat)
                        .AsNoTracking()
                        .FirstOrDefaultAsync(m => m.TicketId == id);

            if (ticket == null) return NotFound();

            return View(ticket);
        }

        // GET: Bookings/PendingCancellations (Admin xem danh sách chờ hủy)
        // Inherits [Authorize(Roles = "Admin,Employee")] - Correct
        [Authorize(Roles = "Admin,Employee")]
        [HttpGet]
        public async Task<IActionResult> PendingCancellations(int? pageNumber)
        {
            var pendingTicketsQuery = _context.Tickets
                                        .Where(t => t.Status == TicketStatus.Pending_Cancel)
                                        .Include(t => t.Passenger)
                                        .Include(t => t.Flight)
                                        .OrderBy(t => t.OrderTime);

            int pageSize = 10;
            var paginatedTickets = await PaginatedList<Ticket>.CreateAsync(pendingTicketsQuery.AsNoTracking(), pageNumber ?? 1, pageSize);

            return View(paginatedTickets);
        }

        // GET: Bookings/ConfirmCancel/5 (Admin xem thông tin trước khi xác nhận hủy)
        // Inherits [Authorize(Roles = "Admin,Employee")] - Correct
        [Authorize(Roles = "Admin,Employee")]
        [HttpGet]
        public async Task<IActionResult> ConfirmCancel(int? id)
        {
            if (id == null) return NotFound();

            var ticket = await _context.Tickets
                                    .Include(t => t.Passenger)
                                    .Include(t => t.Flight)
                                    .FirstOrDefaultAsync(m => m.TicketId == id);

            if (ticket == null) return NotFound();

            if (ticket.Status != TicketStatus.Pending_Cancel)
            {
                TempData["WarningMessage"] = "Vé này không ở trạng thái 'Chờ hủy'.";
                return RedirectToAction(nameof(PendingCancellations));
            }

            return View(ticket);
        }

        // POST: Bookings/ConfirmCancelPost/5 (Admin xác nhận hủy)
        // Inherits [Authorize(Roles = "Admin,Employee")] - Correct
        [HttpPost]
        [ValidateAntiForgeryToken]
        [ActionName("ConfirmCancel")]
        [Authorize(Roles = "Admin,Employee")]
        public async Task<IActionResult> ConfirmCancelPost(int id)
        {
            // save which employee confirmed the cancellation
            var ticket = await _context.Tickets
                                     .Include(t => t.Flight)
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
                ticket.Status = TicketStatus.Cancelled;

                if (ticket.Seat != null)
                {
                    ticket.Seat.Status = "Available";
                    ticket.Seat.TicketId = null;
                    _logger.LogInformation($"Seat ID {ticket.Seat.SeatId} status updated to Available for cancelled Ticket ID {id}.");
                }
                else
                {
                    _logger.LogWarning($"Seat was null for Ticket ID {id} during cancellation. Cannot update seat status.");
                }

                if (ticket.Flight != null)
                {
                    if (ticket.Flight.AvailableSeats < ticket.Flight.Capacity)
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
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                _logger.LogInformation($"Admin/Employee confirmed cancellation for Ticket ID {id}.");
                TempData["SuccessMessage"] = $"Đã xác nhận hủy vé {id} thành công.";

                return RedirectToAction(nameof(PendingCancellations));
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, $"Error confirming cancellation for Ticket ID {id}.");
                TempData["ErrorMessage"] = "Đã xảy ra lỗi khi xác nhận hủy vé.";
                return RedirectToAction(nameof(ConfirmCancel), new { id = id });
            }
        }

   
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Employee")]
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
                ticket.Status = TicketStatus.Booked;
                _context.Update(ticket);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Admin/Employee rejected cancellation request for Ticket ID {id}.");
                TempData["InfoMessage"] = $"Đã từ chối yêu cầu hủy vé {id}. Vé đã được khôi phục trạng thái 'Đã đặt'.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error rejecting cancellation for Ticket ID {id}.");
                TempData["ErrorMessage"] = "Đã xảy ra lỗi khi từ chối yêu cầu hủy vé.";
            }

            return RedirectToAction(nameof(PendingCancellations));
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Employee")] 
        public async Task<IActionResult> ConfirmBookingByEmployee(int id) // id là TicketId
        {
            var ticket = await _context.Tickets
                                     .Include(t => t.Flight)
                                     .FirstOrDefaultAsync(t => t.TicketId == id);

            if (ticket == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy vé.";
                return RedirectToAction(nameof(Index));
            }

            if (ticket.Status != TicketStatus.Pending_Book)
            {
                TempData["WarningMessage"] = $"Vé này không ở trạng thái chờ xác nhận (Trạng thái hiện tại: {ticket.Status}).";
                return RedirectToAction(nameof(Details), new { id = ticket.TicketId });
            }

            if (ticket.Flight == null)
            {
                TempData["ErrorMessage"] = "Lỗi: Không tìm thấy thông tin chuyến bay của vé này.";
                return RedirectToAction(nameof(Details), new { id = ticket.TicketId });
            }

            if (ticket.Flight.AvailableSeats <= 0)
            {
                TempData["ErrorMessage"] = "Rất tiếc, chuyến bay này đã hết chỗ. Không thể xác nhận vé.";
                return RedirectToAction(nameof(Details), new { id = ticket.TicketId });
            }

            AppUser? currentUser = null; // Initialize currentUser
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                ticket.Status = TicketStatus.Booked;

                currentUser = await _userManager.GetUserAsync(User); // Get current user
                if (currentUser != null)
                {
                    ticket.BookingEmployeeId = currentUser.Id;
                }
                else
                {
                    _logger.LogWarning($"Could not retrieve current user for confirming booking of Ticket ID {ticket.TicketId}. BookingEmployeeId will not be set by this action.");
                }

                ticket.Flight.AvailableSeats -= 1;

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                TempData["SuccessMessage"] = $"Đã xác nhận đặt vé thành công cho mã vé #{ticket.TicketId}.";
                _logger.LogInformation($"Employee/Admin ({currentUser?.UserName ?? "Unknown"}) confirmed booking for Ticket ID {ticket.TicketId}. Available seats for flight {ticket.Flight.FlightNumber} is now {ticket.Flight.AvailableSeats}.");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, $"Error confirming booking by employee ({currentUser?.UserName ?? "Unknown"}) for Ticket ID {ticket.TicketId}.");
                TempData["ErrorMessage"] = "Đã xảy ra lỗi trong quá trình xác nhận vé. Vui lòng thử lại.";
            }

            return RedirectToAction(nameof(Details), new { id = ticket.TicketId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Employee, Passenger")] // Allows any authenticated user
        public async Task<IActionResult> CancelPendingBookingByAnyone(int id) // id là TicketId
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                // Should not happen if [Authorize] is effective and user is logged in.
                return Challenge(); // Or Unauthorized()
            }

            var ticket = await _context.Tickets
                                     .Include(t => t.Flight)
                                     .Include(t => t.Seat)
                                     .Include(t => t.Passenger) // Crucial for ownership check if Passenger entity links to AppUser
                                     .FirstOrDefaultAsync(t => t.TicketId == id);

            if (ticket == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy vé.";
                return User.IsInRole("Admin") || User.IsInRole("Employee") ? RedirectToAction(nameof(Index)) : RedirectToAction("Index", "BookingHistory");
            }

            bool canCancel = false;
            if (User.IsInRole("Admin") || User.IsInRole("Employee"))
            {
                canCancel = true;
            }
            else // User is not Admin/Employee, so they must be the owner (Passenger)
            {
                
                if (ticket.Passenger != null && ticket.Passenger.Id == user.Id) 
                {
                    canCancel = true;
                }
            }

            if (!canCancel)
            {
                TempData["ErrorMessage"] = "Bạn không có quyền thực hiện hành động này với vé này.";
                // Redirect based on user's likely origin
                return User.IsInRole("Admin") || User.IsInRole("Employee") ? RedirectToAction(nameof(Details), new { id = id }) : RedirectToAction("Details", "BookingHistory", new { id = id });
            }


            if (ticket.Status != TicketStatus.Pending_Book)
            {
                TempData["WarningMessage"] = $"Không thể hủy vé này vì nó không ở trạng thái chờ xác nhận (Trạng thái hiện tại: {ticket.Status.ToString().Replace("_", " ")}).";
                return User.IsInRole("Admin") || User.IsInRole("Employee") ? RedirectToAction(nameof(Details), new { id = id }) : RedirectToAction("Details", "BookingHistory", new { id = id });
            }


            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                ticket.Status = TicketStatus.Cancelled;

                if (ticket.Seat != null)
                {
                    ticket.Seat.Status = "Available";
                    ticket.Seat.TicketId = null;
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
                        _logger.LogWarning($"Ticket ID {ticket.TicketId} (Pending_Book) cancelled. AvailableSeats for Flight ID {ticket.Flight.FlightId} was already at capacity.");
                    }
                }
                else
                {
                    _logger.LogError($"Flight details not found for Ticket ID {ticket.TicketId} while cancelling pending booking.");
                }


                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                TempData["SuccessMessage"] = $"Đã hủy yêu cầu đặt vé #{ticket.TicketId} thành công.";
                string cancelledByRole = User.IsInRole("Admin") || User.IsInRole("Employee") ? "Staff" : "Passenger";
                _logger.LogInformation($"{cancelledByRole} ({user.UserName}) cancelled pending booking for Ticket ID {ticket.TicketId}.");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, $"Error when {user.UserName} cancelling pending booking for Ticket ID {ticket.TicketId}.");
                TempData["ErrorMessage"] = "Đã xảy ra lỗi trong quá trình hủy vé. Vui lòng thử lại.";
            }

            // Redirect based on user's role
            if (User.IsInRole("Admin") || User.IsInRole("Employee"))
            {
                return RedirectToAction(nameof(Details), new { id = ticket.TicketId });
            }
            return RedirectToAction("Index", "BookingHistory"); // Passenger goes to their booking history
        }
    }
}