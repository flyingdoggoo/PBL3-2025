using Microsoft.AspNetCore.Identity;
using PBL3.Models;
using System.Data;

namespace PBL3.Data
{
    public static class DataSeeder
    {
        public static async Task SeedAsync(WebApplication app)
        {
            using var scope = app.Services.CreateScope();
            var services = scope.ServiceProvider;

            var userManager = services.GetRequiredService<UserManager<AppUser>>();
            var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();

            await SeedRoles(roleManager);
            await SeedTestUsers(userManager);

            // Tạo tài khoản mặc định
            var defaultUser = new AppUser
            {
                UserName = "employee@example.com",
                Email = "employee@example.com",
                FullName = "Default Employee",
                Address = "Default Address",
                Age = 30,
                //Role = "Employee",
                EmailConfirmed = true
            };

            if (await userManager.FindByEmailAsync(defaultUser.Email) == null)
            {
                var result = await userManager.CreateAsync(defaultUser, "123");
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(defaultUser, "Employee");
                }
            }
        }

        private static async Task SeedRoles(RoleManager<IdentityRole> roleManager)
        {
            if (!await roleManager.RoleExistsAsync("Admin"))
                await roleManager.CreateAsync(new IdentityRole("Admin"));

            if (!await roleManager.RoleExistsAsync("Employee"))
                await roleManager.CreateAsync(new IdentityRole("Employee"));

            if (!await roleManager.RoleExistsAsync("Passenger"))
                await roleManager.CreateAsync(new IdentityRole("Passenger"));
        }

        public static async Task SeedTestUsers(UserManager<AppUser> userManager)
        {
            // Admin user
            String email = "admin@aaa.com";
            String role = "Admin";
            var admin = new AppUser
            {
                UserName = email,
                NormalizedUserName = email.ToUpper(),
                Email = email,
                NormalizedEmail = email.ToUpper(),
                EmailConfirmed = true,
                PhoneNumber = "0123456789",
                PhoneNumberConfirmed = true,
                LockoutEnabled = false,

                // Add your custom required fields here
                FullName = "Test User",
                Age = 30,
                Address = "123 Seed St, Dev City",
                //Role = role
            };
            await CreateUser(userManager, admin, "abc123!", "Admin");
            Console.WriteLine($"Created user: {admin.Email}");

            String email1 = "employee@aaa.com";
            String role1 = "Employee";
            var employee = new AppUser
            {
                UserName = email1,
                NormalizedUserName = email1.ToUpper(),
                Email = email1,
                NormalizedEmail = email1.ToUpper(),
                EmailConfirmed = true,
                PhoneNumber = "0123456789",
                PhoneNumberConfirmed = true,
                LockoutEnabled = false,

                // Add your custom required fields here
                FullName = "Test User",
                Age = 30,
                Address = "123 Seed St, Dev City",
                //Role = role1
            };
            await CreateUser(userManager, employee, "abc123!", "Employee");
            Console.WriteLine($"Created user: {employee.Email}");

            String email2 = "passenger@aaa.com";
            String role2 = "Passenger";
            var passenger = new AppUser
            {
                UserName = email2,
                NormalizedUserName = email2.ToUpper(),
                Email = email2,
                NormalizedEmail = email2.ToUpper(),
                EmailConfirmed = true,
                PhoneNumber = "0123456789",
                PhoneNumberConfirmed = true,
                LockoutEnabled = false,

                // Add your custom required fields here
                FullName = "Test User",
                Age = 30,
                Address = "123 Seed St, Dev City",
                //Role = role2
            };
            await CreateUser(userManager, passenger, "abc123!", "Passenger");
            Console.WriteLine($"Created user: {passenger.Email}");
        }

        private static async Task CreateUser(UserManager<AppUser> userManager, AppUser user, string password, string role)
        {
            if (await userManager.FindByEmailAsync(user.Email) == null)
            {
                var result = await userManager.CreateAsync(user, password);
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(user, role);
                }
            }
        }
    }
}
