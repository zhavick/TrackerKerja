using System.ComponentModel.DataAnnotations;

namespace TrackerKerja.Models
{
    public class SystemSetting
    {
        [Key]
        [MaxLength(100)]
        public string Key { get; set; } = string.Empty;

        public string Value { get; set; } = string.Empty;

        [MaxLength(250)]
        public string? Description { get; set; }

        public DateTime UpdatedAt { get; set; } = DateTime.Now;
    }
}
