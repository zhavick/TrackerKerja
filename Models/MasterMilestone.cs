using System.ComponentModel.DataAnnotations;

namespace TrackerKerja.Models
{
    public class MasterMilestone
    {
        public int Id { get; set; }

        [Required, MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(100)]
        public string Phase { get; set; } = string.Empty; // SDLC Waterfall Phase

        [MaxLength(7)]
        public string Color { get; set; } = "#6366F1";

        [MaxLength(50)]
        public string? Icon { get; set; } = "fa-flag";

        public int OrderIndex { get; set; } = 0;

        [MaxLength(500)]
        public string? Description { get; set; }

        public bool IsDefault { get; set; } = false;
    }
}
