using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using PBL3.Data;
using PBL3.Models;
using Microsoft.Extensions.Logging; // Thêm using này

var builder = WebApplication.CreateBuilder(args);

// --- Cấu hình Services ---
builder.Services.AddControllersWithViews();

//var connectionString = builder.Configuration.GetConnectionString("KienConnection")
//    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

builder.Services.AddIdentity<AppUser, IdentityRole>(options =>
{
    options.SignIn.RequireConfirmedAccount = false; // Tắt yêu cầu xác thực email (để test)
    // Nới lỏng yêu cầu mật khẩu (chỉ để test, không nên dùng cho production)
    options.Password.RequireDigit = false;
    options.Password.RequiredLength = 1;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
    options.Password.RequireLowercase = false;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

// Cấu hình Application Cookie (tùy chọn, để xử lý redirect khi chưa đăng nhập)
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login"; // Đường dẫn đến trang đăng nhập
    options.AccessDeniedPath = "/Account/AccessDenied"; // Trang báo lỗi không có quyền
});


// --- Build ứng dụng ---
var app = builder.Build();


// --- ÁP DỤNG MIGRATIONS VÀ SEED DATA ---
// **QUAN TRỌNG: Thực hiện trong scope riêng để giải phóng DbContext**
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var logger = services.GetRequiredService<ILogger<Program>>();
    var dbContext = services.GetRequiredService<ApplicationDbContext>();

    try
    {
        logger.LogInformation("Applying database migrations...");
        dbContext.Database.Migrate(); // **1. Áp dụng Migrations**
        logger.LogInformation("Database migrations applied successfully.");

        logger.LogInformation("Seeding identity data...");
        await IdentityDataInitializer.SeedRolesAndAdminUserAsync(services); // **2. Seed Roles & Admin**
        logger.LogInformation("Identity data seeded successfully.");

        logger.LogInformation("Seeding airport and flight data...");
        DbInitializer.Initialize(services); // **3. Seed Airports & Flights**
        logger.LogInformation("Airport and flight data seeded successfully.");

    }
    catch (Exception ex)
    {
        logger.LogError(ex, "An error occurred during database migration or seeding.");
        // Cân nhắc dừng ứng dụng nếu lỗi nghiêm trọng ở đây
    }
}

// --- Cấu hình HTTP Pipeline ---
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// **QUAN TRỌNG: Đúng thứ tự Authentication và Authorization**
app.UseAuthentication(); // Xác thực người dùng
app.UseAuthorization(); // Kiểm tra quyền hạn

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// Thêm dòng này nếu bạn dùng Identity UI (các trang như /Identity/Account/Login)
// app.MapRazorPages();

// --- Chạy ứng dụng ---
app.Run();