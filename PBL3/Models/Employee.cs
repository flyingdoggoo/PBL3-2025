using System;
using System.Collections.Generic; // Thêm nếu dùng Navigation properties
using System.ComponentModel.DataAnnotations; // Thêm nếu dùng Data Annotations khác

namespace PBL3.Models
{
    // Kế thừa AppUser, sử dụng TPH
    public class Employee : AppUser
    {
        [Display(Name = "Ngày thêm")]
        public DateTime AddedDate { get; set; } = DateTime.UtcNow; // Đặt giá trị mặc định

        // Navigation properties (Tùy chọn)
        public virtual ICollection<Ticket> HandledTickets { get; set; } = new List<Ticket>();
    }
}