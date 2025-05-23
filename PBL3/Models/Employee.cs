using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace PBL3.Models
{
    public class Employee : AppUser
    {
        [Display(Name = "Ngày thêm")]
        public DateTime AddedDate { get; set; } = DateTime.UtcNow;
        public virtual ICollection<Ticket> HandledTickets { get; set; } = new List<Ticket>();
    }
}
