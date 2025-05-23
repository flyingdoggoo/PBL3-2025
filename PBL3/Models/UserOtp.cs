using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PBL3.Models
{
    public class UserOtp
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; }
        [ForeignKey("UserId")]
        public virtual AppUser User { get; set; }

        [Required]
        [MaxLength(10)]
        public string OtpCode { get; set; }

        [Required]
        public string IdentityPasswordResetToken { get; set; }

        [Required]
        public DateTime ExpiryTimestampUtc { get; set; }

        public bool IsVerified { get; set; } = false;
    }
}
