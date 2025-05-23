
using System.ComponentModel.DataAnnotations;
using PBL3.Models;

namespace PBL3.Models.ViewModels
{
    public class EmployeeIndexViewModel
    {
        public IEnumerable<AppUser> Employees { get; set; } = new List<AppUser>();
        public string? SearchString { get; set; }
        public string? SortField { get; set; } = "FullName";
        public string? SortOrder { get; set; } = "asc";
        public Dictionary<string, string> SortFields { get; } = new()
        {
            { "FullName", "Name" },
            { "Email", "Email" },
            { "Age", "Age" },
            { "AddedDate", "Added Date" }
        };
        public Dictionary<string, string> SortOrders { get; } = new()
        {
            { "asc", "Ascending" },
            { "desc", "Descending" }
        };
    }
    public class AddEmployeeViewModel
    {
        [Required]
        [EmailAddress]
        [Display(Name = "Email")]
        public string Email { get; set; }

        [Required]
        [StringLength(100, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        public string Password { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "Confirm password")]
        [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
        public string ConfirmPassword { get; set; }

        [Required(ErrorMessage = "Full name is required.")]
        [StringLength(100, ErrorMessage = "Full name cannot exceed 100 characters.")]
        [Display(Name = "Full Name")]
        public string FullName { get; set; }

        [Range(18, 120, ErrorMessage = "Age must be between 18 and 120.")]
        public int Age { get; set; }

        [StringLength(200)]
        public string? Address { get; set; }
    }
    public class PromoteEmployeeViewModel
    {
        [Required]
        public string UserId { get; set; }

        [Required]
        [DataType(DataType.Password)]
        [Display(Name = "Your Admin Password")]
        public string AdminPassword { get; set; }
    }
}