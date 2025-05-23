using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PBL3.Data;
using PBL3.Models;
using PBL3.Utils;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace PBL3.Controllers
{
    [Authorize]
    public class BookingHistoryController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<AppUser> _userManager;
        private readonly ILogger<BookingHistoryController> _logger;

        public BookingHistoryController(ApplicationDbContext context, UserManager<AppUser> userManager, ILogger<BookingHistoryController> logger)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
        }
        public async Task<IActionResult> Index(int? pageNumber)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var ticketsQuery = _context.Tickets
                        .Where(t => t.PassengerId == user.Id)
                        .Include(t => t.Flight)
                            .ThenInclude(f => f.DepartureAirport)
                        .Include(t => t.Flight)
                            .ThenInclude(f => f.ArrivalAirport)
                        .OrderByDescending(t => t.OrderTime);

            int pageSize = 5;
            var paginatedTickets = await PaginatedList<Ticket>.CreateAsync(ticketsQuery.AsNoTracking(), pageNumber ?? 1, pageSize);

            return View(paginatedTickets);
        }
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var ticket = await _context.Tickets
                     .Include(t => t.Passenger)
                     .Include(t => t.Flight).ThenInclude(f => f.DepartureAirport)
                     .Include(t => t.Flight).ThenInclude(f => f.ArrivalAirport)
                     .Include(t => t.Seat)
                     .Include(t => t.Section)
                     .AsNoTracking()
                     .FirstOrDefaultAsync(m => m.TicketId == id && m.PassengerId == user.Id);

            if (ticket == null) return NotFound("Không tìm thấy vé hoặc bạn không có quyền xem vé này.");

            return View(ticket);
        }
        [HttpGet]
        public async Task<IActionResult> CancelRequest(int? id)
        {
            if (id == null) return NotFound();

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var ticket = await _context.Tickets
                                    .Include(t => t.Flight)
                                    .FirstOrDefaultAsync(m => m.TicketId == id && m.PassengerId == user.Id);

            if (ticket == null) return NotFound("Không tìm thấy vé hoặc bạn không có quyền thao tác.");
            if (ticket.Status != TicketStatus.Booked && ticket.Status != TicketStatus.Pending_Book)
            {
                TempData["ErrorMessage"] = $"Không thể yêu cầu hủy vé ở trạng thái '{ticket.Status}'.";
                return RedirectToAction(nameof(Details), new { id = id });
            }
            if (ticket.Flight == null || ticket.Flight.StartingTime <= DateTime.Now.AddHours(24))
            {
                TempData["ErrorMessage"] = "Không thể yêu cầu hủy vé cho chuyến bay sắp khởi hành (ít hơn 24 giờ).";
                return RedirectToAction(nameof(Details), new { id = id });
            }

            return View(ticket);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitCancelRequest(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var ticket = await _context.Tickets
                                     .Include(t => t.Flight)
                                     .FirstOrDefaultAsync(m => m.TicketId == id && m.PassengerId == user.Id);

            if (ticket == null) return NotFound("Không tìm thấy vé hoặc bạn không có quyền thao tác.");
            if (ticket.Status != TicketStatus.Booked && ticket.Status != TicketStatus.Pending_Book)
            {
                TempData["ErrorMessage"] = $"Không thể yêu cầu hủy vé ở trạng thái '{ticket.Status}'.";
                return RedirectToAction(nameof(Index));
            }
            if (ticket.Flight == null || ticket.Flight.StartingTime <= DateTime.Now.AddHours(24))
            {
                TempData["ErrorMessage"] = "Không thể yêu cầu hủy vé cho chuyến bay sắp khởi hành (ít hơn 24 giờ).";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                ticket.Status = TicketStatus.Pending_Cancel;
                _context.Update(ticket);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"User {user.Email} submitted cancellation request for Ticket ID {id}.");
                TempData["SuccessMessage"] = "Yêu cầu hủy vé của bạn đã được gửi đi. Quản trị viên sẽ xem xét.";
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, $"Database error submitting cancellation request for Ticket ID {id}.");
                TempData["ErrorMessage"] = "Đã xảy ra lỗi khi gửi yêu cầu hủy vé. Vui lòng thử lại.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Generic error submitting cancellation request for Ticket ID {id}.");
                TempData["ErrorMessage"] = "Đã xảy ra lỗi không xác định. Vui lòng thử lại.";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}