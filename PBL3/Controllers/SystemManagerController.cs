using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace PBL3.Controllers 
{
    [Authorize(Roles = "Admin")]
    public class SystemManagerController : Controller
    {
        public ActionResult Index()
        {
            return View(); 
        }

    }
}
