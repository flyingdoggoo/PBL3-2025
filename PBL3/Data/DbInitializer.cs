using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PBL3.Models;
using PBL3.Utils;

namespace PBL3.Data
{
    public static class DbInitializer
    {
        private static readonly Random random = new Random(Guid.NewGuid().GetHashCode());

        public static void Initialize(IServiceProvider serviceProvider)
        {
            var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
            var logger = loggerFactory.CreateLogger("DbInitializer");
            logger.LogInformation("Attempting to initialize database data...");

            using (var context = new ApplicationDbContext(
                serviceProvider.GetRequiredService<DbContextOptions<ApplicationDbContext>>()))
            {
                // --- 1. Seed Airports ---
                if (!context.Airports.Any())
                {
                    logger.LogInformation("Seeding sample airports...");
                    var sampleAirports = new Airport[] {
                        new Airport{ Code="HAN", Name="Sân bay Quốc tế Nội Bài", City="Hà Nội", Country="Việt Nam"},
                        new Airport{ Code="SGN", Name="Sân bay Quốc tế Tân Sơn Nhất", City="TP. Hồ Chí Minh", Country="Việt Nam"},
                        new Airport{ Code="DAD", Name="Sân bay Quốc tế Đà Nẵng", City="Đà Nẵng", Country="Việt Nam"},
                        new Airport{ Code="PQC", Name="Sân bay Quốc tế Phú Quốc", City="Phú Quốc", Country="Việt Nam"},
                        new Airport{ Code="CXR", Name="Sân bay Quốc tế Cam Ranh", City="Nha Trang", Country="Việt Nam"},
                        new Airport{ Code="HPH", Name="Sân bay Quốc tế Cát Bi", City="Hải Phòng", Country="Việt Nam"},
                    };
                    context.Airports.AddRange(sampleAirports);
                    try { context.SaveChanges(); logger.LogInformation("Finished seeding airports."); }
                    catch (Exception ex) { logger.LogError(ex, "Error saving airports during seeding."); return; }
                }
                else { logger.LogInformation("Airports already exist."); }

                var airports = context.Airports.AsEnumerable().ToDictionary(a => a.Code, a => a.Id);
                if (!airports.Any()) { logger.LogError("Cannot seed flights: No airports found."); return; }

                // --- 2. Seed Flights, Sections, and Seats (Chỉ seed nếu bảng Flights trống) ---
                if (!context.Flights.Any())
                {
                    logger.LogInformation("Flights table is empty. Generating new flight, section, and seat data...");
                    var flightsToSeed = new List<Flight>();
                    // Không cần list seatsToSeed ở đây nữa vì sẽ tạo trực tiếp khi có FlightId

                    var today = DateTime.Today;
                    var routesToSeed = new List<(string From, string To, int Distance, int Duration, decimal Price)>
                    {
                        ("HAN", "SGN", 1200, 125, 1500000m), ("SGN", "HAN", 1200, 125, 1450000m),
                        ("HAN", "DAD", 630, 80, 900000m),   ("DAD", "HAN", 630, 80, 850000m),
                        // Thêm các tuyến khác nếu muốn giảm tải
                    };

                    int numberOfDaysToSeed = 3; // Giảm số ngày để test nhanh
                    logger.LogInformation($"Generating flights for the next {numberOfDaysToSeed} days...");

                    for (int day = 0; day < numberOfDaysToSeed; day++)
                    {
                        var currentDate = today.AddDays(day);
                        foreach (var route in routesToSeed)
                        {
                            if (airports.ContainsKey(route.From) && airports.ContainsKey(route.To))
                            {
                                int flightsPerDay = 1; // Chỉ 1 chuyến/ngày/tuyến để giảm
                                AddFlightsToList(flightsToSeed, airports[route.From], airports[route.To], currentDate, route.Distance, route.Duration, route.Price, flightsPerDay);
                            }
                            else { logger.LogWarning($"Skipping flight seed: Airport code '{route.From}' or '{route.To}' not found."); }
                        }
                    }

                    if (flightsToSeed.Any())
                    {
                        context.Flights.AddRange(flightsToSeed);
                        try
                        {
                            context.SaveChanges(); // **LƯU CHUYẾN BAY ĐỂ LẤY FLIGHT ID**
                            logger.LogInformation($"Finished seeding {flightsToSeed.Count} flights.");

                            logger.LogInformation("Generating sections and seats for newly seeded flights...");
                            var allNewSeats = new List<Seat>();
                            foreach (var seededFlight in flightsToSeed) // Lặp qua các chuyến bay vừa lưu
                            {
                                // --- TẠO SECTIONS CHO FLIGHT NÀY ---
                                int businessCapacity = (int)Math.Floor(seededFlight.Capacity * 0.30); // 30% thương gia
                                int economyCapacity = seededFlight.Capacity - businessCapacity;

                                var sectionsForThisFlight = new List<Section>();
                                if (businessCapacity > 0)
                                {
                                    sectionsForThisFlight.Add(new Section
                                    {
                                        FlightId = seededFlight.FlightId, // Gán FlightId
                                        SectionName = "Thương gia",
                                        Capacity = businessCapacity,
                                        PriceMultiplier = 1.8m // Ví dụ: Giá gấp 1.8 lần
                                    });
                                }
                                if (economyCapacity > 0)
                                {
                                    sectionsForThisFlight.Add(new Section
                                    {
                                        FlightId = seededFlight.FlightId, // Gán FlightId
                                        SectionName = "Phổ thông",
                                        Capacity = economyCapacity,
                                        PriceMultiplier = 1.0m // Giá cơ bản
                                    });
                                }

                                if (sectionsForThisFlight.Any())
                                {
                                    context.Sections.AddRange(sectionsForThisFlight);
                                    context.SaveChanges(); // **LƯU SECTIONS ĐỂ LẤY SECTION ID**

                                    // --- TẠO GHẾ CHO TỪNG SECTION VỪA LƯU ---
                                    int startingRow = 1; // Bắt đầu từ hàng 1 cho mỗi section
                                    foreach (var newSection in sectionsForThisFlight)
                                    {
                                        startingRow = SeatGenerator.GenerateSeatsForSection(allNewSeats, newSection, startingRow);
                                    }
                                }
                            } // Kết thúc vòng lặp seededFlight

                            if (allNewSeats.Any())
                            {
                                context.Seats.AddRange(allNewSeats);
                                context.SaveChanges(); // Lưu tất cả ghế
                                logger.LogInformation($"Finished generating and saving {allNewSeats.Count} seats for all flights.");
                            }
                            else { logger.LogWarning("No seats were generated for any flight."); }

                        }
                        catch (Exception ex) { logger.LogError(ex, "Error saving flights, sections, or seats."); }
                    }
                    else { logger.LogWarning("No flights were generated to seed."); }
                }
                else { logger.LogInformation("Flights table is not empty. Skipping flight, section, and seat seeding."); }

                logger.LogInformation("DbInitializer data check/seeding finished.");
            }
        }

        // Hàm AddFlightsToList (Giữ nguyên logic tạo đối tượng Flight)
        private static void AddFlightsToList(List<Flight> flights, int departureAirportId, int arrivalAirportId, DateTime date, int distance, int durationMinutes, decimal basePrice, int numberOfFlights)
        {
            string[] airlines = { "Vietnam Airlines", "Vietjet Air", "Bamboo Airways" };

            for (int i = 0; i < numberOfFlights; i++)
            {
                TimeSpan departureTimeOfDay = (i % 2 == 0) ? new TimeSpan(9, 30, 0) : new TimeSpan(15, 0, 0);
                var departureTime = date.Add(departureTimeOfDay);
                if (departureTime < DateTime.Now && date.Date == DateTime.Today) continue;

                var arrivalTime = departureTime.AddMinutes(durationMinutes);
                var airline = airlines[random.Next(airlines.Length)];
                var flightNumber = $"{airline.Substring(0, 2).ToUpper().Replace(" ", "")}{random.Next(101, 999)}";
                var capacity = random.Next(100, 181);

                decimal timeMultiplier = (departureTime.Hour < 9 || departureTime.Hour > 17) ? 0.98m : 1.05m;
                decimal dayMultiplier = (date.DayOfWeek == DayOfWeek.Saturday || date.DayOfWeek == DayOfWeek.Sunday) ? 1.1m : 1.0m;
                decimal randomMultiplier = 1 + (decimal)(random.Next(-5, 6)) / 100;
                decimal finalPrice = basePrice * timeMultiplier * dayMultiplier * randomMultiplier;
                finalPrice = Math.Round(finalPrice / 10000) * 10000;

                flights.Add(new Flight
                {
                    FlightNumber = flightNumber,
                    StartingDestination = departureAirportId,
                    ReachingDestination = arrivalAirportId,
                    StartingTime = departureTime,
                    ReachingTime = arrivalTime,
                    Capacity = capacity, // Tổng capacity của chuyến bay
                    Price = finalPrice,  // Giá cơ sở (cho hạng Phổ thông)
                    Airline = airline,
                    AvailableSeats = capacity, // Sẽ được cập nhật dựa trên vé bán
                    Distance = distance
                });
            }
        }
        
    }
}