using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PBL3.Data;
using PBL3.Models;
using PBL3.Utils; // Namespace của PaginatedList
using System.Linq;
using System.Threading.Tasks;

namespace PBL3.Controllers
{
    [Authorize] // Yêu cầu đăng nhập
    public class BookingHistoryController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<AppUser> _userManager;

        public BookingHistoryController(ApplicationDbContext context, UserManager<AppUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: BookingHistory/Index
        public async Task<IActionResult> Index(int? pageNumber)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var ticketsQuery = _context.Tickets
                                .Where(t => t.PassengerId == user.Id) // Lọc vé của user hiện tại
                                .Include(t => t.Flight) // Lấy thông tin chuyến bay
                                .OrderByDescending(t => t.OrderTime); // Sắp xếp vé mới nhất lên đầu

            int pageSize = 5; // Số vé hiển thị trên mỗi trang
            var paginatedTickets = await PaginatedList<Ticket>.CreateAsync(ticketsQuery.AsNoTracking(), pageNumber ?? 1, pageSize);

            return View(paginatedTickets);
        }

        // GET: BookingHistory/Details/5 (Tái sử dụng view Booking/Confirmation hoặc tạo view riêng)
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var ticket = await _context.Tickets
                .Include(t => t.Passenger)
                .Include(t => t.Flight)
                .FirstOrDefaultAsync(m => m.TicketId == id);

            if (ticket == null) return NotFound();

            // Kiểm tra quyền xem
            var currentUser = await _userManager.GetUserAsync(User);
            if (ticket.PassengerId != currentUser?.Id) return Forbid();

            // Có thể dùng lại View của Booking/Confirmation
            return View("~/Views/Booking/Confirmation.cshtml", ticket);
        }

        // GET: BookingHistory/Cancel/5 (Có thể thêm chức năng cho phép khách tự hủy)
        // POST: BookingHistory/Cancel/5
        // Logic tương tự BookingsController.Cancel nhưng có thể thêm điều kiện (vd: chỉ hủy trước 24h)
        // Cần View riêng cho Cancel của khách.
    }
}