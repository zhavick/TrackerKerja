using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TrackerKerja.Models
{
    public class DailyCheckIn
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;

        [ForeignKey("UserId")]
        public virtual AppUser? User { get; set; }

        public DateTime CheckInDate { get; set; } // YYYY-MM-DD local date

        public DateTime CheckInTime { get; set; } = DateTime.Now;

        public int PointsEarned { get; set; }

        public int StreakDay { get; set; } = 1;

        public bool IsMonthlyMilestone { get; set; } = false;

        [StringLength(255)]
        public string? Notes { get; set; }
    }
}
