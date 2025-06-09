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