using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PBL3.Models
{
    public class Section
    {
        [Key]
        public int SectionId { get; set; }

        [Required]
        [StringLength(50)]
        [Display(Name = "Tên khu vực")]
        public string SectionName { get; set; } // Ví dụ: Economy, Business

        [Range(1, 500)]
        [Display(Name = "Sức chứa khu vực")]
        public int Capacity { get; set; }

        [Required]
        public int FlightId { get; set; } // FK đến Flight

        [ForeignKey("FlightId")]
        public virtual Flight? Flight { get; set; }
    }
}