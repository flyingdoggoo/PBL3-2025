using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PBL3.Data;
using PBL3.Models;
using System;
using System.Threading.Tasks;

namespace PBL3.Controllers
{
    [Authorize] // Yêu cầu đăng nhập để đặt vé
    public class BookingController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<AppUser> _userManager;

        public BookingController(ApplicationDbContext context, UserManager<AppUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: Booking/SelectFlight?flightId=5
        [HttpGet]
        public async Task<IActionResult> SelectFlight(int? flightId)
        {
            if (flightId == null) return BadRequest("Thiếu mã chuyến bay.");

            var flight = await _context.Flights.FindAsync(flightId);
            if (flight == null) return NotFound("Không tìm thấy chuyến bay.");

            // Kiểm tra lại xem còn chỗ không
            if (flight.AvailableSeats <= 0)
            {
                TempData["ErrorMessage"] = "Rất tiếc, chuyến bay này đã hết chỗ khi bạn chọn.";
                // Chuyển hướng về trang chi tiết hoặc tìm kiếm
                return RedirectToAction("Details", "FlightSearch", new { id = flightId });
            }

            // Lấy thông tin người dùng đang đăng nhập
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                // Trường hợp hiếm gặp: user bị xóa sau khi đăng nhập?
                return Challenge(); // Yêu cầu đăng nhập lại
            }

            // Hiển thị trang xác nhận thông tin trước khi đặt
            ViewData["Flight"] = flight;
            ViewData["Passenger"] = user;

            return View();
        }

        // POST: Booking/ConfirmBooking
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmBooking(int flightId) // Nhận flightId từ form ẩn
        {
            if (!ModelState.IsValid) // Kiểm tra nếu có validation khác
            {
                // Xử lý lỗi validation (nếu có form phức tạp hơn)
                TempData["ErrorMessage"] = "Dữ liệu không hợp lệ.";
                return RedirectToAction("SelectFlight", new { flightId = flightId }); // Quay lại trang chọn
            }


            var flight = await _context.Flights.FirstOrDefaultAsync(f => f.FlightId == flightId); // Theo dõi để update
            if (flight == null) return NotFound("Không tìm thấy chuyến bay.");

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();


            // *** KIỂM TRA LẠI TRONG GIAO DỊCH ***
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // Lock row hoặc kiểm tra lại AvailableSeats một cách an toàn hơn nếu cần xử lý concurrency cao
                if (flight.AvailableSeats <= 0)
                {
                    await transaction.RollbackAsync();
                    TempData["ErrorMessage"] = "Rất tiếc, đã có người khác đặt hết chỗ trong lúc bạn xác nhận.";
                    return RedirectToAction("Details", "FlightSearch", new { id = flightId });
                }

                // Tạo vé mới
                var newTicket = new Ticket
                {
                    PassengerId = user.Id,
                    FlightId = flight.FlightId,
                    // Lấy giá từ chuyến bay làm giá vé (có thể thêm logic phức tạp hơn)
                    Price = flight.Price,
                    OrderTime = DateTime.UtcNow, // Lưu giờ UTC
                    Status = "Booked",
                    SeatNumber = null // Logic gán số ghế có thể phức tạp hơn, tạm để null
                    // SectionId = null // Gán nếu có logic chọn section
                };
                _context.Tickets.Add(newTicket);

                // Giảm số ghế trống
                flight.AvailableSeats -= 1;
                // _context.Update(flight); // Không cần nếu flight đang được theo dõi

                // Lưu thay đổi cả hai bảng
                await _context.SaveChangesAsync();

                // Hoàn tất giao dịch
                await transaction.CommitAsync();

                // Chuyển đến trang xác nhận thành công, truyền ID vé mới
                return RedirectToAction("Confirmation", new { ticketId = newTicket.TicketId });

            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                // Log lỗi
                // logger.LogError(ex, "Error confirming booking for flight {FlightId} by user {UserId}", flightId, user.Id);
                TempData["ErrorMessage"] = "Đã xảy ra lỗi trong quá trình đặt vé. Vui lòng thử lại.";
                return RedirectToAction("SelectFlight", new { flightId = flightId }); // Quay lại trang chọn
            }
        }

        // GET: Booking/Confirmation?ticketId=10
        [HttpGet]
        public async Task<IActionResult> Confirmation(int? ticketId)
        {
            if (ticketId == null) return BadRequest("Thiếu mã vé.");

            var ticket = await _context.Tickets
                .Include(t => t.Passenger)
                .Include(t => t.Flight)
                .FirstOrDefaultAsync(t => t.TicketId == ticketId);

            if (ticket == null) return NotFound("Không tìm thấy thông tin vé.");

            // Kiểm tra xem người dùng hiện tại có phải là chủ vé không (bảo mật)
            var currentUser = await _userManager.GetUserAsync(User);
            if (ticket.PassengerId != currentUser?.Id)
            {
                return Forbid(); // Không cho phép xem vé của người khác
            }


            return View(ticket); // Truyền thông tin vé vào View
        }
    }
}