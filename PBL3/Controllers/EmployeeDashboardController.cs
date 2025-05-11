using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace PBL3.Controllers
{
    [Authorize(Roles = "Employee,Admin")] // Cho phép cả Admin truy cập (tùy chọn)
    public class EmployeeDashboardController : Controller
    {
        public IActionResult Index()
        {
            // Có thể truyền dữ liệu tổng quan cho Employee vào đây
            return View(); // Sẽ sử dụng _EmployeeLayout.cshtml
        }
    }
}