using System.ComponentModel.DataAnnotations;

namespace TrackerKerja.Models
{
    public class RewardItem
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(150)]
        public string Name { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Description { get; set; }

        public int PointCost { get; set; } = 100;

        public int Stock { get; set; } = 10;

        [StringLength(50)]
        public string Category { get; set; } = "Voucher"; // Voucher, Merchandise, E-Wallet, Hadiah Bulanan

        [StringLength(300)]
        public string? ImageUrl { get; set; }

        [StringLength(50)]
        public string Icon { get; set; } = "fa-solid fa-gift";

        [StringLength(20)]
        public string Color { get; set; } = "#EC4899";

        public bool IsActive { get; set; } = true;

        public bool IsMonthlyMilestoneReward { get; set; } = false; // Hadiah spesial streak 1 bulan

        public int OrderIndex { get; set; } = 0;

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public virtual ICollection<RewardClaim> Claims { get; set; } = new List<RewardClaim>();
    }
}
