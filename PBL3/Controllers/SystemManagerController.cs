using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace PBL3.Controllers // Namespace gốc của Controller
{
    [Authorize(Roles = "Admin")]
    public class SystemManagerController : Controller
    {
        // GET: SystemManager/Index (Dashboard của Admin)
        public ActionResult Index()
        {
            // Có thể truyền thêm dữ liệu tổng quan vào View nếu cần
            // Ví dụ: số chuyến bay, số nhân viên, số vé mới...
            return View(); // View này sẽ sử dụng _AdminLayout.cshtml (do _ViewStart.cshtml trong Views/SystemManager)
        }

        // Các action CRUD mặc định có thể xóa đi nếu không dùng đến
        // hoặc triển khai logic cụ thể nếu cần (ví dụ: quản lý cấu hình hệ thống)
    }
}

// Đảm bảo có file Views/SystemManager/_ViewStart.cshtml
// với nội dung:
// @{
//     Layout = "_AdminLayout";
// }

// Và file Views/SystemManager/Index.cshtml (đã cung cấp ở lần trả lời trước)