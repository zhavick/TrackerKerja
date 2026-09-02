using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TrackerKerja.Models
{
    public class UserBadge
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;

        [ForeignKey("UserId")]
        public virtual AppUser? User { get; set; }

        [Required]
        public int BadgeId { get; set; }

        [ForeignKey("BadgeId")]
        public virtual MasterBadge? Badge { get; set; }

        public DateTime UnlockedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Apakah badge ini disematkan (pin / featured) pada profil pengguna.
        /// </summary>
        public bool IsFeatured { get; set; } = false;

        /// <summary>
        /// Catatan tambahan jika diberikan manual oleh admin.
        /// </summary>
        [StringLength(255)]
        public string? AwardedBy { get; set; }
    }
}
