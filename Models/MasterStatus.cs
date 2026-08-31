using System.ComponentModel.DataAnnotations;

namespace TrackerKerja.Models
{
    public class MasterStatus
    {
        public int Id { get; set; }

        [Required, MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(7)]
        public string Color { get; set; } = "#06B6D4";

        public bool IsDoneState { get; set; } = false;

        public int OrderIndex { get; set; } = 0;

        [MaxLength(500)]
        public string? Description { get; set; }

        public bool IsDefault { get; set; } = false;
    }
}
