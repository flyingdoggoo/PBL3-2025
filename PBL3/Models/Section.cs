using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Collections.Generic; 

namespace PBL3.Models
{
    public class Section
    {
        [Key]
        public int SectionId { get; set; }

        [Required]
        [StringLength(50)]
        [Display(Name = "Tên khu vực")]
        public string SectionName { get; set; }

        [Required]
        [Range(1, 500)]
        [Display(Name = "Sức chứa khu vực")]
        public int Capacity { get; set; }
        [Required]
        [Range(0.1, 10.0)]
        [Column(TypeName = "decimal(5,2)")]
        [Display(Name = "Hệ số giá")]
        public decimal PriceMultiplier { get; set; } = 1.0m;

        [Required]
        public int FlightId { get; set; }

        [ForeignKey("FlightId")]
        public virtual Flight? Flight { get; set; }
        public virtual ICollection<Seat> Seats { get; set; } = new List<Seat>();
    }
}
