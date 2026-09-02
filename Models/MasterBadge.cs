using System.ComponentModel.DataAnnotations;

namespace TrackerKerja.Models
{
    public enum BadgeRarity
    {
        Common = 1,
        Rare = 2,
        Epic = 3,
        Legendary = 4
    }

    public enum BadgeTriggerType
    {
        Manual = 0,             // Diberikan manual oleh Admin
        Auto_DoneTasks = 1,     // Berdasarkan jumlah task yang selesai
        Auto_TotalHours = 2,    // Berdasarkan total jam kerja
        Auto_NotesCount = 3,    // Berdasarkan jumlah catatan kerja yang dibuat
        Auto_TotalTasks = 4,    // Berdasarkan total task yang pernah ditugaskan
        Auto_ProfileComplete = 5// Berdasarkan kelengkapan profil (foto, jabatan, no hp)
    }

    public class MasterBadge
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public string Code { get; set; } = string.Empty; // e.g. "TASK_FIRST", "TASK_10", "WORK_50H"

        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty; // e.g. "Langkah Pertama 🐾"

        [StringLength(255)]
        public string Description { get; set; } = string.Empty; // e.g. "Menyelesaikan tugas pertama dengan sukses"

        [StringLength(50)]
        public string Category { get; set; } = "Tasks"; // "Tasks", "Timesheets", "Notes", "Special", "Milestones"

        [StringLength(100)]
        public string Icon { get; set; } = "fa-solid fa-award"; // FontAwesome icon class e.g. "fa-solid fa-trophy"

        [StringLength(50)]
        public string Color { get; set; } = "#F59E0B"; // HEX Color or Theme Accent

        public int Points { get; set; } = 100; // EXP Points reward

        public BadgeRarity Rarity { get; set; } = BadgeRarity.Common;

        public BadgeTriggerType TriggerType { get; set; } = BadgeTriggerType.Manual;

        public int TriggerThreshold { get; set; } = 1; // e.g. 1 task, 10 tasks, 50 hours

        public bool IsActive { get; set; } = true;

        public int OrderIndex { get; set; } = 0;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public virtual ICollection<UserBadge> UserBadges { get; set; } = new List<UserBadge>();
    }
}
