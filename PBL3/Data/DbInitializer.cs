using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PBL3.Models;

namespace PBL3.Data
{
    public static class DbInitializer
    {
        public static void Initialize(IServiceProvider serviceProvider)
{
    using (var context = new ApplicationDbContext(
        serviceProvider.GetRequiredService<DbContextOptions<ApplicationDbContext>>()))
    {
        if(context.Database.EnsureCreated())
                {
                    // Cơ sở dữ liệu đã được tạo, không cần khởi tạo lại
                    return;
                }   
         // Xóa tất cả chuyến bay hiện có nếu cần tạo lại
        if (context.Flights.Any())
            {
            context.Flights.RemoveRange(context.Flights);
            context.SaveChanges();
            }

        var airports = context.Airports.ToDictionary(a => a.Code, a => a.Id);

        // Tạo danh sách chuyến bay mẫu
        var flights = new List<Flight>();
        var today = DateTime.Today;

        // Định nghĩa các cặp sân bay phổ biến và thông tin chuyến bay
        var popularRoutes = new List<(string From, string To, int Distance, int Duration, int BasePrice)>
        {
            ("HAN", "SGN", 1200, 120, 1200000),  // Hà Nội - TP.HCM: 2 giờ, 1.2tr
            ("SGN", "HAN", 1200, 120, 1200000),  // TP.HCM - Hà Nội: 2 giờ, 1.2tr
            ("HAN", "DAD", 600, 75, 800000),     // Hà Nội - Đà Nẵng: 1h15p, 800k
            ("DAD", "HAN", 600, 75, 800000),     // Đà Nẵng - Hà Nội: 1h15p, 800k
            ("SGN", "DAD", 800, 90, 900000),     // TP.HCM - Đà Nẵng: 1h30p, 900k
            ("DAD", "SGN", 800, 90, 900000),     // Đà Nẵng - TP.HCM: 1h30p, 900k
            ("SGN", "PQC", 400, 60, 700000),     // TP.HCM - Phú Quốc: 1h, 700k
            ("PQC", "SGN", 400, 60, 700000),     // Phú Quốc - TP.HCM: 1h, 700k
            ("HAN", "CXR", 1050, 105, 1000000),  // Hà Nội - Nha Trang: 1h45p, 1tr
            ("CXR", "HAN", 1050, 105, 1000000)   // Nha Trang - Hà Nội: 1h45p, 1tr
        };

        // Tạo chuyến bay cho 30 ngày
        for (int day = 0; day < 30; day++)
        {
            var currentDate = today.AddDays(day);
            bool isHoliday = (currentDate.Month == 4 && currentDate.Day == 30) || 
                             (currentDate.Month == 5 && currentDate.Day == 1);

            foreach (var route in popularRoutes)
            {
                AddFlights(
                    flights, 
                    airports[route.From], 
                    airports[route.To], 
                    currentDate, 
                    route.Distance, 
                    route.Duration, 
                    route.BasePrice,
                    isHoliday
                );
            }
        }

        context.Flights.AddRange(flights);
        context.SaveChanges();
    }
}

private static void AddFlights(
    List<Flight> flights, 
    int departureAirportId, 
    int arrivalAirportId, 
    DateTime date, 
    int distance, 
    int durationMinutes, 
    int basePrice,
    bool isHoliday)
{
    // Tạo 2 chuyến bay mỗi ngày: sáng và chiều
    string[] airlines = { "VN", "VJ", "BL", "QH" };
    var random = new Random();

    // Chuyến bay buổi sáng (8:00 - 10:00)
    var morningDepartureHour = random.Next(8, 10);
    var morningDepartureMinute = random.Next(0, 4) * 15; // 0, 15, 30, 45
    var morningDepartureTime = new DateTime(date.Year, date.Month, date.Day, morningDepartureHour, morningDepartureMinute, 0);
    var morningArrivalTime = morningDepartureTime.AddMinutes(durationMinutes);

    // Chuyến bay buổi chiều (14:00 - 17:00)
    var afternoonDepartureHour = random.Next(14, 17);
    var afternoonDepartureMinute = random.Next(0, 4) * 15; // 0, 15, 30, 45
    var afternoonDepartureTime = new DateTime(date.Year, date.Month, date.Day, afternoonDepartureHour, afternoonDepartureMinute, 0);
    var afternoonArrivalTime = afternoonDepartureTime.AddMinutes(durationMinutes);

    // Tạo mã chuyến bay
    string morningAirline = airlines[random.Next(airlines.Length)];
    string afternoonAirline = airlines[random.Next(airlines.Length)];
    string morningFlightNumber = $"{morningAirline}{random.Next(100, 1000)}";
    string afternoonFlightNumber = $"{afternoonAirline}{random.Next(100, 1000)}";

    // Điều chỉnh giá vé
    // Thêm biến động nhỏ cho giá vé (±5%)
    int morningPriceVariation = random.Next(-5, 6);
    int afternoonPriceVariation = random.Next(-5, 6);
    
    int morningPrice = basePrice + (basePrice * morningPriceVariation / 100);
    int afternoonPrice = basePrice + (basePrice * afternoonPriceVariation / 100);

    // Thêm chuyến bay buổi sáng
    flights.Add(new Flight
    {
        FlightNumber = morningFlightNumber,
        StartingDestination = departureAirportId,
        ReachingDestination = arrivalAirportId,
        StartingTime = morningDepartureTime,
        ReachingTime = morningArrivalTime,
        Capacity = random.Next(150, 250),
        Distance = distance,
        BasePrice = morningPrice
    });

    // Thêm chuyến bay buổi chiều
    flights.Add(new Flight
    {
        FlightNumber = afternoonFlightNumber,
        StartingDestination = departureAirportId,
        ReachingDestination = arrivalAirportId,
        StartingTime = afternoonDepartureTime,
        ReachingTime = afternoonArrivalTime,
        Capacity = random.Next(150, 250),
        Distance = distance,
        BasePrice = afternoonPrice
    });
}
    }
}