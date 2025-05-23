using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace PBL3.Controllers
{
    [Authorize(Roles = "Employee,Admin")] 
    public class EmployeeDashboardController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}