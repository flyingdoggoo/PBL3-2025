using System.ComponentModel.DataAnnotations;

namespace PBL3.Models
{
    public class Employee : AppUser
    {
        //[Key]
        //public int EmployeeId { get; set; } // Đổi từ String sang int

        public DateTime AddedDate { get; set; }

        //[Range(18, 70)]
        //public int EmployeeAge { get; set; }

        //[StringLength(200)]
        //public string EmployeeAddress { get; set; }

        // Thuộc tính phân biệt (discriminator) cho kế thừa Table-Per-Hierarchy (TPH) nếu cần
        // Hoặc EF Core có thể tự động quản lý
        // public string EmployeeType { get; set; } = "Regular";

        // --- Thuộc tính điều hướng (Navigation Properties) ---

        // Một nhân viên có thể đã xử lý việc đặt nhiều vé (Mối quan hệ một-nhiều, tùy chọn)
        public virtual ICollection<Ticket> HandledTickets { get; set; } = new List<Ticket>();
    }
}
