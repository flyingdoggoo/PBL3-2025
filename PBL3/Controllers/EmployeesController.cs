using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore; // Nếu bạn có inject DbContext
// using PBL3.Data;               // Nếu bạn có inject DbContext
using PBL3.Models;               // Chứa AppUser, Employee,...
using PBL3.Models.ViewModels;    // *** QUAN TRỌNG: Chứa các ViewModels ***
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
[Authorize(Roles = "Admin")] // Chỉ Admin được quản lý nhân viên
public class EmployeesController : Controller
{
    private readonly UserManager<AppUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;

    public EmployeesController(UserManager<AppUser> userManager, RoleManager<IdentityRole> roleManager)
    {
        _userManager = userManager;
        _roleManager = roleManager;
    }

    // GET: Employees
    public async Task<IActionResult> Index()
    {
        // Lấy tất cả user có vai trò là "Employee"
        // Cách này hiệu quả hơn là GetUsersInRoleAsync nếu có nhiều user
        var employees = await _userManager.GetUsersInRoleAsync("Employee");

        return View(employees.OrderBy(e => e.FullName)); // Sắp xếp theo tên
    }

    // GET: Employees/Create
    public IActionResult Create()
    {
        return View(new CreateEmployeeViewModel()); // Truyền ViewModel trống
    }

    // POST: Employees/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateEmployeeViewModel model)
    {
        if (ModelState.IsValid)
        {
            // Kiểm tra email tồn tại
            var existingUser = await _userManager.FindByEmailAsync(model.Email);
            if (existingUser != null)
            {
                ModelState.AddModelError("Email", "Email đã được sử dụng.");
                return View(model);
            }

            // Tạo đối tượng Employee (vì nó kế thừa AppUser và có thể có thuộc tính riêng)
            var user = new Employee
            {
                UserName = model.Email,
                Email = model.Email,
                FullName = model.FullName,
                Age = model.Age,
                Address = model.Address,
                AddedDate = DateTime.UtcNow, // Gán ngày thêm cho Employee
                EmailConfirmed = true // Tạm thời xác nhận luôn hoặc gửi email xác nhận
            };

            var result = await _userManager.CreateAsync(user, model.Password);
            if (result.Succeeded)
            {
                // Đảm bảo Role "Employee" tồn tại
                const string roleName = "Employee";
                if (!await _roleManager.RoleExistsAsync(roleName))
                {
                    await _roleManager.CreateAsync(new IdentityRole(roleName));
                }
                // Gán vai trò "Employee"
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
        // Nếu lỗi, trả về View với model và lỗi
        return View(model);
    }

    // GET: Employees/Details/5 (Thêm action xem chi tiết)
    public async Task<IActionResult> Details(string id)
    {
        if (id == null) return NotFound();
        var user = await _userManager.FindByIdAsync(id);
        if (user == null || !await _userManager.IsInRoleAsync(user, "Employee"))
        {
            return NotFound();
        }
        // Có thể tạo ViewModel chi tiết nếu cần
        return View(user);
    }


    // GET: Employees/Edit/5
    public async Task<IActionResult> Edit(string id)
    {
        if (id == null) return NotFound();
        var user = await _userManager.FindByIdAsync(id);
        if (user == null || !await _userManager.IsInRoleAsync(user, "Employee")) return NotFound();

        // Tạo ViewModel để chỉnh sửa, chỉ chứa các trường được phép sửa
        var model = new EditEmployeeViewModel
        {
            Id = user.Id,
            Email = user.Email, // Hiển thị Email nhưng không cho sửa trực tiếp ở form này
            FullName = user.FullName,
            Age = user.Age,
            Address = user.Address
            // Lấy các thuộc tính khác nếu cần
        };
        return View(model);
    }

    // POST: Employees/Edit/5
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
            // Cập nhật các thuộc tính được phép thay đổi từ ViewModel
            if (user.FullName != model.FullName) { user.FullName = model.FullName; hasChanges = true; }
            if (user.Age != model.Age) { user.Age = model.Age; hasChanges = true; }
            if (user.Address != model.Address) { user.Address = model.Address; hasChanges = true; }
            // Cập nhật Email/UserName cần xử lý phức tạp hơn (xác thực, kiểm tra trùng)
            // Cập nhật Role cũng cần RoleManager

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
        // Nếu ModelState không hợp lệ hoặc cập nhật thất bại, trả về View với model hiện tại
        return View(model);
    }


    // GET: Employees/Delete/5
    public async Task<IActionResult> Delete(string id)
    {
        if (id == null) return NotFound();
        var user = await _userManager.FindByIdAsync(id);
        if (user == null || !await _userManager.IsInRoleAsync(user, "Employee")) return NotFound();

        // Kiểm tra xem nhân viên có dữ liệu liên quan không (ví dụ: vé đã xử lý)
        // ViewData["HasRelatedData"] = await _context.Tickets.AnyAsync(t => t.BookingEmployeeId == id); // Cần DbContext

        return View(user); // Truyền user vào View để hiển thị thông tin xác nhận xóa
    }

    // POST: Employees/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user != null && await _userManager.IsInRoleAsync(user, "Employee"))
        {
            // Kiểm tra lại ràng buộc trước khi xóa
            // var hasRelatedTickets = await _context.Tickets.AnyAsync(t => t.BookingEmployeeId == id);
            // if (hasRelatedTickets)
            // {
            //     TempData["ErrorMessage"] = "Không thể xóa nhân viên đã xử lý vé.";
            //     return RedirectToAction(nameof(Delete), new { id = id });
            // }

            var result = await _userManager.DeleteAsync(user);
            if (result.Succeeded)
            {
                TempData["SuccessMessage"] = "Xóa tài khoản nhân viên thành công!";
                return RedirectToAction(nameof(Index));
            }
            // Xử lý lỗi nếu xóa thất bại
            TempData["ErrorMessage"] = "Xóa tài khoản nhân viên thất bại. ";
            foreach (var error in result.Errors) { TempData["ErrorMessage"] += error.Description + " "; }
            return RedirectToAction(nameof(Delete), new { id = id }); // Quay lại trang Delete
        }
        else
        {
            TempData["ErrorMessage"] = "Không tìm thấy nhân viên để xóa hoặc người dùng không phải nhân viên.";
            return RedirectToAction(nameof(Index));
        }
    }

    // GET: Employees/ResetPassword/5 (Thêm chức năng Reset mật khẩu cho Admin)
    [HttpGet]
    public async Task<IActionResult> ResetPassword(string id)
    {
        if (id == null) return NotFound();
        var user = await _userManager.FindByIdAsync(id);
        if (user == null || !await _userManager.IsInRoleAsync(user, "Employee")) return NotFound();

        var model = new ResetPasswordAdminViewModel { UserId = user.Id, Email = user.Email };
        return View(model);
    }

    // POST: Employees/ResetPassword
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

        // Tạo token reset password
        var token = await _userManager.GeneratePasswordResetTokenAsync(user);
        // Reset mật khẩu bằng token và mật khẩu mới
        var result = await _userManager.ResetPasswordAsync(user, token, model.NewPassword);

        if (result.Succeeded)
        {
            TempData["SuccessMessage"] = $"Đặt lại mật khẩu cho nhân viên {user.Email} thành công.";
            return RedirectToAction(nameof(Index));
        }

        TempData["ErrorMessage"] = "Đặt lại mật khẩu thất bại. ";
        foreach (var error in result.Errors) { TempData["ErrorMessage"] += error.Description + " "; }
        return View(model); // Quay lại view reset với lỗi
    }

}
