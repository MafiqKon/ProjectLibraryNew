using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProjectLibrary.Models
{
    public class UserBadge
    {
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; }
        public virtual ApplicationUser User { get; set; }

        [Required]
        public int BadgeId { get; set; }
        public virtual Badge Badge { get; set; }

        public DateTime EarnedDate { get; set; } = DateTime.Now;
    }
}