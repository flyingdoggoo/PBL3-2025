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
        private static readonly string[] Airlines = { "Vietnam Airlines", "Vietjet Air", "Bamboo Airways", "Pacific Airlines", "Vietravel Airlines" };

        public static void Initialize(IServiceProvider serviceProvider)
        {
            var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
            var logger = loggerFactory.CreateLogger("DbInitializer");
            logger.LogInformation("Attempting to initialize database data...");

            using (var context = new ApplicationDbContext(
                serviceProvider.GetRequiredService<DbContextOptions<ApplicationDbContext>>()))
            {
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
                        return;
                    }
                }
                else { logger.LogInformation("Airports already exist. Skipping airport seeding."); }
                var airportsDictionary = context.Airports.AsEnumerable().ToDictionary(a => a.Code, a => a.Id);
                if (airportsDictionary.Count < 2)
                {
                    logger.LogError("Cannot seed flights: Need at least 2 airports in the database for routes.");
                    return;
                }
                if (!context.Flights.Any())
                {
                    logger.LogInformation("Flights table is empty. Generating new flight, section, and seat data...");
                    var flightsToSeed = new List<Flight>();

                    var today = DateTime.Today;
                    var allAirportCodes = airportsDictionary.Keys.ToList();
                    var allPossibleRoutes = new List<(string From, string To, int Distance, int Duration, decimal Price)>();
                    foreach (var fromCode in allAirportCodes)
                    {
                        foreach (var toCode in allAirportCodes)
                        {
                            if (fromCode == toCode) continue;
                            int estimatedDistance = random.Next(300, 1801);
                            int estimatedDuration = 60 + (int)(estimatedDistance / 8.5);
                            decimal estimatedPrice = 450000m + (estimatedDistance * 750m);
                            allPossibleRoutes.Add((fromCode, toCode, estimatedDistance, estimatedDuration, estimatedPrice));
                        }
                    }
                    int numberOfRoutesToSelect = 8;
                    var routesToSeed = allPossibleRoutes.OrderBy(r => Guid.NewGuid()).Take(numberOfRoutesToSelect).ToList();

                    int numberOfDaysToSeed = 7;
                    int flightsPerRoutePerDay = 2;
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
                            context.SaveChanges();
                            logger.LogInformation($"SUCCESS: Finished seeding {flightsToSeed.Count} flights into database.");
                            logger.LogInformation("Generating sections for newly seeded flights...");
                            var sectionsToSeed = new List<Section>();
                            foreach (var seededFlight in flightsToSeed)
                            {
                                CreateSectionsForFlight(sectionsToSeed, seededFlight);
                            }

                            if (sectionsToSeed.Any())
                            {
                                context.Sections.AddRange(sectionsToSeed);
                                context.SaveChanges();
                                logger.LogInformation($"SUCCESS: Finished seeding {sectionsToSeed.Count} sections.");
                                logger.LogInformation("Generating seats for newly seeded sections...");
                                var seatsToSeed = new List<Seat>();
                                foreach (var newSection in sectionsToSeed)
                                {
                                    SeatGenerator.GenerateSeatsForSection(seatsToSeed, newSection, 1);
                                }

                                if (seatsToSeed.Any())
                                {
                                    context.Seats.AddRange(seatsToSeed);
                                    context.SaveChanges();
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
                if (numberOfFlightsPerDay == 1) hour = random.Next(9, 17);
                else if (i == 0) hour = random.Next(7, 10);
                else if (i == 1) hour = random.Next(12, 15);
                else hour = random.Next(17, 20);

                int minute = random.Next(0, 4) * 15;
                var departureTime = new DateTime(date.Year, date.Month, date.Day, hour, minute, 0);

                if (departureTime < DateTime.Now.AddHours(1) && date.Date == DateTime.Today) continue;

                var arrivalTime = departureTime.AddMinutes(durationMinutes);
                var airline = Airlines[random.Next(Airlines.Length)];
                var flightNumber = $"{airline.Substring(0, 2).ToUpper().Replace(" ", "")}{random.Next(101, 999)}";
                var capacity = random.Next(100, 151);
                decimal timeMultiplier = (departureTime.Hour < 9 || departureTime.Hour > 18) ? 0.95m :
                                         (departureTime.Hour >= 12 && departureTime.Hour <= 14) ? 1.0m : 1.1m;
                decimal dayMultiplier = (date.DayOfWeek == DayOfWeek.Friday || date.DayOfWeek == DayOfWeek.Saturday || date.DayOfWeek == DayOfWeek.Sunday) ? 1.20m : 0.95m;
                decimal randomFlightMultiplier = 1 + (decimal)(random.Next(-10, 11)) / 100;

                decimal finalPrice = basePrice * timeMultiplier * dayMultiplier * randomFlightMultiplier;
                finalPrice = Math.Max(400000m, Math.Round(finalPrice / 10000) * 10000);

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

        private static void CreateSectionsForFlight(List<Section> sectionListToAddTo, Flight flight)
        {
            if (flight == null || flight.FlightId == 0 || flight.Capacity <= 0) return;
            int businessPercentage = random.Next(10, 26);
            int premiumEconomyPercentage = 0;

            if (flight.Capacity > 120 && random.Next(0, 3) == 0)
            {
                premiumEconomyPercentage = random.Next(10, 21);
            }

            int businessCapacity = (int)Math.Floor(flight.Capacity * (businessPercentage / 100.0));
            int premiumEconomyCapacity = (int)Math.Floor(flight.Capacity * (premiumEconomyPercentage / 100.0));
            int economyCapacity = flight.Capacity - businessCapacity - premiumEconomyCapacity;
            if (economyCapacity < 0)
            {
                economyCapacity = 0;
            }


            if (businessCapacity > 4)
            {
                sectionListToAddTo.Add(new Section
                {
                    FlightId = flight.FlightId,
                    SectionName = "Thương gia",
                    Capacity = businessCapacity,
                    PriceMultiplier = Math.Round(1.6m + (decimal)random.NextDouble() * 0.6m, 2)
                });
            }

            if (premiumEconomyCapacity > 4)
            {
                sectionListToAddTo.Add(new Section
                {
                    FlightId = flight.FlightId,
                    SectionName = "Phổ thông Đặc biệt",
                    Capacity = premiumEconomyCapacity,
                    PriceMultiplier = Math.Round(1.2m + (decimal)random.NextDouble() * 0.3m, 2)
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