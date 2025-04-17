using Microsoft.AspNetCore.Mvc;

namespace PBL3.Controllers.Account
{
    public class AccountController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
