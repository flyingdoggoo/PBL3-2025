using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PBL3.Models;

namespace PBL3.Data
{
    public static class DbInitializer
    {
        // Biến random nên được tạo một lần để tránh lặp giá trị khi gọi nhanh
        private static readonly Random random = new Random();

        public static void Initialize(IServiceProvider serviceProvider)
        {
            var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
            var logger = loggerFactory.CreateLogger("DbInitializer");
            logger.LogInformation("Attempting to initialize database data...");

            using (var context = new ApplicationDbContext(
                serviceProvider.GetRequiredService<DbContextOptions<ApplicationDbContext>>()))
            {
                // --- 1. Seed Airports (Nếu chưa có) ---
                if (!context.Airports.Any())
                {
                    logger.LogInformation("Seeding sample airports...");
                    var sampleAirports = new Airport[]
                    {
                        new Airport{ Code="HAN", Name="Sân bay Quốc tế Nội Bài", City="Hà Nội", Country="Việt Nam"},
                        new Airport{ Code="SGN", Name="Sân bay Quốc tế Tân Sơn Nhất", City="TP. Hồ Chí Minh", Country="Việt Nam"},
                        new Airport{ Code="DAD", Name="Sân bay Quốc tế Đà Nẵng", City="Đà Nẵng", Country="Việt Nam"},
                        new Airport{ Code="PQC", Name="Sân bay Quốc tế Phú Quốc", City="Phú Quốc", Country="Việt Nam"},
                        new Airport{ Code="CXR", Name="Sân bay Quốc tế Cam Ranh", City="Nha Trang", Country="Việt Nam"},
                        new Airport{ Code="HPH", Name="Sân bay Quốc tế Cát Bi", City="Hải Phòng", Country="Việt Nam"},
                        new Airport{ Code="VCA", Name="Sân bay Quốc tế Cần Thơ", City="Cần Thơ", Country="Việt Nam"},
                        new Airport{ Code="HUI", Name="Sân bay Quốc tế Phú Bài", City="Huế", Country="Việt Nam"},
                        new Airport{ Code="DLI", Name="Sân bay Liên Khương", City="Đà Lạt", Country="Việt Nam"},
                        new Airport{ Code="VCS", Name="Sân bay Côn Đảo", City="Côn Đảo", Country="Việt Nam"}
                        // Thêm các sân bay khác nếu bạn muốn
                    };
                    context.Airports.AddRange(sampleAirports);
                    try
                    {
                        context.SaveChanges();
                        logger.LogInformation("Finished seeding airports.");
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Error saving airports during seeding.");
                        return; // Không tiếp tục nếu seed airport lỗi
                    }
                }
                else
                {
                    logger.LogInformation("Airports already exist. Skipping airport seeding.");
                }

                // --- 2. Seed Flights (Xóa cũ và thêm mới để demo) ---
                logger.LogInformation("Attempting to seed flights...");

                // Lấy Dictionary SÂN BAY SAU KHI ĐÃ SEED/KIỂM TRA
                var airports = context.Airports.AsEnumerable().ToDictionary(a => a.Code, a => a.Id);

                if (!airports.Any())
                {
                    logger.LogError("Cannot seed flights: No airports found in the database.");
                    return;
                }

                // Chỉ seed flights nếu bảng Flights đang trống (tránh tạo lại nhiều lần)
                if (!context.Flights.Any())
                {
                    logger.LogInformation("Flights table is empty. Generating new flight data...");
                    var flights = new List<Flight>();
                    var today = DateTime.Today;

                    // Định nghĩa các cặp sân bay, khoảng cách, thời gian bay (phút), giá cơ bản
                    var popularRoutes = new List<(string From, string To, int Distance, int Duration, decimal Price)>
                    {
                        ("HAN", "SGN", 1200, 125, 1500000m), ("SGN", "HAN", 1200, 125, 1450000m),
                        ("HAN", "DAD", 630, 80, 900000m),   ("DAD", "HAN", 630, 80, 850000m),
                        ("SGN", "DAD", 610, 85, 950000m),   ("DAD", "SGN", 610, 85, 920000m),
                        ("SGN", "PQC", 300, 60, 750000m),   ("PQC", "SGN", 300, 60, 700000m),
                        ("HAN", "PQC", 1300, 130, 1600000m),("PQC", "HAN", 1300, 130, 1550000m),
                        ("HAN", "CXR", 1150, 110, 1100000m),("CXR", "HAN", 1150, 110, 1050000m),
                        ("SGN", "CXR", 300, 60, 650000m),   ("CXR", "SGN", 300, 60, 600000m),
                        ("SGN", "HPH", 1100, 120, 1300000m),("HPH", "SGN", 1100, 120, 1250000m),
                        ("DAD", "HPH", 530, 75, 800000m),   ("HPH", "DAD", 530, 75, 780000m),
                        ("HAN", "HUI", 580, 70, 700000m),   ("HUI", "HAN", 580, 70, 680000m),
                        ("SGN", "DLI", 240, 50, 600000m),   ("DLI", "SGN", 240, 50, 580000m),
                        // Thêm các tuyến khác nếu muốn
                    };

                    // Tạo chuyến bay cho 14 ngày tới
                    int numberOfDaysToSeed = 14;
                    logger.LogInformation($"Generating flights for the next {numberOfDaysToSeed} days...");
                    for (int day = 0; day < numberOfDaysToSeed; day++)
                    {
                        var currentDate = today.AddDays(day);
                        foreach (var route in popularRoutes)
                        {
                            if (airports.ContainsKey(route.From) && airports.ContainsKey(route.To))
                            {
                                // Tạo nhiều chuyến bay hơn mỗi ngày cho các tuyến phổ biến
                                int flightsPerDay = (route.From == "HAN" && route.To == "SGN") || (route.From == "SGN" && route.To == "HAN") ? 4 : 2;
                                AddFlightsForRoute(flights, airports[route.From], airports[route.To], currentDate, route.Distance, route.Duration, route.Price, flightsPerDay);
                            }
                            else
                            {
                                logger.LogWarning($"Skipping flight seed: Airport code '{route.From}' or '{route.To}' not found.");
                            }
                        }
                    }

                    if (flights.Any())
                    {
                        context.Flights.AddRange(flights);
                        try
                        {
                            context.SaveChanges();
                            logger.LogInformation($"Finished seeding {flights.Count} flights.");
                        }
                        catch (Exception ex)
                        {
                            logger.LogError(ex, "Error saving flights during seeding.");
                        }

                    }
                    else
                    {
                        logger.LogWarning("No flights were generated to seed.");
                    }
                }
                else
                {
                    logger.LogInformation("Flights table is not empty. Skipping flight seeding.");
                }

                logger.LogInformation("DbInitializer finished.");
            }
        }

        // Hàm tạo chuyến bay cho một tuyến đường cụ thể trong một ngày
        private static void AddFlightsForRoute(List<Flight> flights, int departureAirportId, int arrivalAirportId, DateTime date, int distance, int durationMinutes, decimal basePrice, int numberOfFlights)
        {
            string[] airlines = { "Vietnam Airlines", "Vietjet Air", "Bamboo Airways", "Pacific Airlines", "Vietravel Airlines" };

            for (int i = 0; i < numberOfFlights; i++)
            {
                // Phân bổ giờ bay ngẫu nhiên hơn trong ngày
                int hour;
                if (i < numberOfFlights / 2) // Buổi sáng/trưa
                {
                    hour = random.Next(7, 13); // 7h - 12h
                }
                else // Buổi chiều/tối
                {
                    hour = random.Next(13, 21); // 13h - 20h
                }
                int minute = random.Next(0, 4) * 15; // 0, 15, 30, 45
                var departureTime = new DateTime(date.Year, date.Month, date.Day, hour, minute, 0);

                // Đảm bảo không tạo chuyến bay trong quá khứ (nếu date là hôm nay)
                if (departureTime < DateTime.Now && date.Date == DateTime.Today) continue;

                var arrivalTime = departureTime.AddMinutes(durationMinutes);
                var airline = airlines[random.Next(airlines.Length)];
                var flightNumber = $"{airline.Substring(0, 2).ToUpper().Replace(" ", "")}{random.Next(101, 999)}"; // Mã ngắn gọn hơn
                var capacity = random.Next(120, 221); // Sức chứa ngẫu nhiên

                // Giá vé biến động theo giờ và ngày trong tuần
                decimal timeMultiplier = (hour < 9 || hour > 17) ? 0.95m : 1.1m; // Giờ thấp điểm rẻ hơn, giờ cao điểm đắt hơn
                decimal dayMultiplier = (date.DayOfWeek == DayOfWeek.Friday || date.DayOfWeek == DayOfWeek.Saturday || date.DayOfWeek == DayOfWeek.Sunday) ? 1.2m : 1.0m; // Cuối tuần đắt hơn
                decimal randomMultiplier = 1 + (decimal)(random.Next(-8, 13)) / 100; // Biến động ngẫu nhiên -8% đến +12%

                decimal finalPrice = basePrice * timeMultiplier * dayMultiplier * randomMultiplier;
                finalPrice = Math.Round(finalPrice / 10000) * 10000; // Làm tròn đến 10 nghìn

                flights.Add(new Flight
                {
                    FlightNumber = flightNumber,
                    StartingDestination = departureAirportId,
                    ReachingDestination = arrivalAirportId,
                    StartingTime = departureTime,
                    ReachingTime = arrivalTime,
                    Capacity = capacity,
                    Price = finalPrice, // Giá vé đã tính
                    Airline = airline,
                    AvailableSeats = capacity, // Ban đầu số ghế trống bằng sức chứa
                    Distance = distance
                });
            }
        }
    }
}