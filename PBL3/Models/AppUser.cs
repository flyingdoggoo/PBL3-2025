namespace PBL3.Models
{
    using Microsoft.AspNetCore.Identity;
    using System.ComponentModel.DataAnnotations;

    public class AppUser : IdentityUser
    {
        [Required(ErrorMessage = "Tên hành khách là bắt buộc.")] 
        [StringLength(100, ErrorMessage = "Tên hành khách không được vượt quá 100 ký tự.")]
        public string FullName { get; set; }

        [Range(0, 120, ErrorMessage = "Tuổi phải nằm trong khoảng từ 0 đến 120.")] 
        public int Age { get; set; }

        [StringLength(200)]
        public string Address { get; set; }

        //[StringLength(50)]
        //public string Role { get; set; }
    }
}
