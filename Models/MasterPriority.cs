using System.ComponentModel.DataAnnotations;

namespace TrackerKerja.Models
{
    public class MasterPriority
    {
        public int Id { get; set; }

        [Required, MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(7)]
        public string Color { get; set; } = "#F59E0B";

        [MaxLength(50)]
        public string Icon { get; set; } = "fa-flag";

        public int OrderIndex { get; set; } = 0;

        [MaxLength(500)]
        public string? Description { get; set; }

        public bool IsDefault { get; set; } = false;
    }
}
