using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PBL3.Data;
using PBL3.Models.ViewModels;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Globalization;
using System.Collections.Generic;
using PBL3.Models;

[Authorize(Roles = "Admin,Employee")]
public class StatisticsController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<StatisticsController> _logger;
    public StatisticsController(ApplicationDbContext context, ILogger<StatisticsController> logger)
    {
        _context = context;
        _logger = logger;
    }
    public async Task<IActionResult> Revenue(int? year, int? month)
    {
        _logger.LogInformation("Revenue statistics requested for Year: {Year}, Month: {Month}", year, month);

        int currentYear = year ?? DateTime.Now.Year;
        ViewData["SelectedYear"] = currentYear;
        ViewData["SelectedMonth"] = month;

        try
        {
            ViewData["Years"] = await _context.Tickets
                                                .Select(t => t.OrderTime.Year)
                                                .Distinct()
                                                .OrderByDescending(y => y)
                                                .ToListAsync();
            ViewData["Months"] = Enumerable.Range(1, 12)
                                          .Select(m => new { Value = m, Name = CultureInfo.GetCultureInfo("vi-VN").DateTimeFormat.GetMonthName(m) })
                                          .ToList();
            var ticketsQuery = _context.Tickets
                                    .Include(t => t.Flight)
                                    .Where(t => t.Status != TicketStatus.Cancelled);
            ticketsQuery = ticketsQuery.Where(t => t.OrderTime.Year == currentYear);

            if (month.HasValue && month.Value >= 1 && month.Value <= 12)
            {
                ticketsQuery = ticketsQuery.Where(t => t.OrderTime.Month == month.Value);
                _logger.LogInformation("Filtering by Month: {SelectedMonth}", month.Value);
            }
            _logger.LogInformation("Executing query to get valid tickets...");
            var validTickets = await ticketsQuery.ToListAsync();
            _logger.LogInformation("Found {TicketCount} valid tickets.", validTickets.Count);
            decimal totalRevenue = validTickets.Sum(t => t.Price);
            int totalTicketsSold = validTickets.Count();
            decimal averageTicketPrice = totalTicketsSold > 0 ? totalRevenue / totalTicketsSold : 0;
            _logger.LogInformation("Calculated Totals - Revenue: {TotalRevenue}, Tickets: {TotalTicketsSold}, Avg Price: {AverageTicketPrice}", totalRevenue, totalTicketsSold, averageTicketPrice);
            List<MonthlyRevenue> monthlyRevenueData = new List<MonthlyRevenue>();
            List<DailyRevenue> dailyRevenueData = new List<DailyRevenue>();

            if (!month.HasValue)
            {
                _logger.LogInformation("Calculating monthly revenue...");
                monthlyRevenueData = validTickets
                    .GroupBy(t => t.OrderTime.Month)
                    .Select(g => new MonthlyRevenue
                    {
                        Month = g.Key,
                        MonthName = CultureInfo.GetCultureInfo("vi-VN").DateTimeFormat.GetMonthName(g.Key),
                        Revenue = g.Sum(t => t.Price),
                        TicketCount = g.Count()
                    })
                    .OrderBy(m => m.Month)
                    .ToList();
                _logger.LogInformation("Calculated {Count} months of revenue data.", monthlyRevenueData.Count);
            }
            else
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
            _logger.LogInformation("Calculating revenue by airline...");
            var revenueByAirline = validTickets
               .Where(t => t.Flight != null && t.Flight.Airline != null)
               .GroupBy(t => t.Flight.Airline)
               .Select(g => new RevenueByAirline
               {
                   Airline = g.Key,
                   Revenue = g.Sum(t => t.Price),
                   TicketCount = g.Count()
               })
               .OrderByDescending(a => a.Revenue)
               .ToList();
            _logger.LogInformation("Calculated revenue for {Count} airlines.", revenueByAirline.Count);
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
            return View(viewModel);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while generating revenue statistics for Year: {Year}, Month: {Month}", currentYear, month);
            TempData["ErrorMessage"] = "Đã xảy ra lỗi khi tải dữ liệu thống kê. Vui lòng thử lại sau.";
            return RedirectToAction("Index", "SystemManager");
        }
    }

}
namespace PBL3.Models.ViewModels
{
    public class RevenueViewModel
    {
        public int SelectedYear { get; set; }
        public int? SelectedMonth { get; set; }
        public decimal TotalRevenue { get; set; }
        public int TotalTicketsSold { get; set; }
        public decimal AverageTicketPrice { get; set; }
        public List<MonthlyRevenue> MonthlyRevenues { get; set; } = new List<MonthlyRevenue>();
        public List<DailyRevenue> DailyRevenues { get; set; } = new List<DailyRevenue>();
        public List<RevenueByAirline> RevenueByAirlines { get; set; } = new List<RevenueByAirline>();
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