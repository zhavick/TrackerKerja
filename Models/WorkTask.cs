using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TrackerKerja.Models
{
    public enum TaskPriority { Low, Medium, High, Critical }
    public enum TaskStatus { Todo, InProgress, Done, Overdue }

    public class WorkTask
    {
        public int Id { get; set; }

        [Required, MaxLength(300)]
        public string Title { get; set; } = string.Empty;

        [MaxLength(2000)]
        public string? Description { get; set; }

        [MaxLength(4000)]
        public string? Obstacle { get; set; }

        [MaxLength(4000)]
        public string? Solution { get; set; }

        public int? ProjectId { get; set; }
        public Project? Project { get; set; }

        public int? CategoryId { get; set; }
        public Category? Category { get; set; }

        public string? AssignedToUserId { get; set; }
        public AppUser? AssignedToUser { get; set; }

        public TaskPriority Priority { get; set; } = TaskPriority.Medium;
        public TaskStatus Status { get; set; } = TaskStatus.Todo;

        [Range(0, 100)]
        public int Progress { get; set; } = 0;

        public DateTime? StartDate { get; set; }
        public DateTime? DueDate { get; set; }

        // Tags stored as JSON string
        public string? Tags { get; set; }

        // SDLC Waterfall Milestone Phase (e.g. Requirement Analysis, System Design, Implementation, Testing & QA, Deployment, Maintenance)
        [MaxLength(100)]
        public string? Milestone { get; set; } = "Implementation";

        // Parent / Child Task Relationship
        public int? ParentTaskId { get; set; }
        [ForeignKey("ParentTaskId")]
        public WorkTask? ParentTask { get; set; }
        public ICollection<WorkTask> ChildTasks { get; set; } = new List<WorkTask>();

        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime UpdatedAt { get; set; } = DateTime.Now;

        public ICollection<WorkSession> Sessions { get; set; } = new List<WorkSession>();
        public ICollection<WorkNote> Notes { get; set; } = new List<WorkNote>();

        [NotMapped]
        public string TaskCode => $"TSK-{Id:D4}";

        [NotMapped]
        public string ParentCode => ParentTask != null ? $"TSK-{ParentTask.Id:D4}" : TaskCode;

        [NotMapped]
        public bool IsParent => ChildTasks != null && ChildTasks.Any();

        [NotMapped]
        public bool HasParent => ParentTaskId.HasValue;

        [NotMapped]
        public bool AreAllChildTasksDone => ChildTasks == null || !ChildTasks.Any() || ChildTasks.All(c => c.Status == TaskStatus.Done);

        [NotMapped]
        public long TotalDurationSeconds => Sessions.Sum(s => s.Duration);

        [NotMapped]
        public string TotalDurationFormatted
        {
            get
            {
                var totalSeconds = TotalDurationSeconds;
                var hours = totalSeconds / 3600;
                var minutes = (totalSeconds % 3600) / 60;
                var seconds = totalSeconds % 60;
                return $"{hours:D2}:{minutes:D2}:{seconds:D2}";
            }
        }
    }
}
