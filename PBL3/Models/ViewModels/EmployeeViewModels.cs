// ViewModels/EmployeeViewModels.cs
using System.ComponentModel.DataAnnotations;
using PBL3.Models; // Adjust namespace if needed

namespace PBL3.Models.ViewModels // Adjust namespace if needed
{
    // ViewModel for the Index page list and filtering/sorting
    public class EmployeeIndexViewModel
    {
        public IEnumerable<AppUser> Employees { get; set; } = new List<AppUser>();
        public string? SearchString { get; set; }
        public string? SortField { get; set; } = "FullName"; // Default sort
        public string? SortOrder { get; set; } = "asc"; // Default order
        public Dictionary<string, string> SortFields { get; } = new()
        {
            { "FullName", "Name" },
            { "Email", "Email" },
            { "Age", "Age" },
            { "AddedDate", "Added Date" } // Assuming Employee has AddedDate
        };
        public Dictionary<string, string> SortOrders { get; } = new()
        {
            { "asc", "Ascending" },
            { "desc", "Descending" }
        };

        // Optional: Add properties for pagination if needed
        // public int CurrentPage { get; set; }
        // public int TotalPages { get; set; }
    }

    // ViewModel for Adding a new Employee
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

        [Range(18, 120, ErrorMessage = "Age must be between 18 and 120.")] // Assuming minimum age 18 for employee
        public int Age { get; set; }

        [StringLength(200)]
        public string? Address { get; set; }
    }

    // ViewModel for Promotion Confirmation
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