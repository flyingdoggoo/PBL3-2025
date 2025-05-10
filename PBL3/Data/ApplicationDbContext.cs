using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using PBL3.Models;

namespace PBL3.Data
{
    public class ApplicationDbContext : IdentityDbContext<AppUser> // Chỉ định AppUser làm User gốc
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // DbSet cho các entity gốc không thuộc Identity hoặc không kế thừa
        public DbSet<Ticket> Tickets { get; set; }
        public DbSet<Flight> Flights { get; set; }
        public DbSet<Airport> Airports { get; set; }
        public DbSet<Section> Sections { get; set; } // Thêm nếu có Section
        public DbSet<Seat> Seats { get; set; }
        // Không cần DbSet cho Employee, Passenger, SystemManager vì chúng dùng TPH

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder); // **CỰC KỲ QUAN TRỌNG**

            // --- Cấu hình riêng (nếu cần) ---

            // Cấu hình khóa ngoại cho Flight <-> Airport (Tránh delete cascade)
            modelBuilder.Entity<Flight>()
                .HasOne(f => f.DepartureAirport)
                .WithMany() // Hoặc WithMany(a => a.DepartingFlights) nếu có ở Airport
                .HasForeignKey(f => f.StartingDestination)
                .OnDelete(DeleteBehavior.Restrict); // Hoặc NoAction

            modelBuilder.Entity<Flight>()
                .HasOne(f => f.ArrivalAirport)
                .WithMany() // Hoặc WithMany(a => a.ArrivingFlights) nếu có ở Airport
                .HasForeignKey(f => f.ReachingDestination)
                .OnDelete(DeleteBehavior.Restrict); // Hoặc NoAction

            // Cấu hình kiểu dữ liệu decimal cho Price
            modelBuilder.Entity<Flight>()
                .Property(f => f.Price)
                .HasColumnType("decimal(18,2)");

            modelBuilder.Entity<Ticket>()
                .HasOne(t => t.Seat) // Ticket có một Seat
                .WithOne() // Một Seat chỉ liên kết với một Ticket tại một thời điểm (hoặc dùng WithMany nếu cần)
                .HasForeignKey<Ticket>(t => t.SeatId) // Khóa ngoại là Ticket.SeatId
                .OnDelete(DeleteBehavior.Restrict);

            // Bỏ cấu hình ToTable() cho các lớp kế thừa từ AppUser
        }
    }
}