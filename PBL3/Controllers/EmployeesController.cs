using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PBL3.Models;
using PBL3.Models.ViewModels;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

[Authorize(Roles = "Admin")]
public class EmployeesController : Controller
{
    private readonly UserManager<AppUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;

    public EmployeesController(UserManager<AppUser> userManager, RoleManager<IdentityRole> roleManager)
    {
        _userManager = userManager;
        _roleManager = roleManager;
    }
    public async Task<IActionResult> Index()
    {
        var employees = await _userManager.GetUsersInRoleAsync("Employee");

        return View(employees.OrderBy(e => e.FullName));
    }
    public IActionResult Create()
    {
        return View(new CreateEmployeeViewModel());
    }
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateEmployeeViewModel model)
    {
        if (ModelState.IsValid)
        {
            var existingUser = await _userManager.FindByEmailAsync(model.Email);
            if (existingUser != null)
            {
                ModelState.AddModelError("Email", "Email đã được sử dụng.");
                return View(model);
            }
            var user = new Employee
            {
                UserName = model.Email,
                Email = model.Email,
                FullName = model.FullName,
                Age = model.Age,
                Address = model.Address,
                AddedDate = DateTime.UtcNow,
                EmailConfirmed = true
            };

            var result = await _userManager.CreateAsync(user, model.Password);
            if (result.Succeeded)
            {
                const string roleName = "Employee";
                if (!await _roleManager.RoleExistsAsync(roleName))
                {
                    await _roleManager.CreateAsync(new IdentityRole(roleName));
                }
                await _userManager.AddToRoleAsync(user, roleName);

                TempData["SuccessMessage"] = "Tạo tài khoản nhân viên mới thành công!";
                return RedirectToAction(nameof(Index));
            }
            foreach (var error in result.Errors)
            {
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
        return View(model);
    }
    public async Task<IActionResult> Details(string id)
    {
        if (id == null) return NotFound();
        var user = await _userManager.FindByIdAsync(id);
        if (user == null || !await _userManager.IsInRoleAsync(user, "Employee"))
        {
            return NotFound();
        }
        return View(user);
    }
    public async Task<IActionResult> Edit(string id)
    {
        if (id == null) return NotFound();
        var user = await _userManager.FindByIdAsync(id);
        if (user == null || !await _userManager.IsInRoleAsync(user, "Employee")) return NotFound();
        var model = new EditEmployeeViewModel
        {
            Id = user.Id,
            Email = user.Email,
            FullName = user.FullName,
            Age = user.Age,
            Address = user.Address
        };
        return View(model);
    }
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(string id, EditEmployeeViewModel model)
    {
        if (id != model.Id) return NotFound();

        var user = await _userManager.FindByIdAsync(id);
        if (user == null || !await _userManager.IsInRoleAsync(user, "Employee")) return NotFound();

        if (ModelState.IsValid)
        {
            bool hasChanges = false;
            if (user.FullName != model.FullName) { user.FullName = model.FullName; hasChanges = true; }
            if (user.Age != model.Age) { user.Age = model.Age; hasChanges = true; }
            if (user.Address != model.Address) { user.Address = model.Address; hasChanges = true; }

            if (!hasChanges)
            {
                TempData["InfoMessage"] = "Không có thông tin nào được thay đổi.";
                return RedirectToAction(nameof(Index));
            }

            var result = await _userManager.UpdateAsync(user);
            if (result.Succeeded)
            {
                TempData["SuccessMessage"] = "Cập nhật thông tin nhân viên thành công!";
                return RedirectToAction(nameof(Index));
            }
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
        }
        return View(model);
    }
    public async Task<IActionResult> Delete(string id)
    {
        if (id == null) return NotFound();
        var user = await _userManager.FindByIdAsync(id);
        if (user == null || !await _userManager.IsInRoleAsync(user, "Employee")) return NotFound();

        return View(user);
    }
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user != null && await _userManager.IsInRoleAsync(user, "Employee"))
        {

            var result = await _userManager.DeleteAsync(user);
            if (result.Succeeded)
            {
                TempData["SuccessMessage"] = "Xóa tài khoản nhân viên thành công!";
                return RedirectToAction(nameof(Index));
            }
            TempData["ErrorMessage"] = "Xóa tài khoản nhân viên thất bại. ";
            foreach (var error in result.Errors) { TempData["ErrorMessage"] += error.Description + " "; }
            return RedirectToAction(nameof(Delete), new { id = id });
        }
        else
        {
            TempData["ErrorMessage"] = "Không tìm thấy nhân viên để xóa hoặc người dùng không phải nhân viên.";
            return RedirectToAction(nameof(Index));
        }
    }
    [HttpGet]
    public async Task<IActionResult> ResetPassword(string id)
    {
        if (id == null) return NotFound();
        var user = await _userManager.FindByIdAsync(id);
        if (user == null || !await _userManager.IsInRoleAsync(user, "Employee")) return NotFound();

        var model = new ResetPasswordAdminViewModel { UserId = user.Id, Email = user.Email };
        return View(model);
    }
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetPassword(ResetPasswordAdminViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var user = await _userManager.FindByIdAsync(model.UserId);
        if (user == null || !await _userManager.IsInRoleAsync(user, "Employee"))
        {
            TempData["ErrorMessage"] = "Không tìm thấy nhân viên.";
            return RedirectToAction(nameof(Index));
        }
        var token = await _userManager.GeneratePasswordResetTokenAsync(user);
        var result = await _userManager.ResetPasswordAsync(user, token, model.NewPassword);

        if (result.Succeeded)
        {
            TempData["SuccessMessage"] = $"Đặt lại mật khẩu cho nhân viên {user.Email} thành công.";
            return RedirectToAction(nameof(Index));
        }

        TempData["ErrorMessage"] = "Đặt lại mật khẩu thất bại. ";
        foreach (var error in result.Errors) { TempData["ErrorMessage"] += error.Description + " "; }
        return View(model);
    }

}
