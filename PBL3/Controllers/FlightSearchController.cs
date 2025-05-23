
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PBL3.Data;
using PBL3.Models;
using PBL3.Models.ViewModels;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace PBL3.Controllers
{
    public class FlightSearchController : Controller
    {
        private readonly ApplicationDbContext _context;

        public FlightSearchController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> Index(FlightSearchViewModel searchModel)
        {
            if (searchModel.DepartureAirportId <= 0 ||
                searchModel.ArrivalAirportId <= 0 ||
                searchModel.DepartureDate == default(DateTime))
            {
                return RedirectToAction("Index", "Home", new { needsSearch = true });
            }
            var query = _context.Flights
                                .Where(f => f.StartingDestination == searchModel.DepartureAirportId &&
                                            f.ReachingDestination == searchModel.ArrivalAirportId &&
                                            f.StartingTime.Date == searchModel.DepartureDate.Date &&
                                            f.AvailableSeats > 0)
                                .OrderBy(f => f.StartingTime);

            var flights = await query.AsNoTracking().ToListAsync();
            ViewData["SearchCriteria"] = searchModel;

            return View(flights);
        }
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var flight = await _context.Flights
                .Include(f => f.Sections)
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.FlightId == id);

            if (flight == null) return NotFound();

            return View(flight);
        }
    }
}