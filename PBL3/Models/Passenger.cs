using System.Collections.Generic; // Thêm nếu dùng Navigation properties
using System.ComponentModel.DataAnnotations;

namespace PBL3.Models
{
    // Kế thừa AppUser, sử dụng TPH
    public class Passenger : AppUser
    {
        [StringLength(50)]
        [Display(Name = "Số hộ chiếu")]
        public string? PassportNumber { get; set; } // Cho phép null

        // Navigation properties
        public virtual ICollection<Ticket> Tickets { get; set; } = new List<Ticket>();
    }
}