using System.ComponentModel.DataAnnotations;

namespace TrackerKerja.Models
{
    public class Category
    {
        public int Id { get; set; }

        [Required, MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(7)]
        public string Color { get; set; } = "#6366F1";

        [MaxLength(500)]
        public string? Description { get; set; }

        public ICollection<WorkTask> Tasks { get; set; } = new List<WorkTask>();
    }
}
