namespace PBL3.Models.ViewModels
{
    public class SeatViewModel
    {
        public int SeatId { get; set; }
        public string SeatNumber { get; set; }
        public int Row { get; set; }
        public string Column { get; set; }
        public string Status { get; set; }
        public string SectionName { get; set; }
        public decimal CalculatedPrice { get; set; }
    }
}
