using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TrackerKerja.Models
{
    public class UserBadgeSnapshot
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public string SnapshotBatchId { get; set; } = string.Empty;

        [StringLength(50)]
        public string ResetMode { get; set; } = "transactional";

        [Required]
        public string UserId { get; set; } = string.Empty;

        [ForeignKey("UserId")]
        public virtual AppUser? User { get; set; }

        [Required]
        public int BadgeId { get; set; }

        [ForeignKey("BadgeId")]
        public virtual MasterBadge? Badge { get; set; }

        public DateTime OriginalUnlockedAt { get; set; } = DateTime.UtcNow;

        public bool IsFeatured { get; set; } = false;

        [StringLength(255)]
        public string? AwardedBy { get; set; }

        public DateTime SnapshotCreatedAt { get; set; } = DateTime.UtcNow;

        public bool IsRestored { get; set; } = false;

        public DateTime? RestoredAt { get; set; }

        [StringLength(255)]
        public string? RestoredBy { get; set; }
    }
}
