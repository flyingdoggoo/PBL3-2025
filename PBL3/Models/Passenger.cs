using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace PBL3.Models
{
    public class Passenger : AppUser
    {
        [StringLength(50)]
        [Display(Name = "Số hộ chiếu")]
        public string? PassportNumber { get; set; }
        public virtual ICollection<Ticket> Tickets { get; set; } = new List<Ticket>();
    }
}
