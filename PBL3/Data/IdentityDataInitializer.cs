using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging; // Thêm nếu muốn dùng ILogger
using Microsoft.Extensions.DependencyInjection; // Thêm cho GetRequiredService
using PBL3.Models; // Namespace chứa AppUser, Passenger, Employee, SystemManager
using System;
using System.Linq; // Thêm cho Select
using System.Threading.Tasks;

namespace PBL3.Data
{
    public static class IdentityDataInitializer
    {
        public static async Task SeedRolesAndAdminUserAsync(IServiceProvider serviceProvider)
        {
            var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
            var logger = loggerFactory.CreateLogger("IdentityDataInitializer"); // Tạo logger

            var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = serviceProvider.GetRequiredService<UserManager<AppUser>>();

            logger.LogInformation("Starting identity seeding (Roles, Admin, Test User)...");

            // --- 1. Tạo các Roles cần thiết ---
            string[] roleNames = { "Admin", "Employee", "Passenger" };
            foreach (var roleName in roleNames)
            {
                if (!await roleManager.RoleExistsAsync(roleName))
                {
                    logger.LogInformation($"Creating role: {roleName}");
                    var roleResult = await roleManager.CreateAsync(new IdentityRole(roleName));
                    if (!roleResult.Succeeded)
                    {
                        logger.LogError($"Error creating role {roleName}: {string.Join(", ", roleResult.Errors.Select(e => e.Description))}");
                    }
                }
            }

            // --- 2. Tạo tài khoản Admin mặc định ---
            string adminEmail = "admin@gmail.com";
            string adminPassword = "123456"; // **LƯU Ý: Mật khẩu yếu cho test, đổi khi production**

            var adminUser = await userManager.FindByEmailAsync(adminEmail);
            if (adminUser == null)
            {
                logger.LogInformation($"Creating admin user: {adminEmail}");
                var newAdminUser = new SystemManager // Admin cũng là SystemManager (và Employee)
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    FullName = "Administrator Prime", // Tên đầy đủ
                    Age = 35,
                    Address = "Head Office, Capital City",
                    EmailConfirmed = true,
                    AddedDate = DateTime.UtcNow
                };
                var createAdminResult = await userManager.CreateAsync(newAdminUser, adminPassword);
                if (createAdminResult.Succeeded)
                {
                    await userManager.AddToRoleAsync(newAdminUser, "Admin");
                    await userManager.AddToRoleAsync(newAdminUser, "Employee"); // Admin cũng là Employee
                    logger.LogInformation($"Admin user {adminEmail} created and roles assigned.");
                }
                else
                {
                    logger.LogError($"Error creating admin user {adminEmail}: {string.Join(", ", createAdminResult.Errors.Select(e => e.Description))}");
                }
            }
            else
            {
                logger.LogInformation($"Admin user {adminEmail} already exists. Ensuring roles...");
                if (!await userManager.IsInRoleAsync(adminUser, "Admin")) await userManager.AddToRoleAsync(adminUser, "Admin");
                if (!await userManager.IsInRoleAsync(adminUser, "Employee")) await userManager.AddToRoleAsync(adminUser, "Employee");
            }


            // --- 3. TẠO TÀI KHOẢN TEST PASSENGER ---
            string passengerTestEmail = "test@gmail.com";
            string passengerTestPassword = "123456"; // Mật khẩu cho user test

            var testPassenger = await userManager.FindByEmailAsync(passengerTestEmail);
            if (testPassenger == null)
            {
                logger.LogInformation($"Creating test passenger user: {passengerTestEmail}");
                var newTestPassenger = new Passenger // Tạo đối tượng Passenger
                {
                    UserName = passengerTestEmail,
                    Email = passengerTestEmail,
                    FullName = "Test Passenger User",
                    Age = 28,
                    Address = "123 Test Street, Sample City",
                    EmailConfirmed = true, // Xác nhận email để đăng nhập ngay
                    PassportNumber = "P01234567" // Thuộc tính riêng của Passenger
                };

                var createPassengerResult = await userManager.CreateAsync(newTestPassenger, passengerTestPassword);
                if (createPassengerResult.Succeeded)
                {
                    await userManager.AddToRoleAsync(newTestPassenger, "Passenger");
                    logger.LogInformation($"Test passenger user {passengerTestEmail} created and 'Passenger' role assigned.");
                }
                else
                {
                    logger.LogError($"Error creating test passenger user {passengerTestEmail}: {string.Join(", ", createPassengerResult.Errors.Select(e => e.Description))}");
                }
            }
            else
            {
                logger.LogInformation($"Test passenger user {passengerTestEmail} already exists. Ensuring 'Passenger' role...");
                if (!await userManager.IsInRoleAsync(testPassenger, "Passenger")) await userManager.AddToRoleAsync(testPassenger, "Passenger");
            }

            // --- 4. TẠO TÀI KHOẢN TEST EMPLOYEE (Nếu cần) ---
            string employeeTestEmail = "employee@gmail.com";
            string employeeTestPassword = "123456";

            var testEmployee = await userManager.FindByEmailAsync(employeeTestEmail);
            if (testEmployee == null)
            {
                logger.LogInformation($"Creating test employee user: {employeeTestEmail}");
                var newTestEmployee = new Employee
                {
                    UserName = employeeTestEmail,
                    Email = employeeTestEmail,
                    FullName = "Test Employee User",
                    Age = 32,
                    Address = "456 Staff Avenue, Worksville",
                    EmailConfirmed = true,
                    AddedDate = DateTime.UtcNow
                };
                var createEmployeeResult = await userManager.CreateAsync(newTestEmployee, employeeTestPassword);
                if (createEmployeeResult.Succeeded)
                {
                    await userManager.AddToRoleAsync(newTestEmployee, "Employee");
                    logger.LogInformation($"Test employee user {employeeTestEmail} created and 'Employee' role assigned.");
                }
                else
                {
                    logger.LogError($"Error creating test employee user {employeeTestEmail}: {string.Join(", ", createEmployeeResult.Errors.Select(e => e.Description))}");
                }
            }
            else
            {
                logger.LogInformation($"Test employee user {employeeTestEmail} already exists. Ensuring 'Employee' role...");
                if (!await userManager.IsInRoleAsync(testEmployee, "Employee")) await userManager.AddToRoleAsync(testEmployee, "Employee");
            }


            logger.LogInformation("Identity seeding finished.");
        }
    }
}