using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using PBL3.Models;
using PBL3.Models.ViewModels;
using Microsoft.EntityFrameworkCore;
using PBL3.Data;
using PBL3.Services;

public class AccountController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly SignInManager<AppUser> _signInManager;
    private readonly UserManager<AppUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly IEmailService _emailService;
    private const int OtpValidityMinutes = 10; 

    public AccountController(ApplicationDbContext context,
                             SignInManager<AppUser> signInManager,
                             UserManager<AppUser> userManager,
                             RoleManager<IdentityRole> roleManager,
                             IEmailService emailService)
    {
        _context = context;
        _signInManager = signInManager;
        _userManager = userManager;
        _roleManager = roleManager;
        _emailService = emailService;
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

    private string GenerateOtp()
    {
        Random random = new Random();
        return random.Next(100000, 999999).ToString();
    }

    [HttpGet]
    [AllowAnonymous] // Forgot password should be accessible without login
    public IActionResult ForgotPassword()
    {
        return View();
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
    {
        if (ModelState.IsValid)
        {
            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
            {
                // Still don't reveal user existence.
                // Instead of a separate confirmation page, set a message and redirect to ResetPassword.
                TempData["InfoMessage"] = "If an account with that email exists, an OTP has been sent. Please check your email.";
                return RedirectToAction(nameof(ResetPassword), new { email = model.Email }); // Pass email
            }

            // Proceed with OTP generation and sending
            bool otpSent = await SendNewOtpForUser(user, model.Email);

            if (otpSent)
            {
                TempData["InfoMessage"] = "An OTP has been sent to your email. Please enter it below to reset your password.";
                return RedirectToAction(nameof(ResetPassword), new { email = model.Email });
            }
            else
            {
                // Error message already set by SendNewOtpForUser in ModelState
                // Fall through to return View(model)
            }
        }
        return View(model); // If ModelState invalid or OTP sending failed
    }

    private async Task<bool> SendNewOtpForUser(AppUser user, string emailForSending)
    {
        var shortOtpCode = GenerateOtp();
        var identityToken = await _userManager.GeneratePasswordResetTokenAsync(user);
        var now = DateTime.UtcNow;
        var expiryTime = now.AddMinutes(OtpValidityMinutes);

        // Invalidate/Delete previous active password reset OTPs for this user
        var previousOtps = await _context.UserOtps
            .Where(o => o.UserId == user.Id && !o.IsVerified && o.ExpiryTimestampUtc > now)
            .ToListAsync();

        if (previousOtps.Any())
        {
            _context.UserOtps.RemoveRange(previousOtps); // Delete old OTPs
            // Or mark them as IsVerified = true if you prefer to keep a record
        }

        var userOtpRecord = new UserOtp
        {
            UserId = user.Id,
            OtpCode = shortOtpCode,
            IdentityPasswordResetToken = identityToken,
            ExpiryTimestampUtc = expiryTime,
            IsVerified = false
        };

        _context.UserOtps.Add(userOtpRecord);
        // Save changes after adding new OTP and removing/invalidating old ones
        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException ex) // Handle potential DB errors
        {
            System.Diagnostics.Debug.WriteLine($"Error saving OTP to DB: {ex.Message}");
            ModelState.AddModelError(string.Empty, "A database error occurred. Please try again.");
            return false;
        }


        try
        {
            var emailSubject = "Your Password Reset OTP";
            var emailBody = $"<p>Dear {user.UserName ?? "User"},</p>" +
                            $"<p>Your One-Time Password (OTP) for resetting your password is: <strong>{shortOtpCode}</strong></p>" +
                            $"<p>This OTP is valid for {OtpValidityMinutes} minutes.</p>" +
                            $"<p>If you did not request this, please ignore this email.</p>" +
                            $"<p>Thanks,<br/>Your Application Team</p>";

            await _emailService.SendEmailAsync(emailForSending, emailSubject, emailBody);
            return true; // OTP sent successfully
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error sending OTP email: {ex.Message}");
            ModelState.AddModelError(string.Empty, "An error occurred while trying to send the OTP. Please try again later.");
            // If email sending fails, remove the OTP record we just added.
            _context.UserOtps.Remove(userOtpRecord);
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException dbEx)
            {
                System.Diagnostics.Debug.WriteLine($"Error cleaning up OTP from DB after failed send: {dbEx.Message}");
            }
            return false; // OTP sending failed
        }
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult ResetPassword(string email, string otp = "")
    {
        var model = new ResetPasswordViewModel { Email = email, Otp = otp };
        ViewBag.InfoMessage = TempData["InfoMessage"]; // Pass along messages from ForgotPassword
        return View(model);
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
    {

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var user = await _userManager.FindByEmailAsync(model.Email);
        if (user == null)
        {
            ModelState.AddModelError(string.Empty, "Invalid request or user not found.");
            return View(model);
        }

        var now = DateTime.UtcNow;
        var userOtpRecord = await _context.UserOtps
            .FirstOrDefaultAsync(o => o.UserId == user.Id &&
                                      o.OtpCode == model.Otp &&
                                      !o.IsVerified &&
                                      o.ExpiryTimestampUtc > now);

        if (userOtpRecord == null)
        {
            ModelState.AddModelError("Otp", "Invalid or expired OTP. Please try requesting a new one or resend.");
            return View(model);
        }

        var result = await _userManager.ResetPasswordAsync(user, userOtpRecord.IdentityPasswordResetToken, model.NewPassword);

        if (result.Succeeded)
        {
            userOtpRecord.IsVerified = true; // Mark as used
            // _context.UserOtps.Remove(userOtpRecord); // Or delete
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Your password has been reset successfully. Please log in with your new password.";
            return RedirectToAction(nameof(Login));
        }

        foreach (var error in result.Errors)
        {
            if (error.Code == "InvalidToken")
                ModelState.AddModelError("Otp", "The OTP or associated reset token is invalid. Please request a new one or resend.");
            else
                ModelState.AddModelError(string.Empty, error.Description);
        }
        return View(model);
    }

    // POST: /Account/ResendOtp
    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken] // Important for POST actions
    public async Task<IActionResult> ResendOtp(string email) // Expecting email from the form
    {
        if (string.IsNullOrEmpty(email))
        {
            TempData["ErrorMessage"] = "Email address is required to resend OTP.";
            return RedirectToAction(nameof(ResetPassword)); // Redirect back, user needs to enter email if missing
        }

        var user = await _userManager.FindByEmailAsync(email);
        if (user == null)
        {
            TempData["InfoMessage"] = "If an account with that email exists, a new OTP has been sent.";
            // Don't reveal user non-existence. Redirect to ResetPassword page with the email prefilled.
            return RedirectToAction(nameof(ResetPassword), new { email = email });
        }

        // Use the refactored method to send a new OTP
        bool otpSent = await SendNewOtpForUser(user, email);

        if (otpSent)
        {
            TempData["InfoMessage"] = "A new OTP has been sent to your email address.";
        }
        else
        {
            // Check if ModelState has errors and transfer to TempData if needed.
            if (ModelState.Any(m => m.Value.Errors.Any()))
            {
                TempData["ErrorMessage"] = ModelState.SelectMany(m => m.Value.Errors)
                                                     .FirstOrDefault()?.ErrorMessage
                                           ?? "An unknown error occurred while resending OTP.";
            }
        }
        // Always redirect back to the ResetPassword page, prefilling the email.
        return RedirectToAction(nameof(ResetPassword), new { email = email });
    }
}
