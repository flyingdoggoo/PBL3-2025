using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PBL3.Data;
using PBL3.Models;
using PBL3.Models.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace PBL3.Controllers
{
    [Authorize]
    public class BookingController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<AppUser> _userManager;
        private readonly ILogger<BookingController> _logger;

        public BookingController(ApplicationDbContext context, UserManager<AppUser> userManager, ILogger<BookingController> logger)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
        }

        // GET: Booking/StartBooking?flightId=5&passengers=2
        // Action này giờ sẽ là trang chọn ghế và nhập thông tin hành khách
        [HttpGet]
        public async Task<IActionResult> StartBooking(int? flightId, int passengers = 1)
        {
            _logger.LogInformation("GET SelectSeats called. FlightId: {FlightId}, Passengers: {Passengers}", flightId, passengers);

            if (flightId == null) return BadRequest("Thiếu mã chuyến bay.");
            const int maxPassengersPerBooking = 6; // Giới hạn số lượng hành khách
            if (passengers < 1 || passengers > maxPassengersPerBooking)
            {
                TempData["ErrorMessage"] = $"Số lượng hành khách không hợp lệ (1-{maxPassengersPerBooking}).";
                return RedirectToAction("Index", "Home");
            }

            var flight = await _context.Flights
                                      .Include(f => f.DepartureAirport)
                                      .Include(f => f.ArrivalAirport)
                                      .Include(f => f.Sections) // *** QUAN TRỌNG: Include Sections của chuyến bay ***
                                          .ThenInclude(sec => sec.Seats.Where(s => s.Status == "Available" || s.TicketId == null)) // Chỉ lấy ghế Available
                                      .AsNoTracking()
                                      .FirstOrDefaultAsync(f => f.FlightId == flightId);

            if (flight == null) return NotFound($"Không tìm thấy chuyến bay ID {flightId}.");

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            // Đếm tổng số ghế thực sự Available từ các Section đã include
            int totalActualAvailableSeats = flight.Sections.SelectMany(sec => sec.Seats).Count(s => s.Status == "Available");

            if (totalActualAvailableSeats < passengers)
            {
                TempData["ErrorMessage"] = $"Chuyến bay {flight.FlightNumber} chỉ còn {totalActualAvailableSeats} ghế trống, không đủ cho {passengers} hành khách.";
                return RedirectToAction("Details", "FlightSearch", new { id = flightId });
            }

            // --- Tạo SeatViewModel với giá đã tính toán ---
            var seatLayout = new List<SeatViewModel>();
            if (flight?.Sections != null) // Kiểm tra flight và sections không null
            {
                foreach (var section in flight.Sections.OrderBy(s => s.SectionName == "Thương gia" ? 0 : (s.SectionName == "Phổ thông" ? 1 : 2))) // Ưu tiên Thương gia, rồi Phổ thông
                {
                    // Lấy TẤT CẢ ghế của section này để vẽ sơ đồ
                    var allSeatsInSection = await _context.Seats
                                                    .Where(s => s.SectionId == section.SectionId)
                                                    .OrderBy(s => s.Row) // Sắp xếp theo hàng
                                                    .ThenBy(s => s.Column) // Rồi đến cột
                                                    .AsNoTracking() // Thêm AsNoTracking nếu chỉ đọc
                                                    .ToListAsync();

                    foreach (var dbSeat in allSeatsInSection)
                    {
                        seatLayout.Add(new SeatViewModel
                        {
                            SeatId = dbSeat.SeatId,
                            SeatNumber = dbSeat.SeatNumber,
                            Row = dbSeat.Row,
                            Column = dbSeat.Column,
                            Status = dbSeat.Status.ToString().ToLower(),
                            SectionName = section.SectionName,
                            CalculatedPrice = flight.Price * section.PriceMultiplier,
                            // Thêm IsEmergencyExit, IsNearToilet nếu có
                        });
                    }
                }
            }


            seatLayout = seatLayout
                .OrderBy(s => s.SectionName == "Thương gia" ? 0 : (s.SectionName == "Phổ thông" ? 1 : 2)) // Sắp xếp theo SectionName (TG -> PT -> Khác)
                .ThenBy(s => s.Row)
                .ThenBy(s => s.Column) // Dùng lại hàm helper nếu có
                .ToList();


            var viewModel = new BookingViewModel
            {
                FlightId = flight.FlightId,
                FlightInfo = flight,
                SeatsLayout = seatLayout, // seatsLayout đã được tạo ở trên
                FlightSections = flight.Sections.Select(s => new SectionInfoViewModel { Name = s.SectionName, PriceMultiplier = s.PriceMultiplier }).ToList(),
                Passengers = Enumerable.Range(0, passengers).Select(i =>
                    (i == 0)
                        ? new PassengerBookingInfo { FullName = user.FullName, Age = user.Age, Gender = null }
                        : new PassengerBookingInfo()
                ).ToList(),
                // EstimatedTotalPrice sẽ được tính bằng JS
            };

            return View("SelectSeats", viewModel);  // Trả về view SelectSeats.cshtml
        }


        // POST: Booking/ConfirmBooking
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmBooking(BookingViewModel model)
        {
            _logger.LogInformation("ConfirmBooking POST received for Flight ID: {FlightId} with {PassengerCount} passengers.", model.FlightId, model.Passengers?.Count);

            // --- VALIDATION ---
            if (!ModelState.IsValid) // Kiểm tra validation từ ViewModel (Annotations)
            {
                _logger.LogWarning("ConfirmBooking failed: ModelState is invalid. Errors: {ModelStateErrors}", GetModelStateErrors(ModelState));
                await ReloadViewModelForRetry(model, "Dữ liệu nhập không hợp lệ. Vui lòng kiểm tra lại.");
                return View("SelectSeats", model);
            }
            if (model.Passengers == null || !model.Passengers.Any())
            {
                await ReloadViewModelForRetry(model, "Danh sách hành khách không được rỗng.");
                return View("SelectSeats", model);
            }

            var selectedSeatIds = model.Passengers
                                        .Where(p => p.SelectedSeatId.HasValue)
                                        .Select(p => p.SelectedSeatId.Value)
                                        .ToList();

            if (selectedSeatIds.Count != model.Passengers.Count)
            {
                await ReloadViewModelForRetry(model, $"Vui lòng chọn đủ {model.Passengers.Count} ghế cho tất cả hành khách.");
                return View("SelectSeats", model);
            }
            if (selectedSeatIds.Distinct().Count() != selectedSeatIds.Count)
            {
                await ReloadViewModelForRetry(model, "Lỗi: Bạn đã chọn trùng ghế cho nhiều hành khách.");
                return View("SelectSeats", model);
            }

            // --- XỬ LÝ ĐẶT VÉ TRONG TRANSACTION ---
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            using var transaction = await _context.Database.BeginTransactionAsync();
            _logger.LogInformation("Database transaction started for Flight ID: {FlightId}.", model.FlightId);
            try
            {
                var flight = await _context.Flights
                                      .Include(f => f.Sections) // Include Sections để lấy PriceMultiplier
                                      .FirstOrDefaultAsync(f => f.FlightId == model.FlightId); // Có tracking

                if (flight == null) throw new InvalidOperationException($"Chuyến bay ID {model.FlightId} không còn tồn tại.");

                // Lấy các Seat object từ DB dựa trên SeatId người dùng chọn (có tracking)
                var dbSeatsToBook = await _context.Seats
                                              .Include(s => s.Section) // Include Section để lấy PriceMultiplier
                                              .Where(s => s.Section.FlightId == model.FlightId && selectedSeatIds.Contains(s.SeatId))
                                              .ToListAsync();

                // --- Kiểm tra logic nghiệp vụ bên trong transaction ---
                if (dbSeatsToBook.Count != selectedSeatIds.Count)
                {
                    var foundSeatIds = dbSeatsToBook.Select(s => s.SeatId);
                    var missingSeatIds = selectedSeatIds.Except(foundSeatIds);
                    throw new InvalidOperationException($"Các ghế với ID: {string.Join(", ", missingSeatIds)} không hợp lệ hoặc không tồn tại.");
                }

                decimal calculatedTotalPrice = 0;
                foreach (var seatToBook in dbSeatsToBook)
                {
                    if (seatToBook.Status != "Available")
                    {
                        throw new InvalidOperationException($"Ghế {seatToBook.SeatNumber} (ID: {seatToBook.SeatId}) đã được người khác đặt hoặc không khả dụng.");
                    }
                    seatToBook.Status = "Booked"; // Cập nhật trạng thái
                    _context.Update(seatToBook);

                    // Tính giá cho ghế này
                    if (seatToBook.Section == null) throw new InvalidOperationException($"Lỗi: Ghế {seatToBook.SeatNumber} không thuộc Section nào.");
                    calculatedTotalPrice += flight.Price * seatToBook.Section.PriceMultiplier;
                }

                // Tạo các vé mới
                var createdTickets = new List<Ticket>();
                for (int i = 0; i < model.Passengers.Count; i++)
                {
                    var passengerInfo = model.Passengers[i];
                    var correspondingDbSeat = dbSeatsToBook.FirstOrDefault(s => s.SeatId == passengerInfo.SelectedSeatId);
                    if (correspondingDbSeat == null) throw new InvalidOperationException($"Lỗi: Không tìm thấy thông tin DB cho ghế đã chọn của hành khách {passengerInfo.FullName}.");

                    var newTicket = new Ticket
                    {
                        PassengerId = user.Id, // Vé này do user hiện tại đặt
                        FlightId = flight.FlightId,
                        SeatId = correspondingDbSeat.SeatId,
                        SectionId = correspondingDbSeat.SectionId,
                        Price = flight.Price * correspondingDbSeat.Section.PriceMultiplier, // Giá vé cuối cùng
                        OrderTime = DateTime.UtcNow,
                        Status = TicketStatus.Pending_Book,
                        // Thông tin hành khách thực tế có thể lưu riêng nếu cần, hoặc dùng FullName, Age từ user
                        // PassengerNameForTicket = passengerInfo.FullName, // Ví dụ
                        // PassengerAgeForTicket = passengerInfo.Age,       // Ví dụ
                    };
                    _context.Tickets.Add(newTicket);
                    createdTickets.Add(newTicket);
                }

                // Cập nhật số ghế trống của chuyến bay
                flight.AvailableSeats = await _context.Seats.CountAsync(s => s.Section.FlightId == flight.FlightId && s.Status == "Available");
                // flight.AvailableSeats -= model.Passengers.Count; // Cách cũ nếu không quản lý từng ghế

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                _logger.LogInformation("Booking transaction committed. {TicketCount} tickets for Flight ID {FlightId}.", createdTickets.Count, model.FlightId);

                TempData["SuccessMessage"] = $"Đặt vé thành công cho {createdTickets.Count} hành khách! Tổng tiền: {calculatedTotalPrice:N0} VNĐ";
                return RedirectToAction("Confirmation", new { ticketId = createdTickets.FirstOrDefault()?.TicketId ?? 0 });
            }
            catch (InvalidOperationException ex)
            {
                await transaction.RollbackAsync();
                _logger.LogWarning(ex, "Booking failed (InvalidOperationException) for Flight ID {FlightId}.", model.FlightId);
                await ReloadViewModelForRetry(model, ex.Message);
                return View("SelectSeats", model);
            }
            catch (DbUpdateConcurrencyException ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Concurrency error during booking for Flight ID {FlightId}.", model.FlightId);
                await ReloadViewModelForRetry(model, "Đã có lỗi xảy ra do dữ liệu bị thay đổi. Vui lòng thử lại.");
                return View("SelectSeats", model);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Generic error during booking for Flight ID {FlightId}.", model.FlightId);
                await ReloadViewModelForRetry(model, "Đã xảy ra lỗi không mong muốn. Vui lòng thử lại sau.");
                return View("SelectSeats", model);
            }
        }

        // GET: Booking/Confirmation?ticketId=10
        [HttpGet]
        public async Task<IActionResult> Confirmation(int? ticketId)
        {
            // ... (Giữ nguyên code cũ, đảm bảo Include các navigation property cần thiết) ...
            // Đã thêm Include Seat và Section trong câu trả lời trước, kiểm tra lại
            _logger.LogInformation("Confirmation page requested for Ticket ID: {TicketId}", ticketId);
            if (ticketId == null) return BadRequest("Thiếu mã vé.");

            var ticket = await _context.Tickets
                            .Include(t => t.Passenger) // Passenger là người đặt vé
                            .Include(t => t.Flight).ThenInclude(f => f.DepartureAirport)
                            .Include(t => t.Flight).ThenInclude(f => f.ArrivalAirport)
                            .Include(t => t.Seat)    // Ghế cụ thể của vé này
                            .Include(t => t.Section) // Section của ghế này
                            .AsNoTracking()
                            .FirstOrDefaultAsync(t => t.TicketId == ticketId.Value);

            if (ticket == null)
            {
                _logger.LogWarning("Confirmation failed: Ticket not found for ID {TicketId}", ticketId);
                return NotFound("Không tìm thấy thông tin vé.");
            }

            var currentUser = await _userManager.GetUserAsync(User);
            if (ticket.PassengerId != currentUser?.Id) // Vé phải thuộc người đang xem
            {
                _logger.LogWarning("Authorization failed: User {UserId} attempted to view ticket {TicketId} owned by {OwnerId}", currentUser?.Id, ticketId, ticket.PassengerId);
                return Forbid();
            }

            // Nếu bạn muốn hiển thị thông tin của nhiều vé trong cùng một lần đặt (nếu có)
            // thì cần logic khác, ví dụ truyền một OrderId hoặc tìm các vé có cùng OrderTime + PassengerId
            // Hiện tại, chỉ hiển thị thông tin của 1 vé được truyền qua ticketId

            return View(ticket);
        }


        private async Task ReloadViewModelForRetry(BookingViewModel model, string errorMessage)
        {
            _logger.LogInformation("Reloading ViewModel for Flight ID {FlightId} after error: {ErrorMessage}", model.FlightId, errorMessage);
            ModelState.AddModelError("", errorMessage); // Thêm lỗi vào ModelState để View hiển thị

            if (model.FlightId <= 0) return;

            var flight = await _context.Flights
                                       .Include(f => f.DepartureAirport)
                                       .Include(f => f.ArrivalAirport)
                                       .Include(f => f.Sections)
                                            .ThenInclude(sec => sec.Seats)
                                       .AsNoTracking()
                                       .FirstOrDefaultAsync(f => f.FlightId == model.FlightId);
            var user = await _userManager.GetUserAsync(User); // Booker

            model.FlightInfo = flight;
            // model.BookerInfo = user; // BookerInfo đã có trong BookingViewModel

            var seatLayout = new List<SeatViewModel>();
            if (flight?.Sections != null)
            {
                foreach (var section in flight.Sections.OrderBy(s => s.SectionName == "Thương gia" ? 0 : 1))
                {
                    var allSeatsInSection = await _context.Seats
                                               .Where(s => s.SectionId == section.SectionId)
                                               .OrderBy(s => s.Row)
                                               .ThenBy(s => s.Column)
                                               .ToListAsync();
                    foreach (var dbSeat in allSeatsInSection)
                    {
                        seatLayout.Add(new SeatViewModel
                        {
                            SeatId = dbSeat.SeatId,
                            SeatNumber = dbSeat.SeatNumber,
                            Row = dbSeat.Row,
                            Column = dbSeat.Column,
                            Status = dbSeat.Status.ToString().ToLower(),
                            SectionName = section.SectionName,
                            CalculatedPrice = flight.Price * section.PriceMultiplier
                        });
                    }
                }
            }
            model.SeatsLayout = seatLayout.OrderBy(s => s.Row).ThenBy(s => s.Column).ToList();
            model.FlightSections = flight?.Sections.Select(s => new SectionInfoViewModel { Name = s.SectionName, PriceMultiplier = s.PriceMultiplier }).ToList() ?? new List<SectionInfoViewModel>();

            // Giữ lại thông tin hành khách đã nhập, nhưng xóa ghế đã chọn để họ chọn lại
            if (model.Passengers != null)
            {
                foreach (var p in model.Passengers)
                {
                    p.SelectedSeatId = null;
                    p.SelectedSeatNumber = null;
                }
            }
        }

        private string GetModelStateErrors(Microsoft.AspNetCore.Mvc.ModelBinding.ModelStateDictionary modelState)
        {
            return string.Join("; ", modelState.Values
                                        .SelectMany(x => x.Errors)
                                        .Select(x => x.ErrorMessage));
        }
    }
}