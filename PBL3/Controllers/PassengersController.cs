using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PBL3.Data;
using PBL3.Models;
using PBL3.Models.ViewModels;
using PBL3.Utils;
using System.Linq;
using System.Threading.Tasks;

namespace PBL3.Controllers
{
    [Authorize(Roles = "Admin,Employee")]
    public class PassengersController : Controller
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly ApplicationDbContext _context;

        public PassengersController(UserManager<AppUser> userManager, ApplicationDbContext context)
        {
            _userManager = userManager;
            _context = context;
        }

        public async Task<IActionResult> Index(string sortOrder, string currentFilter, string searchString, int? pageNumber)
        {
            ViewData["CurrentSort"] = sortOrder;
            ViewData["NameSortParm"] = string.IsNullOrEmpty(sortOrder) ? "name_desc" : "";
            ViewData["EmailSortParm"] = sortOrder == "Email" ? "email_desc" : "Email";
            ViewData["AgeSortParm"] = sortOrder == "Age" ? "age_desc" : "Age";

            if (searchString != null)
            {
                pageNumber = 1;
            }
            else
            {
                searchString = currentFilter;
            }
            ViewData["CurrentFilter"] = searchString;

            IQueryable<Passenger> passengersQuery = _context.Users.OfType<Passenger>();

            if (!string.IsNullOrEmpty(searchString))
            {
                passengersQuery = passengersQuery.Where(s =>
                   (s.FullName != null && s.FullName.Contains(searchString, StringComparison.OrdinalIgnoreCase)) ||
                   (s.Email != null && s.Email.Contains(searchString, StringComparison.OrdinalIgnoreCase))
                );
            }

            switch (sortOrder)
            {
                case "name_desc":
                    passengersQuery = passengersQuery.OrderByDescending(s => s.FullName);
                    break;
                case "Email":
                    passengersQuery = passengersQuery.OrderBy(s => s.Email);
                    break;
                case "email_desc":
                    passengersQuery = passengersQuery.OrderByDescending(s => s.Email);
                    break;
                case "Age":
                    passengersQuery = passengersQuery.OrderBy(s => s.Age);
                    break;
                case "age_desc":
                    passengersQuery = passengersQuery.OrderByDescending(s => s.Age);
                    break;
                default:
                    passengersQuery = passengersQuery.OrderBy(s => s.FullName);
                    break;
            }

            int pageSize = 10;
            var paginatedPassengers = await PaginatedList<AppUser>.CreateAsync(passengersQuery, pageNumber ?? 1, pageSize);

            return View(paginatedPassengers);
        }

        public async Task<IActionResult> Details(string id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var passenger = await _userManager.FindByIdAsync(id);
            if (passenger == null || !await _userManager.IsInRoleAsync(passenger, "Passenger"))
            {
                return NotFound("Passenger not found or is not a passenger.");
            }
            var passengerDetails = passenger as Passenger;
            return View(passengerDetails ?? passenger);
        }

        public async Task<IActionResult> Edit(string id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var user = await _userManager.FindByIdAsync(id);
            if (user == null || !await _userManager.IsInRoleAsync(user, "Passenger"))
            {
                return NotFound("Passenger not found or is not a passenger.");
            }

            var passenger = user as Passenger;

            var model = new EditPassengerViewModel
            {
                Id = user.Id,
                Email = user.Email,
                FullName = user.FullName,
                Age = user.Age,
                Address = user.Address,
                PassportNumber = passenger?.PassportNumber
            };
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, EditPassengerViewModel model)
        {
            if (id != model.Id)
            {
                return NotFound();
            }

            var user = await _userManager.FindByIdAsync(id);
            if (user == null || !await _userManager.IsInRoleAsync(user, "Passenger"))
            {
                return NotFound("Passenger not found or is not a passenger.");
            }

            if (ModelState.IsValid)
            {
                bool hasChanges = false;
                if (user.FullName != model.FullName) { user.FullName = model.FullName; hasChanges = true; }
                if (user.Age != model.Age) { user.Age = model.Age; hasChanges = true; }
                if (user.Address != model.Address) { user.Address = model.Address; hasChanges = true; }

                if (user is Passenger passengerInstance)
                {
                    if (passengerInstance.PassportNumber != model.PassportNumber)
                    {
                        passengerInstance.PassportNumber = model.PassportNumber;
                        hasChanges = true;
                    }
                }

                if (!hasChanges)
                {
                    TempData["InfoMessage"] = "Không có thông tin nào được thay đổi.";
                    return RedirectToAction(nameof(Index));
                }

                var result = await _userManager.UpdateAsync(user);
                if (result.Succeeded)
                {
                    TempData["SuccessMessage"] = "Cập nhật thông tin hành khách thành công!";
                    return RedirectToAction(nameof(Index));
                }
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> ResetPassword(string id)
        {
            if (id == null) return NotFound();
            var user = await _userManager.FindByIdAsync(id);
            if (user == null || !await _userManager.IsInRoleAsync(user, "Passenger"))
            {
                return NotFound("Passenger not found or is not a passenger.");
            }
            var model = new ResetPasswordAdminViewModel { UserId = user.Id, Email = user.Email };
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(ResetPasswordAdminViewModel model)
        {
            if (!ModelState.IsValid) return View(model);
            var user = await _userManager.FindByIdAsync(model.UserId);
            if (user == null || !await _userManager.IsInRoleAsync(user, "Passenger"))
            {
                TempData["ErrorMessage"] = "Không tìm thấy hành khách.";
                return RedirectToAction(nameof(Index));
            }
            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var result = await _userManager.ResetPasswordAsync(user, token, model.NewPassword);

            if (result.Succeeded)
            {
                TempData["SuccessMessage"] = $"Đặt lại mật khẩu cho hành khách {user.Email} thành công.";
                return RedirectToAction(nameof(Index));
            }
            TempData["ErrorMessage"] = "Đặt lại mật khẩu thất bại. ";
            foreach (var error in result.Errors) { TempData["ErrorMessage"] += error.Description + " "; }
            return View(model);
        }

        public async Task<IActionResult> Delete(string id)
        {
            if (id == null)
            {
                return NotFound();
            }
            var passenger = await _userManager.FindByIdAsync(id);
            if (passenger == null || !await _userManager.IsInRoleAsync(passenger, "Passenger"))
            {
                return NotFound("Passenger not found or is not a passenger.");
            }
            return View(passenger);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            var passenger = await _userManager.FindByIdAsync(id);
            if (passenger != null && await _userManager.IsInRoleAsync(passenger, "Passenger"))
            {
                var result = await _userManager.DeleteAsync(passenger);
                if (result.Succeeded)
                {
                    TempData["SuccessMessage"] = "Xóa tài khoản hành khách thành công!";
                    return RedirectToAction(nameof(Index));
                }
                TempData["ErrorMessage"] = "Xóa tài khoản hành khách thất bại. ";
                foreach (var error in result.Errors) { TempData["ErrorMessage"] += error.Description + " "; }
                return RedirectToAction(nameof(Delete), new { id = id });
            }
            else
            {
                TempData["ErrorMessage"] = "Không tìm thấy hành khách để xóa hoặc người dùng không phải hành khách.";
                return RedirectToAction(nameof(Index));
            }
        }
    }
}