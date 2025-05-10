using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PBL3.Models;
using PBL3.Utils; // Namespace của SeatGenerator

namespace PBL3.Data
{
    public static class DbInitializer
    {
        private static readonly Random random = new Random(Guid.NewGuid().GetHashCode()); // Khởi tạo Random một lần
        private static readonly string[] Airlines = { "Vietnam Airlines", "Vietjet Air", "Bamboo Airways", "Pacific Airlines", "Vietravel Airlines" };

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
                    logger.LogInformation("No airports found. Seeding sample airports...");
                    var sampleAirports = new Airport[] {
                        new Airport{ Code="HAN", Name="Sân bay Quốc tế Nội Bài", City="Hà Nội", Country="Việt Nam"},
                        new Airport{ Code="SGN", Name="Sân bay Quốc tế Tân Sơn Nhất", City="TP. Hồ Chí Minh", Country="Việt Nam"},
                        new Airport{ Code="DAD", Name="Sân bay Quốc tế Đà Nẵng", City="Đà Nẵng", Country="Việt Nam"},
                        new Airport{ Code="PQC", Name="Sân bay Quốc tế Phú Quốc", City="Phú Quốc", Country="Việt Nam"},
                        new Airport{ Code="CXR", Name="Sân bay Quốc tế Cam Ranh", City="Nha Trang", Country="Việt Nam"},
                        new Airport{ Code="HPH", Name="Sân bay Quốc tế Cát Bi", City="Hải Phòng", Country="Việt Nam"},
                        new Airport{ Code="VCA", Name="Sân bay Quốc tế Cần Thơ", City="Cần Thơ", Country="Việt Nam"},
                        new Airport{ Code="HUI", Name="Sân bay Quốc tế Phú Bài", City="Huế", Country="Việt Nam"},
                        new Airport{ Code="DLI", Name="Sân bay Liên Khương", City="Đà Lạt", Country="Việt Nam"},
                        new Airport{ Code="VDO", Name="Sân bay Quốc tế Vân Đồn", City="Quảng Ninh", Country="Việt Nam"}
                    };
                    context.Airports.AddRange(sampleAirports);
                    try
                    {
                        context.SaveChanges();
                        logger.LogInformation("Finished seeding 10 airports.");
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Error saving airports during seeding.");
                        return; // Không tiếp tục nếu seed airport lỗi
                    }
                }
                else { logger.LogInformation("Airports already exist. Skipping airport seeding."); }

                // Lấy Dictionary SÂN BAY SAU KHI ĐÃ SEED/KIỂM TRA
                var airportsDictionary = context.Airports.AsEnumerable().ToDictionary(a => a.Code, a => a.Id);
                if (airportsDictionary.Count < 2)
                {
                    logger.LogError("Cannot seed flights: Need at least 2 airports in the database for routes.");
                    return;
                }

                // --- 2. Seed Flights, Sections, and Seats ---
                // Chỉ seed nếu bảng Flights đang trống (tránh tạo lại nhiều lần)
                if (!context.Flights.Any())
                {
                    logger.LogInformation("Flights table is empty. Generating new flight, section, and seat data...");
                    var flightsToSeed = new List<Flight>(); // Danh sách Flight Models để AddRange

                    var today = DateTime.Today;

                    // Tạo tất cả các cặp tuyến bay có thể có
                    var allAirportCodes = airportsDictionary.Keys.ToList();
                    var allPossibleRoutes = new List<(string From, string To, int Distance, int Duration, decimal Price)>();
                    foreach (var fromCode in allAirportCodes)
                    {
                        foreach (var toCode in allAirportCodes)
                        {
                            if (fromCode == toCode) continue; // Bỏ qua bay đến chính nó
                            // Ước tính đơn giản
                            int estimatedDistance = random.Next(300, 1801);
                            int estimatedDuration = 60 + (int)(estimatedDistance / 8.5); // Khoảng 8.5km/phút
                            decimal estimatedPrice = 450000m + (estimatedDistance * 750m);
                            allPossibleRoutes.Add((fromCode, toCode, estimatedDistance, estimatedDuration, estimatedPrice));
                        }
                    }

                    // Chọn ngẫu nhiên một số tuyến để seed (để giới hạn tổng số chuyến bay)
                    int numberOfRoutesToSelect = 8; // Chọn 8 tuyến ngẫu nhiên
                    var routesToSeed = allPossibleRoutes.OrderBy(r => Guid.NewGuid()).Take(numberOfRoutesToSelect).ToList();

                    int numberOfDaysToSeed = 7;    // Seed cho 7 ngày tới
                    int flightsPerRoutePerDay = 2; // 2 chuyến/tuyến/ngày
                    logger.LogInformation($"Generating flights for {routesToSeed.Count} selected routes over {numberOfDaysToSeed} days, with {flightsPerRoutePerDay} flights/route/day.");

                    for (int day = 0; day < numberOfDaysToSeed; day++)
                    {
                        var currentDate = today.AddDays(day);
                        foreach (var route in routesToSeed)
                        {
                            if (airportsDictionary.ContainsKey(route.From) && airportsDictionary.ContainsKey(route.To))
                            {
                                AddFlightsToList(flightsToSeed, airportsDictionary[route.From], airportsDictionary[route.To], currentDate, route.Distance, route.Duration, route.Price, flightsPerRoutePerDay);
                            }
                            else
                            {
                                logger.LogWarning($"Skipping flight seed for route {route.From}-{route.To}: Airport code not found in dictionary.");
                            }
                        }
                    }
                    logger.LogInformation($"Total flights generated before saving: {flightsToSeed.Count}. Expected around: {numberOfRoutesToSelect * numberOfDaysToSeed * flightsPerRoutePerDay}");

                    if (flightsToSeed.Any())
                    {
                        context.Flights.AddRange(flightsToSeed);
                        try
                        {
                            context.SaveChanges(); // **1. LƯU CHUYẾN BAY ĐỂ LẤY FLIGHT ID**
                            logger.LogInformation($"SUCCESS: Finished seeding {flightsToSeed.Count} flights into database.");

                            // Bây giờ flightsToSeed đã có FlightId được gán bởi database
                            logger.LogInformation("Generating sections for newly seeded flights...");
                            var sectionsToSeed = new List<Section>();
                            foreach (var seededFlight in flightsToSeed) // Lặp qua các chuyến bay đã được lưu
                            {
                                CreateSectionsForFlight(sectionsToSeed, seededFlight);
                            }

                            if (sectionsToSeed.Any())
                            {
                                context.Sections.AddRange(sectionsToSeed);
                                context.SaveChanges(); // **2. LƯU SECTIONS ĐỂ LẤY SECTION ID**
                                logger.LogInformation($"SUCCESS: Finished seeding {sectionsToSeed.Count} sections.");

                                // Bây giờ sectionsToSeed đã có SectionId
                                logger.LogInformation("Generating seats for newly seeded sections...");
                                var seatsToSeed = new List<Seat>();
                                foreach (var newSection in sectionsToSeed) // Lặp qua các section đã được lưu
                                {
                                    SeatGenerator.GenerateSeatsForSection(seatsToSeed, newSection, 1); // Bắt đầu từ hàng 1 cho mỗi section
                                }

                                if (seatsToSeed.Any())
                                {
                                    context.Seats.AddRange(seatsToSeed);
                                    context.SaveChanges(); // **3. LƯU TẤT CẢ GHẾ**
                                    logger.LogInformation($"SUCCESS: Finished generating and saving {seatsToSeed.Count} seats.");
                                }
                                else { logger.LogWarning("No seats were generated (perhaps no sections were created or sections had 0 capacity)."); }
                            }
                            else { logger.LogWarning("No sections were generated for any flight."); }
                        }
                        catch (DbUpdateException dbEx)
                        {
                            logger.LogError(dbEx, "DbUpdateException while saving flights, sections, or seats.");
                            foreach (var entry in dbEx.Entries)
                            {
                                logger.LogError($"Entity of type '{entry.Entity.GetType().Name}' in state '{entry.State}' could not be saved.");
                            }
                        }
                        catch (Exception ex)
                        {
                            logger.LogError(ex, "General error saving flights, sections, or seats.");
                        }
                    }
                    else { logger.LogWarning("No flights were generated to seed (check route selection or day/flight per day counts)."); }
                }
                else { logger.LogInformation("Flights table is not empty. Skipping flight, section, and seat seeding."); }

                logger.LogInformation("DbInitializer data check/seeding finished.");
            }
        }

        private static void AddFlightsToList(List<Flight> flights, int departureAirportId, int arrivalAirportId, DateTime date, int distance, int durationMinutes, decimal basePrice, int numberOfFlightsPerDay)
        {
            for (int i = 0; i < numberOfFlightsPerDay; i++)
            {
                int hour;
                if (numberOfFlightsPerDay == 1) hour = random.Next(9, 17); // 1 chuyến thì giờ nào cũng được
                else if (i == 0) hour = random.Next(7, 10);      // Chuyến sớm
                else if (i == 1) hour = random.Next(12, 15);     // Chuyến trưa/chiều
                else hour = random.Next(17, 20);                 // Chuyến tối

                int minute = random.Next(0, 4) * 15;
                var departureTime = new DateTime(date.Year, date.Month, date.Day, hour, minute, 0);

                if (departureTime < DateTime.Now.AddHours(1) && date.Date == DateTime.Today) continue; // Chuyến bay phải cách ít nhất 1h

                var arrivalTime = departureTime.AddMinutes(durationMinutes);
                var airline = Airlines[random.Next(Airlines.Length)];
                var flightNumber = $"{airline.Substring(0, 2).ToUpper().Replace(" ", "")}{random.Next(101, 999)}";
                var capacity = random.Next(100, 151); // Số ghế từ 100 đến 150

                // Giá vé biến động
                decimal timeMultiplier = (departureTime.Hour < 9 || departureTime.Hour > 18) ? 0.95m :
                                         (departureTime.Hour >= 12 && departureTime.Hour <= 14) ? 1.0m : 1.1m;
                decimal dayMultiplier = (date.DayOfWeek == DayOfWeek.Friday || date.DayOfWeek == DayOfWeek.Saturday || date.DayOfWeek == DayOfWeek.Sunday) ? 1.20m : 0.95m;
                decimal randomFlightMultiplier = 1 + (decimal)(random.Next(-10, 11)) / 100;

                decimal finalPrice = basePrice * timeMultiplier * dayMultiplier * randomFlightMultiplier;
                finalPrice = Math.Max(400000m, Math.Round(finalPrice / 10000) * 10000); // Giá tối thiểu 400k

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
                    AvailableSeats = capacity, // Ban đầu bằng Capacity
                    Distance = distance
                });
            }
        }

        private static void CreateSectionsForFlight(List<Section> sectionListToAddTo, Flight flight)
        {
            if (flight == null || flight.FlightId == 0 || flight.Capacity <= 0) return;

            // Tỷ lệ ngẫu nhiên hơn cho các hạng ghế
            int businessPercentage = random.Next(10, 26); // 10% - 25% là Thương gia
            int premiumEconomyPercentage = 0; // Tạm thời không có Phổ thông đặc biệt

            if (flight.Capacity > 120 && random.Next(0, 3) == 0) // 1/3 cơ hội có Phổ thông đặc biệt cho máy bay lớn
            {
                premiumEconomyPercentage = random.Next(10, 21); // 10% - 20%
            }

            int businessCapacity = (int)Math.Floor(flight.Capacity * (businessPercentage / 100.0));
            int premiumEconomyCapacity = (int)Math.Floor(flight.Capacity * (premiumEconomyPercentage / 100.0));
            int economyCapacity = flight.Capacity - businessCapacity - premiumEconomyCapacity;

            // Đảm bảo economy capacity không âm nếu tỷ lệ tính ra quá lớn
            if (economyCapacity < 0)
            {
                economyCapacity = 0;
                // Có thể điều chỉnh lại business/premium nếu muốn
            }


            if (businessCapacity > 4) // Chỉ tạo nếu có ít nhất vài ghế
            {
                sectionListToAddTo.Add(new Section
                {
                    FlightId = flight.FlightId,
                    SectionName = "Thương gia",
                    Capacity = businessCapacity,
                    PriceMultiplier = Math.Round(1.6m + (decimal)random.NextDouble() * 0.6m, 2) // Giá từ 1.6 đến 2.2 lần
                });
            }

            if (premiumEconomyCapacity > 4)
            {
                sectionListToAddTo.Add(new Section
                {
                    FlightId = flight.FlightId,
                    SectionName = "Phổ thông Đặc biệt",
                    Capacity = premiumEconomyCapacity,
                    PriceMultiplier = Math.Round(1.2m + (decimal)random.NextDouble() * 0.3m, 2) // Giá từ 1.2 đến 1.5 lần
                });
            }

            if (economyCapacity > 0)
            {
                sectionListToAddTo.Add(new Section
                {
                    FlightId = flight.FlightId,
                    SectionName = "Phổ thông",
                    Capacity = economyCapacity,
                    PriceMultiplier = 1.0m
                });
            }
            // Nếu sau khi chia, không có section nào được tạo (ví dụ capacity chuyến bay quá nhỏ)
            // thì tạo một section Phổ thông với toàn bộ capacity
            if (!sectionListToAddTo.Any(s => s.FlightId == flight.FlightId) && flight.Capacity > 0)
            {
                sectionListToAddTo.Add(new Section
                {
                    FlightId = flight.FlightId,
                    SectionName = "Phổ thông",
                    Capacity = flight.Capacity,
                    PriceMultiplier = 1.0m
                });
            }
        }
    }
}
