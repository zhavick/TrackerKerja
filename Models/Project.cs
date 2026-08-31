using System.ComponentModel.DataAnnotations;

namespace TrackerKerja.Models
{
    public enum ProjectStatus
    {
        Active,
        Completed,
        Archived
    }

    public class Project
    {
        public int Id { get; set; }

        [Required, MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(1000)]
        public string? Description { get; set; }

        [MaxLength(7)]
        public string Color { get; set; } = "#6366F1";

        public DateTime? Deadline { get; set; }

        public ProjectStatus Status { get; set; } = ProjectStatus.Active;

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public ICollection<WorkTask> Tasks { get; set; } = new List<WorkTask>();

        // Computed
        public int TotalTasks => Tasks.Count;
        public int CompletedTasks => Tasks.Count(t => t.Status == TaskStatus.Done);
        public int ProgressPercent => Tasks.Any() ? (int)Math.Round(Tasks.Average(t => (double)t.Progress)) : 0;
    }
}
