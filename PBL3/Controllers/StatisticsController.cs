using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PBL3.Data;
using PBL3.Models.ViewModels; // Chứa ViewModel cho Thống kê
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Globalization;
using System.Collections.Generic; // Cho List

[Authorize(Roles = "Admin,Employee")] // Cho phép cả Employee xem thống kê? (Tùy yêu cầu)
public class StatisticsController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<StatisticsController> _logger;
    public StatisticsController(ApplicationDbContext context, ILogger<StatisticsController> logger)
    {
        _context = context;
        _logger = logger;
    }

    // GET: Statistics/Revenue
    public async Task<IActionResult> Revenue(int? year, int? month)
    {
        _logger.LogInformation("Revenue statistics requested for Year: {Year}, Month: {Month}", year, month);

        int currentYear = year ?? DateTime.Now.Year;
        ViewData["SelectedYear"] = currentYear;
        ViewData["SelectedMonth"] = month;

        try // Bọc trong try-catch để xử lý lỗi truy vấn DB
        {
            // Lấy danh sách năm có dữ liệu vé (tối ưu hơn)
            ViewData["Years"] = await _context.Tickets
                                                .Select(t => t.OrderTime.Year)
                                                .Distinct()
                                                .OrderByDescending(y => y)
                                                .ToListAsync();

            // Lấy danh sách tháng
            ViewData["Months"] = Enumerable.Range(1, 12)
                                          .Select(m => new { Value = m, Name = CultureInfo.GetCultureInfo("vi-VN").DateTimeFormat.GetMonthName(m) }) // Dùng vi-VN
                                          .ToList();

            // --- Truy vấn cơ sở ---
            var ticketsQuery = _context.Tickets
                                    .Include(t => t.Flight) // *** QUAN TRỌNG: Include Flight để lấy Airline ***
                                    .Where(t => t.Status != "Cancelled"); // Chỉ tính vé hợp lệ

            // --- Áp dụng bộ lọc ---
            ticketsQuery = ticketsQuery.Where(t => t.OrderTime.Year == currentYear); // Luôn lọc theo năm

            if (month.HasValue && month.Value >= 1 && month.Value <= 12)
            {
                ticketsQuery = ticketsQuery.Where(t => t.OrderTime.Month == month.Value);
                _logger.LogInformation("Filtering by Month: {SelectedMonth}", month.Value);
            }

            // --- Thực thi truy vấn ---
            _logger.LogInformation("Executing query to get valid tickets...");
            var validTickets = await ticketsQuery.ToListAsync();
            _logger.LogInformation("Found {TicketCount} valid tickets.", validTickets.Count);


            // --- Tính toán chỉ số tổng quan ---
            decimal totalRevenue = validTickets.Sum(t => t.Price);
            int totalTicketsSold = validTickets.Count();
            decimal averageTicketPrice = totalTicketsSold > 0 ? totalRevenue / totalTicketsSold : 0;
            _logger.LogInformation("Calculated Totals - Revenue: {TotalRevenue}, Tickets: {TotalTicketsSold}, Avg Price: {AverageTicketPrice}", totalRevenue, totalTicketsSold, averageTicketPrice);


            // --- Thống kê chi tiết (theo tháng hoặc ngày) ---
            List<MonthlyRevenue> monthlyRevenueData = new List<MonthlyRevenue>();
            List<DailyRevenue> dailyRevenueData = new List<DailyRevenue>();

            if (!month.HasValue) // Xem cả năm -> thống kê theo tháng
            {
                _logger.LogInformation("Calculating monthly revenue...");
                monthlyRevenueData = validTickets
                    .GroupBy(t => t.OrderTime.Month)
                    .Select(g => new MonthlyRevenue
                    {
                        Month = g.Key,
                        MonthName = CultureInfo.GetCultureInfo("vi-VN").DateTimeFormat.GetMonthName(g.Key), // Dùng vi-VN
                        Revenue = g.Sum(t => t.Price),
                        TicketCount = g.Count()
                    })
                    .OrderBy(m => m.Month)
                    .ToList();
                _logger.LogInformation("Calculated {Count} months of revenue data.", monthlyRevenueData.Count);
            }
            else // Xem theo tháng -> thống kê theo ngày
            {
                _logger.LogInformation("Calculating daily revenue for Month {SelectedMonth}...", month.Value);
                dailyRevenueData = validTickets
                   .GroupBy(t => t.OrderTime.Day)
                   .Select(g => new DailyRevenue
                   {
                       Day = g.Key,
                       Revenue = g.Sum(t => t.Price),
                       TicketCount = g.Count()
                   })
                   .OrderBy(d => d.Day)
                   .ToList();
                _logger.LogInformation("Calculated {Count} days of revenue data.", dailyRevenueData.Count);
            }

            // --- Thống kê theo hãng bay ---
            _logger.LogInformation("Calculating revenue by airline...");
            var revenueByAirline = validTickets
               // Thêm kiểm tra Flight không null trước khi GroupBy để cực kỳ an toàn
               .Where(t => t.Flight != null && t.Flight.Airline != null)
               .GroupBy(t => t.Flight.Airline) // Không còn lỗi NullReferenceException
               .Select(g => new RevenueByAirline
               {
                   Airline = g.Key, // Không cần xử lý null ở đây nếu đã Where ở trên
                   Revenue = g.Sum(t => t.Price),
                   TicketCount = g.Count()
               })
               .OrderByDescending(a => a.Revenue)
               .ToList();
            _logger.LogInformation("Calculated revenue for {Count} airlines.", revenueByAirline.Count);


            // --- Tạo ViewModel ---
            var viewModel = new RevenueViewModel
            {
                SelectedYear = currentYear,
                SelectedMonth = month,
                TotalRevenue = totalRevenue,
                TotalTicketsSold = totalTicketsSold,
                AverageTicketPrice = averageTicketPrice,
                MonthlyRevenues = monthlyRevenueData,
                DailyRevenues = dailyRevenueData,
                RevenueByAirlines = revenueByAirline
            };

            _logger.LogInformation("Revenue statistics generated successfully.");
            return View(viewModel); // Trả về Views/Statistics/Revenue.cshtml
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while generating revenue statistics for Year: {Year}, Month: {Month}", currentYear, month);
            // Hiển thị trang lỗi thân thiện hoặc thông báo lỗi
            TempData["ErrorMessage"] = "Đã xảy ra lỗi khi tải dữ liệu thống kê. Vui lòng thử lại sau.";
            // Có thể trả về một View lỗi riêng hoặc quay về trang trước đó
            // return View("Error"); // Giả sử có View tên Error
            return RedirectToAction("Index", "SystemManager"); // Quay về Dashboard Admin
        }
    }

}

// ----- Thêm các ViewModels cần thiết (trong Models/ViewModels) -----
namespace PBL3.Models.ViewModels
{
    public class RevenueViewModel
    {
        public int SelectedYear { get; set; }
        public int? SelectedMonth { get; set; } // Nullable nếu xem cả năm
        public decimal TotalRevenue { get; set; }
        public int TotalTicketsSold { get; set; }
        public decimal AverageTicketPrice { get; set; }
        public List<MonthlyRevenue> MonthlyRevenues { get; set; } = new List<MonthlyRevenue>(); // Doanh thu theo tháng
        public List<DailyRevenue> DailyRevenues { get; set; } = new List<DailyRevenue>(); // Doanh thu theo ngày
        public List<RevenueByAirline> RevenueByAirlines { get; set; } = new List<RevenueByAirline>(); // Doanh thu theo hãng
    }

    public class MonthlyRevenue
    {
        public int Month { get; set; }
        public string MonthName { get; set; }
        public decimal Revenue { get; set; }
        public int TicketCount { get; set; }
    }
    public class DailyRevenue
    {
        public int Day { get; set; }
        public decimal Revenue { get; set; }
        public int TicketCount { get; set; }
    }

    public class RevenueByAirline
    {
        public string Airline { get; set; }
        public decimal Revenue { get; set; }
        public int TicketCount { get; set; }
    }
}