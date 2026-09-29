using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

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

        // Company / Team Multi-Tenancy
        public int? CompanyId { get; set; }
        public virtual Company? Company { get; set; }

        // Financial & Client Tracking
        [MaxLength(150)]
        public string? ClientName { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? Budget { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? ActualCost { get; set; }

        [MaxLength(450)]
        public string? ProjectManagerId { get; set; }

        [ForeignKey("ProjectManagerId")]
        public virtual AppUser? ProjectManager { get; set; }

        [MaxLength(250)]
        public string? Tags { get; set; }

        public ICollection<WorkTask> Tasks { get; set; } = new List<WorkTask>();

        // Computed
        public int TotalTasks => Tasks.Count;
        public int CompletedTasks => Tasks.Count(t => t.Status == TaskStatus.Done);
        public int InProgressTasks => Tasks.Count(t => t.Status == TaskStatus.InProgress);
        public int TodoTasks => Tasks.Count(t => t.Status == TaskStatus.Todo);
        public int OverdueTasks => Tasks.Count(t => t.DueDate < DateTime.Now && t.Status != TaskStatus.Done);
        public int ProgressPercent => Tasks.Any() ? (int)Math.Round(Tasks.Average(t => (double)t.Progress)) : 0;

        public double TotalWorkHours => Tasks.SelectMany(t => t.Sessions).Sum(s => (long?)s.Duration ?? 0) / 3600.0;

        public int BudgetBurnPercent => Budget.HasValue && Budget.Value > 0
            ? (int)Math.Min(100, Math.Round(((ActualCost ?? 0) / Budget.Value) * 100))
            : 0;

        public decimal RemainingBudget => (Budget ?? 0) - (ActualCost ?? 0);
        public bool IsOverBudget => Budget.HasValue && (ActualCost ?? 0) > Budget.Value;
    }
}
