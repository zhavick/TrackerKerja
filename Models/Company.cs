using System.ComponentModel.DataAnnotations;

namespace TrackerKerja.Models
{
    public class Company
    {
        public int Id { get; set; }

        [Required, MaxLength(150)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(50)]
        public string? Code { get; set; }

        [MaxLength(500)]
        public string? Description { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Navigation Collections
        public virtual ICollection<AppUser> Members { get; set; } = new List<AppUser>();
        public virtual ICollection<Project> Projects { get; set; } = new List<Project>();
        public virtual ICollection<WorkTask> Tasks { get; set; } = new List<WorkTask>();
        public virtual ICollection<WorkNote> Notes { get; set; } = new List<WorkNote>();
    }
}
