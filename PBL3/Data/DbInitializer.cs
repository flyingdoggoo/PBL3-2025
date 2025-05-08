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
        private static readonly Random random = new Random(Guid.NewGuid().GetHashCode());

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
                    var sampleAirports = new Airport[] { /* ... danh sách sân bay giữ nguyên ... */
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

                // --- 2. Seed Flights and Seats (Chỉ seed nếu bảng Flights trống) ---
                if (!context.Flights.Any())
                {
                    logger.LogInformation("Flights table is empty. Generating new flight data...");
                    var flightsToSeed = new List<Flight>();
                    var seatsToSeed = new List<Seat>();
                    var today = DateTime.Today;

                    // *** GIẢM SỐ LƯỢNG TUYẾN ĐƯỜNG HOẶC SỐ NGÀY ***
                    var routesToSeed = new List<(string From, string To, int Distance, int Duration, decimal Price)>
                    {
                        ("HAN", "SGN", 1200, 125, 1500000m), ("SGN", "HAN", 1200, 125, 1450000m),
                        ("HAN", "DAD", 630, 80, 900000m),   ("DAD", "HAN", 630, 80, 850000m),
                        ("SGN", "DAD", 610, 85, 950000m),   ("DAD", "SGN", 610, 85, 920000m),
                        ("SGN", "PQC", 300, 60, 750000m),   ("PQC", "SGN", 300, 60, 700000m),
                        ("HAN", "HPH", 100, 30, 500000m),   ("HPH", "HAN", 100, 30, 480000m), // Thêm tuyến ngắn
                        ("DAD", "CXR", 250, 50, 600000m),   ("CXR", "DAD", 250, 50, 590000m) // Thêm tuyến miền Trung
                    };

                    int numberOfDaysToSeed = 3; // *** GIẢM SỐ NGÀY SEED *** (Ví dụ: 3 ngày tới)
                    logger.LogInformation($"Generating flights for the next {numberOfDaysToSeed} days...");

                    for (int day = 0; day < numberOfDaysToSeed; day++)
                    {
                        var currentDate = today.AddDays(day);
                        foreach (var route in routesToSeed)
                        {
                            if (airports.ContainsKey(route.From) && airports.ContainsKey(route.To))
                            {
                                // *** GIẢM SỐ CHUYẾN/NGÀY/TUYẾN ***
                                int flightsPerDay = 1; // Chỉ tạo 1 chuyến/ngày/tuyến để giảm số lượng
                                AddFlightsToList(flightsToSeed, airports[route.From], airports[route.To], currentDate, route.Distance, route.Duration, route.Price, flightsPerDay);
                            }
                            else { logger.LogWarning($"Skipping flight seed: Airport code '{route.From}' or '{route.To}' not found."); }
                        }
                    }

                    // --- LƯU CHUYẾN BAY VÀ TẠO GHẾ (Giữ nguyên logic) ---
                    if (flightsToSeed.Any())
                    {
                        context.Flights.AddRange(flightsToSeed);
                        try
                        {
                            context.SaveChanges(); // Lưu Flights để lấy ID
                            logger.LogInformation($"Finished seeding {flightsToSeed.Count} flights.");

                            logger.LogInformation("Generating seats for newly seeded flights...");
                            // Tạo ghế cho các chuyến bay vừa lưu
                            foreach (var seededFlight in flightsToSeed)
                            {
                                GenerateSeatsForFlight(seatsToSeed, seededFlight);
                            }

                            if (seatsToSeed.Any())
                            {
                                context.Seats.AddRange(seatsToSeed);
                                context.SaveChanges(); // Lưu Seats
                                logger.LogInformation($"Finished generating and saving {seatsToSeed.Count} seats.");
                            }
                            else { logger.LogWarning("No seats were generated."); }
                        }
                        catch (Exception ex) { logger.LogError(ex, "Error saving flights or generating/saving seats."); }
                    }
                    else { logger.LogWarning("No flights were generated to seed."); }
                }
                else { logger.LogInformation("Flights table is not empty. Skipping flight and seat seeding."); }

                logger.LogInformation("DbInitializer data check/seeding finished.");
            }
        }

        // Hàm AddFlightsToList (Giữ nguyên logic tạo flight)
        private static void AddFlightsToList(List<Flight> flights, int departureAirportId, int arrivalAirportId, DateTime date, int distance, int durationMinutes, decimal basePrice, int numberOfFlights)
        {
            string[] airlines = { "Vietnam Airlines", "Vietjet Air", "Bamboo Airways" }; // Giảm số hãng bay

            for (int i = 0; i < numberOfFlights; i++)
            {
                // Chọn giờ cố định hơn cho ít chuyến bay
                TimeSpan departureTimeOfDay = (i % 2 == 0) ? new TimeSpan(9, 30, 0) : new TimeSpan(15, 0, 0);
                // Hoặc giữ random nếu muốn đa dạng giờ
                // int hour = (i < numberOfFlights / 2) ? random.Next(7, 13) : random.Next(13, 21);
                // int minute = random.Next(0, 4) * 15;
                // var departureTimeOfDay = new TimeSpan(hour, minute, 0);


                var departureTime = date.Add(departureTimeOfDay);
                if (departureTime < DateTime.Now && date.Date == DateTime.Today) continue;

                var arrivalTime = departureTime.AddMinutes(durationMinutes);
                var airline = airlines[random.Next(airlines.Length)];
                var flightNumber = $"{airline.Substring(0, 2).ToUpper().Replace(" ", "")}{random.Next(101, 999)}";
                var capacity = random.Next(100, 181); // Giảm capacity một chút

                // Giá vé biến động ít hơn
                decimal timeMultiplier = (departureTime.Hour < 9 || departureTime.Hour > 17) ? 0.98m : 1.05m;
                decimal dayMultiplier = (date.DayOfWeek == DayOfWeek.Saturday || date.DayOfWeek == DayOfWeek.Sunday) ? 1.1m : 1.0m;
                decimal randomMultiplier = 1 + (decimal)(random.Next(-5, 6)) / 100; // -5% đến +5%
                decimal finalPrice = basePrice * timeMultiplier * dayMultiplier * randomMultiplier;
                finalPrice = Math.Round(finalPrice / 10000) * 10000;

                flights.Add(new Flight
                {
                    FlightNumber = flightNumber,
                    StartingDestination = departureAirportId,
                    ReachingDestination = arrivalAirportId,
                    StartingTime = departureTime,
                    ReachingTime = arrivalTime,
                    Capacity = capacity,
                    Price = finalPrice,
                    Airline = airline,
                    AvailableSeats = capacity,
                    Distance = distance
                });
            }
        }

        // Hàm GenerateSeatsForFlight (Giữ nguyên)
        private static void GenerateSeatsForFlight(List<Seat> seats, Flight flight)
        {
            // ... (Code tạo seat như cũ) ...
            if (flight == null || flight.FlightId <= 0 || flight.Capacity <= 0) return;
            int seatsGenerated = 0;
            int rows = (int)Math.Ceiling((double)flight.Capacity / 6);
            string[] seatLetters = { "A", "B", "C", "D", "E", "F" };
            for (int row = 1; row <= rows && seatsGenerated < flight.Capacity; row++)
            {
                foreach (string letter in seatLetters)
                {
                    if (seatsGenerated >= flight.Capacity) break;
                    string seatNumber = $"{row}{letter}";
                    string seatType = (letter == "A" || letter == "F") ? "Window" : (letter == "C" || letter == "D" ? "Aisle" : "Standard");
                    seats.Add(new Seat
                    {
                        SeatNumber = seatNumber,
                        FlightId = flight.FlightId,
                        Status = "Available",
                        SeatType = seatType
                    });
                    seatsGenerated++;
                }
            }
        }
    }
}