using Microsoft.AspNetCore.Identity;
using PBL3.Models; // Namespace chứa AppUser của bạn
using System;
using System.Threading.Tasks;

namespace PBL3.Data // Hoặc namespace bạn muốn
{
    public static class IdentityDataInitializer
    {
        public static async Task SeedRolesAndAdminUserAsync(IServiceProvider serviceProvider)
        {
            // Lấy các dịch vụ cần thiết từ Dependency Injection
            var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = serviceProvider.GetRequiredService<UserManager<AppUser>>();

            // --- 1. Tạo các Roles cần thiết ---
            string[] roleNames = { "Admin", "Employee", "Passenger" };
            IdentityResult roleResult;

            foreach (var roleName in roleNames)
            {
                var roleExist = await roleManager.RoleExistsAsync(roleName);
                if (!roleExist)
                {
                    // Tạo role mới nếu chưa tồn tại
                    roleResult = await roleManager.CreateAsync(new IdentityRole(roleName));
                    // (Thêm xử lý lỗi nếu cần thiết)
                }
            }

            // --- 2. Tạo tài khoản Admin mặc định ---
            string adminEmail = "admin@yourapp.com"; // Đặt email admin của bạn
            string adminPassword = "Password123!";   // Đặt mật khẩu mạnh!

            // Kiểm tra xem admin user đã tồn tại chưa
            var adminUser = await userManager.FindByEmailAsync(adminEmail);

            if (adminUser == null)
            {
                // Tạo user Admin mới nếu chưa tồn tại
                // Có thể tạo SystemManager trực tiếp nếu nó kế thừa AppUser và có thuộc tính riêng
                var newAdminUser = new SystemManager // Hoặc new AppUser nếu SystemManager không có thuộc tính riêng
                {
                    UserName = adminEmail, // Identity yêu cầu UserName
                    Email = adminEmail,
                    FullName = "Administrator", // Tên đầy đủ
                    Age = 30, // Tuổi ví dụ
                    Address = "Office", // Địa chỉ ví dụ
                    EmailConfirmed = true, // Xác nhận email luôn để đăng nhập được ngay
                                           // Gán các thuộc tính khác nếu cần (vd: AddedDate cho Employee/SystemManager)
                    AddedDate = DateTime.UtcNow
                };

                var createAdminResult = await userManager.CreateAsync(newAdminUser, adminPassword);

                // Gán vai trò "Admin" cho user vừa tạo
                if (createAdminResult.Succeeded)
                {
                    await userManager.AddToRoleAsync(newAdminUser, "Admin");
                    // Có thể gán thêm vai trò khác nếu cần, ví dụ: Employee
                    await userManager.AddToRoleAsync(newAdminUser, "Employee");
                }
                else
                {
                    // Xử lý lỗi nếu tạo user thất bại (ví dụ: log lỗi)
                    Console.WriteLine($"Error creating admin user: {string.Join(", ", createAdminResult.Errors.Select(e => e.Description))}");
                }
            }
            else
            {
                // Nếu Admin user đã tồn tại, đảm bảo user đó có vai trò Admin
                var isAdmin = await userManager.IsInRoleAsync(adminUser, "Admin");
                if (!isAdmin)
                {
                    await userManager.AddToRoleAsync(adminUser, "Admin");
                }
                // Đảm bảo có cả vai trò Employee nếu SystemManager cần
                var isEmployee = await userManager.IsInRoleAsync(adminUser, "Employee");
                if (!isEmployee && adminUser is Employee) // Chỉ gán nếu user là Employee/SystemManager
                {
                    await userManager.AddToRoleAsync(adminUser, "Employee");
                }
            }
        }
    }
}