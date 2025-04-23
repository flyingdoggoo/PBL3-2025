using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using PBL3.Models;
namespace PBL3.Data
{
    public class ApplicationDbContext : IdentityDbContext<AppUser> 
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }
        public DbSet<AppUser> Users { get; set; }
        public DbSet<Employee> Employees { get; set; }
        public DbSet<SystemManager> SystemManagers { get; set; }
        public DbSet<Passenger> Passengers { get; set; }
        public DbSet<Ticket> Tickets { get; set; }
        public DbSet<Flight> Flights { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<Employee>().ToTable("Employees");
            modelBuilder.Entity<SystemManager>().ToTable("SystemManagers");
            modelBuilder.Entity<Passenger>().ToTable("Passengers");
        }

    }
}
