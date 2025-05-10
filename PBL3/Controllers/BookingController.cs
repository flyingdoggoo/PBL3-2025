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

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("ConfirmBooking failed: ModelState is invalid. Errors: {ModelStateErrors}", GetModelStateErrors(ModelState));
                await ReloadViewModelForRetry(model, "Dữ liệu nhập không hợp lệ. Vui lòng kiểm tra lại.");
                return View("SelectSeats", model); // Or wherever your seat selection view is
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

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge(); // Or RedirectToPage("/Account/Login")

            try
            {
                // Fetch data WITHOUT transaction here, as we are just preparing for review
                var flight = await _context.Flights
                                      .Include(f => f.Sections)
                                      .Include(f => f.DepartureAirport) // For FlightDetailsViewModel
                                      .Include(f => f.ArrivalAirport)   // For FlightDetailsViewModel
                                      .AsNoTracking() // No tracking needed for review preparation
                                      .FirstOrDefaultAsync(f => f.FlightId == model.FlightId);

                if (flight == null)
                {
                    await ReloadViewModelForRetry(model, $"Chuyến bay ID {model.FlightId} không còn tồn tại.");
                    return View("SelectSeats", model);
                }

                var dbSeatsToPreview = await _context.Seats
                                              .Include(s => s.Section)
                                              .Where(s => s.Section.FlightId == model.FlightId && selectedSeatIds.Contains(s.SeatId))
                                              .AsNoTracking()
                                              .ToListAsync();

                if (dbSeatsToPreview.Count != selectedSeatIds.Count)
                {
                    var foundSeatIds = dbSeatsToPreview.Select(s => s.SeatId);
                    var missingSeatIds = selectedSeatIds.Except(foundSeatIds);
                    await ReloadViewModelForRetry(model, $"Các ghế với ID: {string.Join(", ", missingSeatIds)} không hợp lệ hoặc không tồn tại.");
                    return View("SelectSeats", model);
                }

                decimal calculatedTotalPrice = 0;
                var paymentTickets = new List<TicketPaymentViewModel>();

                for (int i = 0; i < model.Passengers.Count; i++)
                {
                    var passengerInfo = model.Passengers[i];
                    var correspondingDbSeat = dbSeatsToPreview.FirstOrDefault(s => s.SeatId == passengerInfo.SelectedSeatId);

                    if (correspondingDbSeat == null)
                    {
                        await ReloadViewModelForRetry(model, $"Lỗi: Không tìm thấy thông tin DB cho ghế đã chọn của hành khách {passengerInfo.FullName}.");
                        return View("SelectSeats", model);
                    }
                    if (correspondingDbSeat.Status != "Available") // Check status even for preview
                    {
                        await ReloadViewModelForRetry(model, $"Ghế {correspondingDbSeat.SeatNumber} (ID: {correspondingDbSeat.SeatId}) không còn khả dụng.");
                        return View("SelectSeats", model);
                    }
                    if (correspondingDbSeat.Section == null)
                    {
                        await ReloadViewModelForRetry(model, $"Lỗi: Ghế {correspondingDbSeat.SeatNumber} không thuộc Section nào.");
                        return View("SelectSeats", model);
                    }


                    var ticketPrice = flight.Price * correspondingDbSeat.Section.PriceMultiplier;
                    calculatedTotalPrice += ticketPrice;

                    paymentTickets.Add(new TicketPaymentViewModel
                    {
                        // TicketId will be 0 or unassigned, it's a preview
                        PassengerName = passengerInfo.FullName,
                        SeatNumber = correspondingDbSeat.SeatNumber,
                        SeatId = correspondingDbSeat.SeatId, // Important for ProcessPayment
                        Price = ticketPrice,
                        Section = correspondingDbSeat.Section.SectionName
                    });
                }

                var paymentViewModel = new PaymentViewModel
                {
                    FlightInfo = new FlightDetailsViewModel
                    {
                        FlightId = flight.FlightId,
                        FlightNumber = flight.FlightNumber,
                        AirlineName = flight.Airline, // Assuming Flight entity has Airline property
                        DepartureAirportName = flight.DepartureAirport?.City ?? flight.StartingDestination.ToString(), // Use Airport Name
                        ArrivalAirportName = flight.ArrivalAirport?.City ?? flight.ReachingDestination.ToString(),     // Use Airport Name
                        DepartureTime = flight.StartingTime,
                        ArrivalTime = flight.ReachingTime
                    },
                    Tickets = paymentTickets,
                    Total = calculatedTotalPrice,
                    BookerName = user.FullName, // Assuming AppUser has FullName
                    BookerEmail = user.Email
                };

                _logger.LogInformation("Prepared PaymentViewModel for Flight ID {FlightId}. Redirecting to payment review.", flight.FlightId);
                return View("PaymentReview", paymentViewModel); // Pass to the new review view
            }
            catch (Exception ex) // Catch broader exceptions for unexpected issues during preparation
            {
                _logger.LogError(ex, "Generic error during payment preparation for Flight ID {FlightId}.", model.FlightId);
                await ReloadViewModelForRetry(model, "Đã xảy ra lỗi không mong muốn khi chuẩn bị thanh toán. Vui lòng thử lại.");
                return View("SelectSeats", model);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ProcessPayment(PaymentViewModel model)
        {
            _logger.LogInformation("ProcessPayment POST received for Flight ID: {FlightId}.", model.FlightInfo?.FlightId);

            // --- BASIC VALIDATION OF INCOMING MODEL ---
            if (model == null || model.FlightInfo == null || model.Tickets == null || !model.Tickets.Any())
            {
                _logger.LogWarning("ProcessPayment failed: PaymentViewModel is invalid or incomplete.");
                TempData["ErrorMessage"] = "Thông tin thanh toán không hợp lệ. Vui lòng thử đặt lại.";
                return RedirectToAction("Index", "Home"); // Or back to flight search
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            // --- XỬ LÝ ĐẶT VÉ TRONG TRANSACTION (Logic moved from original ConfirmBooking) ---
            using var transaction = await _context.Database.BeginTransactionAsync();
            _logger.LogInformation("Database transaction started for payment of Flight ID: {FlightId}.", model.FlightInfo.FlightId);
            try
            {
                // --- RE-FETCH AND RE-VALIDATE DATA ---
                // This is crucial for security and data integrity. Don't trust the model blindly.
                var flight = await _context.Flights
                                      .Include(f => f.Sections) // For price calculation
                                      .FirstOrDefaultAsync(f => f.FlightId == model.FlightInfo.FlightId); // TRACKING NEEDED

                if (flight == null)
                {
                    throw new InvalidOperationException($"Chuyến bay ID {model.FlightInfo.FlightId} không còn tồn tại.");
                }

                var selectedSeatIdsFromModel = model.Tickets.Select(t => t.SeatId).ToList();
                var dbSeatsToBook = await _context.Seats
                                              .Include(s => s.Section)
                                              .Where(s => s.Section.FlightId == flight.FlightId && selectedSeatIdsFromModel.Contains(s.SeatId))
                                              .ToListAsync(); // TRACKING NEEDED

                if (dbSeatsToBook.Count != selectedSeatIdsFromModel.Count)
                {
                    var foundDbSeatIds = dbSeatsToBook.Select(s => s.SeatId);
                    var missingSeatIds = selectedSeatIdsFromModel.Except(foundDbSeatIds);
                    throw new InvalidOperationException($"Một hoặc nhiều ghế đã chọn không còn hợp lệ (ID: {string.Join(", ", missingSeatIds)}).");
                }

                decimal reCalculatedTotalPrice = 0;
                var createdTickets = new List<Ticket>();

                foreach (var ticketPreview in model.Tickets)
                {
                    var seatToBook = dbSeatsToBook.FirstOrDefault(s => s.SeatId == ticketPreview.SeatId);
                    if (seatToBook == null) // Should be caught by previous check, but defensive
                    {
                        throw new InvalidOperationException($"Lỗi hệ thống: Ghế ID {ticketPreview.SeatId} không tìm thấy trong DB sau khi xác thực.");
                    }

                    if (seatToBook.Status != "Available")
                    {
                        throw new InvalidOperationException($"Xin lỗi, ghế {seatToBook.SeatNumber} đã được người khác đặt trong lúc bạn xem xét. Vui lòng chọn lại.");
                    }
                    seatToBook.Status = "Booked"; // Update status
                    _context.Update(seatToBook);

                    if (seatToBook.Section == null) throw new InvalidOperationException($"Lỗi: Ghế {seatToBook.SeatNumber} không thuộc Section nào.");

                    var actualTicketPrice = flight.Price * seatToBook.Section.PriceMultiplier;
                    reCalculatedTotalPrice += actualTicketPrice; // Recalculate for safety

                    // Compare actual price with price from model - optional sanity check
                    if (actualTicketPrice != ticketPreview.Price)
                    {
                        _logger.LogWarning("Price mismatch for Seat ID {SeatId}. Model: {ModelPrice}, Calculated: {ActualPrice}. Using calculated.",
                                           seatToBook.SeatId, ticketPreview.Price, actualTicketPrice);
                    }

                    var newTicket = new Ticket
                    {
                        PassengerId = user.Id,
                        FlightId = flight.FlightId,
                        SeatId = seatToBook.SeatId,
                        SectionId = seatToBook.SectionId,
                        Price = actualTicketPrice, // Use re-calculated or validated price
                        OrderTime = DateTime.UtcNow,
                        Status = TicketStatus.Pending_Book, // Mark as Pending_Book after "payment"
                    };
                    _context.Tickets.Add(newTicket);
                    createdTickets.Add(newTicket);
                }

                // Final check of total price (optional, good for sanity)
                if (reCalculatedTotalPrice != model.Total)
                {
                    _logger.LogWarning("Total price mismatch. Model: {ModelTotal}, ReCalculated: {RecalculatedTotal}. Booking with re-calculated total.",
                                       model.Total, reCalculatedTotalPrice);
                    // You might choose to throw an error or proceed with the re-calculated total.
                }


                flight.AvailableSeats = await _context.Seats.CountAsync(s => s.Section.FlightId == flight.FlightId && s.Status == "Available");
                _context.Update(flight);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                _logger.LogInformation("Payment processed and transaction committed. {TicketCount} tickets for Flight ID {FlightId}.", createdTickets.Count, flight.FlightId);

                TempData["SuccessMessage"] = $"Thanh toán thành công cho {createdTickets.Count} hành khách! Tổng tiền: {reCalculatedTotalPrice:N0} VNĐ. Vé của bạn đã được xác nhận.";
                // Instead of redirecting to a single ticket confirmation, redirect to booking history.
                return RedirectToAction("Index", "BookingHistory"); 
            }
            catch (InvalidOperationException ex)
            {
                await transaction.RollbackAsync();
                _logger.LogWarning(ex, "Payment processing failed (InvalidOperationException) for Flight ID {FlightId}.", model.FlightInfo?.FlightId);
                TempData["ErrorMessage"] = ex.Message; // Show specific error to user
                                                       
                return RedirectToAction("SelectSeats", new { flightId = model.FlightInfo?.FlightId }); // Or "Search", "Home"
            }
            catch (DbUpdateConcurrencyException ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Concurrency error during payment processing for Flight ID {FlightId}.", model.FlightInfo?.FlightId);
                TempData["ErrorMessage"] = "Đã có lỗi xảy ra do dữ liệu bị thay đổi (có thể ghế vừa được người khác đặt). Vui lòng thử lại.";
                return RedirectToAction("SelectSeats", new { flightId = model.FlightInfo?.FlightId });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Generic error during payment processing for Flight ID {FlightId}.", model.FlightInfo?.FlightId);
                TempData["ErrorMessage"] = "Đã xảy ra lỗi không mong muốn trong quá trình xử lý thanh toán. Vui lòng thử lại sau.";
                return RedirectToAction("SelectSeats", new { flightId = model.FlightInfo?.FlightId });
            }
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