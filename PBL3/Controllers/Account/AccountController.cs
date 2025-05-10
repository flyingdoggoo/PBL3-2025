using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using PBL3.Models;
using PBL3.Models.ViewModels;

public class AccountController : Controller
{
    private readonly SignInManager<AppUser> _signInManager;
    private readonly UserManager<AppUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;

    public AccountController(SignInManager<AppUser> signInManager, UserManager<AppUser> userManager, RoleManager<IdentityRole> roleManager)
    {
        _signInManager = signInManager;
        _userManager = userManager;
        _roleManager = roleManager; // Khởi tạo
    }

    [HttpGet]
    public IActionResult Login()
    {
        return View();
    }

    [HttpPost]
    [AllowAnonymous] // Thường thì Login Post không cần Authorize
    [ValidateAntiForgeryToken] // Nên thêm ValidateAntiForgeryToken
    public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null) // Sử dụng ViewModel chuẩn hơn
    {
        ViewData["ReturnUrl"] = returnUrl; // Lưu lại returnUrl
        if (ModelState.IsValid)
        {
            // Tìm user bằng Email
            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user != null)
            {
                // Đăng nhập bằng PasswordSignInAsync
                var result = await _signInManager.PasswordSignInAsync(user, model.Password, model.RememberMe, lockoutOnFailure: false); // lockoutOnFailure: xem xét chính sách khóa tài khoản

                if (result.Succeeded)
                {
                    // Lấy vai trò của người dùng
                    var roles = await _userManager.GetRolesAsync(user);

                    // Điều hướng dựa trên vai trò cao nhất (ví dụ: nếu vừa là Admin vừa là Employee thì vào Admin)
                    if (roles.Contains("Admin"))
                    {
                        // return RedirectToAction("Index", "SystemManager"); // Redirect đến Dashboard Admin
                        return LocalRedirect(returnUrl ?? Url.Action("Index", "SystemManager")); // Ưu tiên returnUrl
                    }
                    else if (roles.Contains("Employee"))
                    {
                        // return RedirectToAction("Index", "Employee"); // Redirect đến Dashboard Employee
                        return LocalRedirect(returnUrl ?? Url.Action("Index", "Employee"));
                    }
                    else // Mặc định là Passenger hoặc vai trò khác
                    {
                        // return RedirectToAction("Index", "Passenger"); // Redirect đến Dashboard Passenger
                        return LocalRedirect(returnUrl ?? Url.Action("Index", "Home")); // Hoặc trang chủ nếu ko có dashboard riêng
                    }
                }
                // if (result.RequiresTwoFactor) { /* Xử lý 2FA */ }
                // if (result.IsLockedOut) { /* Xử lý bị khóa */ }
                else
                {
                    ModelState.AddModelError(string.Empty, "Thông tin đăng nhập không hợp lệ.");
                    return View(model);
                }
            }
            else
            {
                ModelState.AddModelError(string.Empty, "Thông tin đăng nhập không hợp lệ.");
                return View(model);
            }

        }

        // If we got this far, something failed, redisplay form
        return View(model);
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult Register()
    {
        return View();
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model) // Sử dụng ViewModel
    {
        if (ModelState.IsValid)
        {
            // Tạo user mới
            var user = new Passenger // Tạo trực tiếp Passenger nếu đăng ký mặc định là Passenger
            {
                UserName = model.Email, // Identity yêu cầu UserName
                Email = model.Email,
                FullName = model.FullName,
                Age = model.Age,
                Address = model.Address,
                // KHÔNG set user.Role ở đây
            };

            var result = await _userManager.CreateAsync(user, model.Password);
            if (result.Succeeded)
            {
                // Đảm bảo vai trò "Passenger" tồn tại
                if (!await _roleManager.RoleExistsAsync("Passenger"))
                {
                    await _roleManager.CreateAsync(new IdentityRole("Passenger"));
                }
                // Gán vai trò "Passenger" cho user mới
                await _userManager.AddToRoleAsync(user, "Passenger");

                // Có thể tự động đăng nhập user sau khi đăng ký thành công
                // await _signInManager.SignInAsync(user, isPersistent: false);
                // return RedirectToAction("Index", "Home");

                TempData["SuccessMessage"] = "Đăng ký thành công! Vui lòng đăng nhập.";
                return RedirectToAction(nameof(Login)); // Chuyển đến trang đăng nhập
            }
            foreach (var error in result.Errors)
            {
                // Xử lý lỗi trùng email riêng biệt cho rõ ràng
                if (error.Code == "DuplicateUserName" || error.Code == "DuplicateEmail")
                {
                    ModelState.AddModelError("Email", "Email đã được sử dụng.");
                }
                else
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }
        }

        // If we got this far, something failed, redisplay form
        return View(model);
    }


    //[Authorize(Roles = "Admin")]
    //public IActionResult AdminDashboard()
    //{
    //    return RedirectToAction("Index", "SystemManager");
    //}

    //[Authorize(Roles = "Passenger")]
    //public IActionResult PassengerDashboard()
    //{
    //    return RedirectToAction("Index", "Passenger");
    //}

    //[Authorize(Roles = "Employee")]
    //public IActionResult EmployeeDashboard()
    //{
    //    return RedirectToAction("Index", "Employee");
    //}

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        return RedirectToAction("Index", "Home"); // Về trang chủ sau khi logout
    }
    [Authorize]
    public async Task<IActionResult> Profile()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return NotFound($"Không thể tải thông tin người dùng với ID '{_userManager.GetUserId(User)}'.");
        }
        // Cần ViewModel để hiển thị thông tin an toàn hơn, tránh lộ các thuộc tính của IdentityUser
        var model = new UserProfileViewModel
        {
            Email = user.Email,
            FullName = user.FullName,
            Age = user.Age,
            Address = user.Address,
            // Thêm các trường khác nếu cần
        };
        return View(model);
    }

    [Authorize]
    [HttpGet]
    public async Task<IActionResult> EditProfile() // Đổi tên action cho rõ ràng
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return NotFound();

        var model = new EditProfileViewModel // Dùng ViewModel riêng cho Edit
        {
            FullName = user.FullName,
            Age = user.Age,
            Address = user.Address
        };
        return View(model);
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditProfile(EditProfileViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var user = await _userManager.GetUserAsync(User);
        if (user == null) return NotFound();

        bool hasChanges = false;
        if (user.FullName != model.FullName) { user.FullName = model.FullName; hasChanges = true; }
        if (user.Age != model.Age) { user.Age = model.Age; hasChanges = true; }
        if (user.Address != model.Address) { user.Address = model.Address; hasChanges = true; }

        if (!hasChanges)
        {
            TempData["InfoMessage"] = "Không có thông tin nào được thay đổi.";
            return RedirectToAction(nameof(Profile));
        }

        var result = await _userManager.UpdateAsync(user);
        if (result.Succeeded)
        {
            TempData["SuccessMessage"] = "Thông tin cá nhân đã được cập nhật thành công.";
            return RedirectToAction(nameof(Profile));
        }

        foreach (var error in result.Errors)
        {
            ModelState.AddModelError(string.Empty, error.Description);
        }
        return View(model); // Trả về view edit với lỗi
    }

    [Authorize]
    [HttpGet]
    public IActionResult ChangePassword()
    {
        return View();
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return NotFound();
        }

        // Thay đổi mật khẩu
        var result = await _userManager.ChangePasswordAsync(user, model.CurrentPassword, model.NewPassword);
        if (result.Succeeded)
        {
            await _userManager.UpdateSecurityStampAsync(user);

            await _signInManager.SignInAsync(user, isPersistent: false);

            TempData["SuccessMessage"] = "Mật khẩu của bạn đã được thay đổi thành công.";
            return RedirectToAction(nameof(Profile));
        }

        foreach (var error in result.Errors)
        {
            ModelState.AddModelError(string.Empty, error.Description);
        }
        return View(model);
    }
}
