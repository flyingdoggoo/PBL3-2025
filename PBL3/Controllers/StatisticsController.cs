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

    public StatisticsController(ApplicationDbContext context)
    {
        _context = context;
    }

    // GET: Statistics/Revenue
    public async Task<IActionResult> Revenue(int? year, int? month)
    {
        int currentYear = year ?? DateTime.Now.Year;
        // Nếu không chọn tháng, mặc định là xem cả năm
        ViewData["SelectedYear"] = currentYear;
        ViewData["SelectedMonth"] = month; // Giữ nguyên null nếu không chọn tháng
        ViewData["Years"] = await _context.Tickets
                                            .Select(t => t.OrderTime.Year)
                                            .Distinct()
                                            .OrderByDescending(y => y)
                                            .ToListAsync();
        ViewData["Months"] = Enumerable.Range(1, 12)
                                      .Select(m => new { Value = m, Name = CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(m) })
                                      .ToList();


        var ticketsQuery = _context.Tickets
                                .Where(t => t.Status != "Cancelled") // Chỉ tính vé hợp lệ
                                .Where(t => t.OrderTime.Year == currentYear); // Lọc theo năm đã chọn

        // Lọc thêm theo tháng nếu được chọn
        if (month.HasValue && month.Value >= 1 && month.Value <= 12)
        {
            ticketsQuery = ticketsQuery.Where(t => t.OrderTime.Month == month.Value);
        }

        // Lấy dữ liệu vé đã lọc
        var validTickets = await ticketsQuery.ToListAsync();

        // --- Tính toán các chỉ số ---
        decimal totalRevenue = validTickets.Sum(t => t.Price);
        int totalTicketsSold = validTickets.Count();
        decimal averageTicketPrice = totalTicketsSold > 0 ? totalRevenue / totalTicketsSold : 0;

        // --- Thống kê doanh thu theo từng tháng trong năm (nếu không lọc theo tháng) ---
        List<MonthlyRevenue> monthlyRevenueData = new List<MonthlyRevenue>();
        if (!month.HasValue) // Chỉ tính khi xem cả năm
        {
            monthlyRevenueData = validTickets
                .GroupBy(t => t.OrderTime.Month)
                .Select(g => new MonthlyRevenue
                {
                    Month = g.Key,
                    MonthName = CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(g.Key),
                    Revenue = g.Sum(t => t.Price),
                    TicketCount = g.Count()
                })
                .OrderBy(m => m.Month)
                .ToList();
        }

        // --- Thống kê doanh thu theo từng ngày trong tháng (nếu lọc theo tháng) ---
        List<DailyRevenue> dailyRevenueData = new List<DailyRevenue>();
        if (month.HasValue) // Chỉ tính khi xem theo tháng
        {
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
        }


        // --- Thống kê theo hãng bay (Ví dụ) ---
        var revenueByAirline = validTickets
           .GroupBy(t => t.Flight.Airline) // Cần Include(t => t.Flight) ở trên nếu chưa có
           .Select(g => new RevenueByAirline
           {
               Airline = g.Key ?? "Không xác định", // Xử lý null nếu có
               Revenue = g.Sum(t => t.Price),
               TicketCount = g.Count()
           })
           .OrderByDescending(a => a.Revenue)
           .ToList();


        // Tạo ViewModel để gửi sang View
        var viewModel = new RevenueViewModel
        {
            SelectedYear = currentYear,
            SelectedMonth = month,
            TotalRevenue = totalRevenue,
            TotalTicketsSold = totalTicketsSold,
            AverageTicketPrice = averageTicketPrice,
            MonthlyRevenues = monthlyRevenueData, // Sẽ rỗng nếu xem theo tháng
            DailyRevenues = dailyRevenueData,     // Sẽ rỗng nếu xem theo năm
            RevenueByAirlines = revenueByAirline
        };

        return View(viewModel);
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