using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PBL3.Data;
using PBL3.Models;
using PBL3.Models.ViewModels; // Namespace chứa BookingViewModel, SeatViewModel,...
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging; // Namespace cho ILogger

namespace PBL3.Controllers
{
    [Authorize] // Yêu cầu đăng nhập cho tất cả các action trong controller này
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
        [HttpGet]
        [HttpGet]
        public async Task<IActionResult> StartBooking(int? flightId, int passengers = 1)
        {
            _logger.LogInformation("GET StartBooking called. FlightId: {FlightId}, Passengers: {Passengers}", flightId, passengers);

            // --- 1. Validation đầu vào ---
            if (flightId == null)
            {
                _logger.LogWarning("StartBooking failed: FlightId is null.");
                return BadRequest("Thiếu mã chuyến bay.");
            }
            // Đặt giới hạn hợp lý cho số lượng hành khách có thể đặt trong 1 lần
            const int maxPassengersPerBooking = 10;
            if (passengers < 1 || passengers > maxPassengersPerBooking)
            {
                _logger.LogWarning("StartBooking failed: Invalid passenger count ({PassengerCount}). Allowed range: 1-{MaxPassengers}", passengers, maxPassengersPerBooking);
                TempData["ErrorMessage"] = $"Số lượng hành khách không hợp lệ (phải từ 1 đến {maxPassengersPerBooking}).";
                // Quay về trang chủ hoặc trang tìm kiếm trước đó nếu có thông tin
                return RedirectToAction("Index", "Home");
            }

            // --- 2. Lấy thông tin chuyến bay ---
            var flight = await _context.Flights
                                      .Include(f => f.DepartureAirport) // Include để hiển thị tên/code sân bay
                                      .Include(f => f.ArrivalAirport)
                                      .AsNoTracking() // Không cần theo dõi thay đổi ở bước này
                                      .FirstOrDefaultAsync(f => f.FlightId == flightId);

            if (flight == null)
            {
                _logger.LogWarning("StartBooking failed: Flight not found for ID {FlightId}.", flightId);
                return NotFound($"Không tìm thấy chuyến bay với ID {flightId}.");
            }

            // --- 3. Lấy thông tin người dùng hiện tại ---
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                // Tình huống hiếm gặp nếu session hết hạn hoặc user bị xóa
                _logger.LogError("StartBooking failed: User not found for authenticated principal.");
                return Challenge(); // Yêu cầu đăng nhập lại
            }

            // --- 4. Kiểm tra số ghế trống ---
            if (flight.AvailableSeats < passengers)
            {
                _logger.LogWarning("StartBooking failed: Not enough available seats for Flight ID {FlightId}. Needed: {NeededSeats}, Available: {AvailableSeats}", flightId, passengers, flight.AvailableSeats);
                TempData["ErrorMessage"] = $"Chuyến bay {flight.FlightNumber} chỉ còn {flight.AvailableSeats} ghế trống, không đủ cho {passengers} hành khách.";
                // Chuyển hướng đến trang chi tiết để người dùng xem lại
                return RedirectToAction("Details", "FlightSearch", new { id = flightId });
            }

            // --- 5. Lấy và Sắp xếp dữ liệu ghế ---
            _logger.LogInformation("Fetching and sorting seats for Flight ID: {FlightId}", flightId);
            List<Seat> seatsList;
            try
            {
                // Lấy dữ liệu từ DB
                seatsList = await _context.Seats
                                        .Where(s => s.FlightId == flightId)
                                        .AsNoTracking()
                                        .ToListAsync();

                // Sắp xếp trong bộ nhớ (In-Memory) để tránh lỗi Expression Tree
                seatsList = seatsList
                            .OrderBy(s => s.SeatNumber?.Length ?? int.MaxValue) // Xử lý SeatNumber null
                            .ThenBy(s => int.TryParse(new string((s.SeatNumber ?? "").Where(char.IsDigit).ToArray()), out int r) ? r : int.MaxValue) // Sắp theo số hàng
                            .ThenBy(s => GetSeatColumnSortOrder(new string((s.SeatNumber ?? "").Where(char.IsLetter).ToArray()))) // Sắp theo cột
                            .ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching or sorting seats for Flight ID {FlightId}", flightId);
                TempData["ErrorMessage"] = "Đã xảy ra lỗi khi tải sơ đồ ghế. Vui lòng thử lại.";
                return RedirectToAction("Details", "FlightSearch", new { id = flightId });
            }


            _logger.LogInformation("Found and sorted {SeatCount} seats for Flight ID {FlightId}.", seatsList.Count, flightId);

            // Kiểm tra xem có dữ liệu ghế không (sau khi lấy từ DB)
            if (!seatsList.Any())
            {
                _logger.LogError("StartBooking failed: No seat records found in database for Flight ID {FlightId}. Check data seeding.", flightId);
                TempData["ErrorMessage"] = "Lỗi nghiêm trọng: Không tìm thấy dữ liệu ghế cho chuyến bay này. Vui lòng liên hệ bộ phận hỗ trợ.";
                // Có thể chuyển hướng về trang lỗi hoặc trang chi tiết
                return RedirectToAction("Details", "FlightSearch", new { id = flightId });
            }

            // --- 6. Tạo ViewModel ---
            _logger.LogInformation("Creating BookingViewModel...");
            var viewModel = new BookingViewModel
            {
                FlightId = flight.FlightId,
                FlightInfo = flight, // Truyền thông tin chuyến bay đã lấy
                BookerInfo = user,  // Truyền thông tin người đặt vé
                AvailableSeats = seatsList.Select(s => MapSeatToViewModel(s)).ToList(), // Map danh sách ghế đã sắp xếp
                Passengers = Enumerable.Range(0, passengers).Select(i => // Tạo list PassengerInfoViewModel
                    (i == 0)
                        ? new PassengerInfoViewModel { FullName = user.FullName, Age = user.Age } // Người đầu tiên lấy thông tin từ người đặt
                        : new PassengerInfoViewModel() // Những người khác để trống
                ).ToList(),
                EstimatedTotalPrice = flight.Price * passengers // Tính giá ước tính
            };
            _logger.LogInformation("BookingViewModel created with {PassengerCount} passengers and {SeatCount} seat models.", viewModel.Passengers.Count, viewModel.AvailableSeats.Count);

            // --- 7. Trả về View ---
            return View("SelectSeats", viewModel); // Chỉ định rõ tên View và truyền ViewModel
        }

        // --- Private Helper Methods ---

        // Hàm map từ Seat (Entity) sang SeatViewModel (cho View)
        private SeatViewModel MapSeatToViewModel(Seat seat)
        {
            string seatNumStr = seat?.SeatNumber ?? "";
            string col = string.Empty;
            int row = 0;

            if (!string.IsNullOrEmpty(seatNumStr))
            {
                col = new string(seatNumStr.Where(char.IsLetter).ToArray());
                string rowStr = new string(seatNumStr.Where(char.IsDigit).ToArray());
                int.TryParse(rowStr, out row); // Gán 0 nếu không parse được

                if (row == 0 || string.IsNullOrEmpty(col))
                {
                    _logger.LogWarning("Could not properly parse Row/Column for SeatNumber: {SeatNumber} (SeatId: {SeatId})", seatNumStr, seat?.SeatId);
                }
            }
            else
            {
                _logger.LogWarning("SeatNumber is null or empty for SeatId: {SeatId}", seat?.SeatId);
            }

            return new SeatViewModel
            {
                SeatId = seat?.SeatId ?? 0,
                SeatNumber = seatNumStr,
                Status = seat?.Status ?? "Unavailable", // Trạng thái mặc định nếu seat null
                SeatType = seat?.SeatType,
                Row = row,
                Column = col
            };
        }

        // Hàm lấy thứ tự sắp xếp cho cột ghế
        private static int GetSeatColumnSortOrder(string column)
        {
            // Chuyển về chữ hoa và xử lý null/rỗng
            return column?.ToUpperInvariant() switch // Dùng ToUpperInvariant để ổn định hơn
            {
                "A" => 1,
                "B" => 2,
                "C" => 3,
                "D" => 4,
                "E" => 5,
                "F" => 6,
                "G" => 7,
                "H" => 8,
                "K" => 9, // Thêm cột K nếu layout có
                _ => 99 // Các cột không xác định xếp cuối
            };
        }

        // POST: Booking/ConfirmBooking
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmBooking(BookingViewModel model)
        {
            _logger.LogInformation("ConfirmBooking POST received for Flight ID: {FlightId}", model.FlightId);

            // --- VALIDATION PHÍA SERVER ---
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("ConfirmBooking failed: ModelState is invalid.");
                await ReloadViewModelForRetry(model);
                return View("SelectSeats", model);
            }
            if (model.Passengers == null || !model.Passengers.Any())
            {
                ModelState.AddModelError("", "Danh sách hành khách không được rỗng.");
                _logger.LogWarning("ConfirmBooking failed: Passenger list is empty.");
                await ReloadViewModelForRetry(model);
                return View("SelectSeats", model);
            }

            // Lấy danh sách SỐ GHẾ (string) mà người dùng đã chọn từ các input ẩn
            var selectedSeatNumbers = model.Passengers
                                            .Select(p => p.SelectedSeat)
                                            .Where(s => !string.IsNullOrEmpty(s))
                                            .ToList();

            // Kiểm tra số lượng ghế chọn có đủ không
            if (selectedSeatNumbers.Count != model.Passengers.Count)
            {
                ModelState.AddModelError("", $"Vui lòng chọn đủ {model.Passengers.Count} ghế cho tất cả hành khách.");
                _logger.LogWarning("ConfirmBooking failed: Not enough seats selected. Needed: {Needed}, Selected: {Selected}", model.Passengers.Count, selectedSeatNumbers.Count);
                await ReloadViewModelForRetry(model);
                return View("SelectSeats", model);
            }
            // Kiểm tra ghế trùng lặp trong danh sách người dùng chọn
            if (selectedSeatNumbers.Distinct().Count() != selectedSeatNumbers.Count)
            {
                ModelState.AddModelError("", "Lỗi: Bạn đã chọn trùng ghế cho nhiều hành khách.");
                _logger.LogWarning("ConfirmBooking failed: Duplicate seat selection detected in input: {SelectedSeats}", string.Join(", ", selectedSeatNumbers));
                await ReloadViewModelForRetry(model);
                return View("SelectSeats", model);
            }

            // --- XỬ LÝ ĐẶT VÉ TRONG TRANSACTION ---
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            using var transaction = await _context.Database.BeginTransactionAsync();
            _logger.LogInformation("Database transaction started for booking Flight ID: {FlightId}.", model.FlightId);
            try
            {
                // Lấy chuyến bay (có tracking để update AvailableSeats)
                var flight = await _context.Flights.FirstOrDefaultAsync(f => f.FlightId == model.FlightId);
                if (flight == null) throw new InvalidOperationException($"Chuyến bay với ID {model.FlightId} không còn tồn tại.");

                // **Lấy các Seat object từ DB dựa trên SeatNumber người dùng chọn (có tracking)**
                var dbSeatsToBook = await _context.Seats
                                              .Where(s => s.FlightId == model.FlightId && selectedSeatNumbers.Contains(s.SeatNumber))
                                              .ToListAsync();

                // --- Kiểm tra logic nghiệp vụ quan trọng BÊN TRONG transaction ---

                // 1. Kiểm tra lại số ghế trống tổng thể
                if (flight.AvailableSeats < model.Passengers.Count)
                {
                    throw new InvalidOperationException($"Không đủ ghế trống (cần {model.Passengers.Count}, chỉ còn {flight.AvailableSeats}).");
                }

                // 2. Kiểm tra xem có lấy đủ số ghế từ DB không (số ghế gửi lên có tồn tại không)
                if (dbSeatsToBook.Count != selectedSeatNumbers.Count)
                {
                    var foundSeatNumbers = dbSeatsToBook.Select(s => s.SeatNumber);
                    var missingSeats = selectedSeatNumbers.Except(foundSeatNumbers);
                    _logger.LogWarning("ConfirmBooking failed: Some selected seats not found in DB. Missing: {MissingSeats}", string.Join(", ", missingSeats));
                    throw new InvalidOperationException($"Các số ghế sau không hợp lệ hoặc không tồn tại trên chuyến bay này: {string.Join(", ", missingSeats)}");
                }

                // 3. Kiểm tra trạng thái 'Available' của từng ghế sẽ đặt
                foreach (var seatToBook in dbSeatsToBook)
                {
                    if (seatToBook.Status != "Available")
                    {
                        _logger.LogWarning("ConfirmBooking failed: Seat {SeatNumber} (ID: {SeatId}) is not Available (Status: {Status}).", seatToBook.SeatNumber, seatToBook.SeatId, seatToBook.Status);
                        throw new InvalidOperationException($"Ghế {seatToBook.SeatNumber} đã được người khác đặt hoặc không khả dụng. Vui lòng chọn lại.");
                    }
                    // Cập nhật trạng thái ghế -> Booked
                    seatToBook.Status = "Booked";
                    _context.Update(seatToBook); // Đánh dấu để EF cập nhật khi SaveChanges
                    _logger.LogInformation("Seat {SeatNumber} (ID: {SeatId}) status marked as Booked.", seatToBook.SeatNumber, seatToBook.SeatId);
                }

                // --- Tạo các vé mới ---
                List<Ticket> createdTickets = new List<Ticket>();
                decimal calculatedTotalPrice = 0;

                // Lặp qua thông tin hành khách mà người dùng gửi lên
                foreach (var passengerInfo in model.Passengers)
                {
                    // Tìm Seat object tương ứng với SeatNumber của hành khách này từ list dbSeatsToBook đã lấy
                    var correspondingDbSeat = dbSeatsToBook.FirstOrDefault(s => s.SeatNumber == passengerInfo.SelectedSeat);
                    if (correspondingDbSeat == null) // Kiểm tra lại cho chắc chắn (dù khó xảy ra nếu logic trước đúng)
                    {
                        throw new InvalidOperationException($"Lỗi nội bộ: Không tìm thấy chi tiết ghế cho số ghế {passengerInfo.SelectedSeat}.");
                    }

                    // Tạo đối tượng Ticket mới
                    var newTicket = new Ticket
                    {
                        PassengerId = user.Id, // Gán vé cho người đang đăng nhập đặt vé
                        FlightId = flight.FlightId,
                        SeatId = correspondingDbSeat.SeatId, // *** GÁN SeatId TỪ dbSeat TƯƠNG ỨNG ***
                        Price = flight.Price, // Lấy giá cơ sở (TODO: Tính giá theo loại ghế/hạng nếu cần)
                        OrderTime = DateTime.UtcNow,
                        Status = "Booked",
                        SectionId = correspondingDbSeat.SectionId // Lấy SectionId từ ghế (nếu có)
                    };

                    _context.Tickets.Add(newTicket); // Thêm vé mới vào context
                    createdTickets.Add(newTicket);   // Thêm vào list để lấy ID sau này
                    calculatedTotalPrice += newTicket.Price; // Tính tổng giá

                    _logger.LogInformation("Ticket object created for Passenger: {PassengerName}, SeatID: {SeatId} (Number: {SeatNumber})",
                        passengerInfo.FullName, correspondingDbSeat.SeatId, correspondingDbSeat.SeatNumber);
                }

                // --- Cập nhật số ghế trống của chuyến bay ---
                flight.AvailableSeats -= model.Passengers.Count;
                _logger.LogInformation("Flight {FlightId} AvailableSeats updated to {AvailableSeats}", flight.FlightId, flight.AvailableSeats);
                // Không cần _context.Update(flight) vì flight đang được DbContext theo dõi

                // --- Lưu tất cả thay đổi vào Database ---
                await _context.SaveChangesAsync(); // Lưu Seats đã update và Tickets mới

                // --- Hoàn tất Transaction ---
                await transaction.CommitAsync();
                _logger.LogInformation("Booking transaction committed successfully. {TicketCount} tickets created for Flight ID {FlightId}.", createdTickets.Count, model.FlightId);

                // --- Chuyển hướng đến trang xác nhận ---
                TempData["SuccessMessage"] = $"Đặt vé thành công cho {createdTickets.Count} hành khách!";
                // Lấy ID của vé đầu tiên làm tham số chuyển hướng (có thể thay đổi nếu muốn hiển thị trang tổng hợp)
                return RedirectToAction("Confirmation", new { ticketId = createdTickets.FirstOrDefault()?.TicketId ?? 0 });

            }
            catch (InvalidOperationException ex) // Bắt lỗi nghiệp vụ đã throw ở trên
            {
                await transaction.RollbackAsync();
                _logger.LogWarning(ex, "Booking failed due to business rule violation during transaction for Flight ID {FlightId}.", model.FlightId);
                ModelState.AddModelError("", ex.Message); // Hiển thị lỗi cụ thể cho người dùng
                await ReloadViewModelForRetry(model); // Tải lại dữ liệu ghế mới nhất
                return View("SelectSeats", model); // Quay lại trang chọn ghế
            }
            catch (DbUpdateConcurrencyException ex) // Bắt lỗi concurrency (có người khác sửa dữ liệu cùng lúc)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Concurrency error during booking transaction for Flight ID {FlightId}.", model.FlightId);
                ModelState.AddModelError("", "Đã có lỗi xảy ra do dữ liệu bị thay đổi bởi người khác. Vui lòng thử lại.");
                await ReloadViewModelForRetry(model);
                return View("SelectSeats", model);
            }
            catch (DbUpdateException ex) // Bắt lỗi chung khi lưu vào DB
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Database update error during booking transaction for Flight ID {FlightId}.", model.FlightId);
                ModelState.AddModelError("", "Đã xảy ra lỗi khi lưu thông tin đặt vé. Vui lòng thử lại.");
                await ReloadViewModelForRetry(model);
                return View("SelectSeats", model);
            }
            catch (Exception ex) // Bắt các lỗi không mong muốn khác
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Generic error during booking transaction for Flight ID {FlightId}.", model.FlightId);
                ModelState.AddModelError("", "Đã xảy ra lỗi không mong muốn trong quá trình đặt vé. Vui lòng thử lại sau.");
                await ReloadViewModelForRetry(model);
                return View("SelectSeats", model);
            }
        } // Đóng action ConfirmBooking

        // GET: Booking/Confirmation?ticketId=10 (Giữ nguyên code cũ)
        [HttpGet]
        public async Task<IActionResult> Confirmation(int? ticketId)
        {
            _logger.LogInformation("Confirmation page requested for Ticket ID: {TicketId}", ticketId);
            if (ticketId == null) return BadRequest("Thiếu mã vé.");

            var ticket = await _context.Tickets
                            .Include(t => t.Passenger)
                            .Include(t => t.Flight).ThenInclude(f => f.DepartureAirport)
                            .Include(t => t.Flight).ThenInclude(f => f.ArrivalAirport)
                            .Include(t => t.Seat)    // <<< ĐẢM BẢO CÓ INCLUDE NÀY
                            .Include(t => t.Section) // <<< THÊM INCLUDE NÀY NẾU DÙNG SECTION
                            .AsNoTracking()
                            .FirstOrDefaultAsync(t => t.TicketId == ticketId);

            if (ticket == null)
            {
                _logger.LogWarning("Confirmation failed: Ticket not found for ID {TicketId}", ticketId);
                return NotFound("Không tìm thấy thông tin vé.");
            }

            var currentUser = await _userManager.GetUserAsync(User);
            if (ticket.PassengerId != currentUser?.Id)
            {
                _logger.LogWarning("Authorization failed: User {UserId} attempted to view ticket {TicketId} owned by {OwnerId}", currentUser?.Id, ticketId, ticket.PassengerId);
                return Forbid(); // Không cho xem vé người khác
            }

            return View(ticket);
        }


        // Hàm tải lại dữ liệu cho ViewModel khi cần quay lại View SelectSeats
        private async Task ReloadViewModelForRetry(BookingViewModel model)
        {
            _logger.LogInformation("Reloading ViewModel data for Flight ID: {FlightId} after error.", model.FlightId);
            if (model.FlightId <= 0)
            {
                _logger.LogError("Cannot reload ViewModel: Invalid FlightId {FlightId}.", model.FlightId);
                return;
            }

            var flight = await _context.Flights
                                       .Include(f => f.DepartureAirport)
                                       .Include(f => f.ArrivalAirport)
                                       .AsNoTracking()
                                       .FirstOrDefaultAsync(f => f.FlightId == model.FlightId);
            var user = await _userManager.GetUserAsync(User);
            var seatsQuery = _context.Seats.Where(s => s.FlightId == model.FlightId);
            var seatsList = await seatsQuery.AsNoTracking().ToListAsync(); // Lấy về bộ nhớ

            // Sắp xếp trong bộ nhớ
            var sortedSeats = seatsList
                           .OrderBy(s => s.SeatNumber?.Length ?? int.MaxValue)
                           .ThenBy(s => int.TryParse(new string((s.SeatNumber ?? "").Where(char.IsDigit).ToArray()), out int r) ? r : int.MaxValue)
                           .ThenBy(s => GetSeatColumnSortOrder(new string((s.SeatNumber ?? "").Where(char.IsLetter).ToArray())))
                           .ToList();


            model.FlightInfo = flight;
            model.BookerInfo = user;
            model.AvailableSeats = sortedSeats.Select(s => MapSeatToViewModel(s)).ToList();
            // **Quan trọng:** Giữ lại thông tin hành khách đã nhập từ model POST lên
            // model.Passengers = model.Passengers; // Không cần gán lại chính nó

            if (flight != null && model.Passengers != null)
            {
                model.EstimatedTotalPrice = flight.Price * model.Passengers.Count;
            }
            else
            {
                model.EstimatedTotalPrice = 0;
                _logger.LogWarning("Could not recalculate EstimatedTotalPrice in ReloadViewModelForRetry.");
            }
        }
    }
}