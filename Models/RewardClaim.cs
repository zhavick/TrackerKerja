using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TrackerKerja.Models
{
    public enum ClaimStatus
    {
        Pending = 0,
        Approved = 1,
        Completed = 2,
        Rejected = 3
    }

    public class RewardClaim
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;

        [ForeignKey("UserId")]
        public virtual AppUser? User { get; set; }

        public int RewardItemId { get; set; }

        [ForeignKey("RewardItemId")]
        public virtual RewardItem? RewardItem { get; set; }

        public int PointsSpent { get; set; }

        public int PointValueSnapshot { get; set; } = 100;

        [Column(TypeName = "decimal(18,2)")]
        public decimal RupiahEquivalent { get; set; }

        public ClaimStatus Status { get; set; } = ClaimStatus.Pending;

        [StringLength(500)]
        public string? UserNotes { get; set; }

        [StringLength(500)]
        public string? AdminNotes { get; set; }

        public DateTime ClaimedAt { get; set; } = DateTime.Now;

        public DateTime? ProcessedAt { get; set; }

        public string? ProcessedByUserId { get; set; }

        [ForeignKey("ProcessedByUserId")]
        public virtual AppUser? ProcessedByUser { get; set; }
    }
}
