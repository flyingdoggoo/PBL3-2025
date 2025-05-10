namespace PBL3.Models.ViewModels
{
    public class SeatViewModel
    {
        public int SeatId { get; set; } // Nếu dùng model Seat
        public string SeatNumber { get; set; } // Ví dụ: "1A"
        public int Row { get; set; }
        public string Column { get; set; }
        public string Status { get; set; } // "available", "booked", "unavailable"
        public string SectionName { get; set; } // "Thương gia", "Phổ thông"
        public decimal CalculatedPrice { get; set; } // Giá đã tính cho ghế này
        public bool IsEmergencyExit { get; set; } // Ví dụ
        public bool IsNearToilet { get; set; }   // Ví dụ
    }
}