using System.ComponentModel.DataAnnotations;

namespace TrackerKerja.Models
{
    public class WorkSession
    {
        public int Id { get; set; }

        public int TaskId { get; set; }
        public WorkTask? Task { get; set; }

        public string? UserId { get; set; }
        public AppUser? User { get; set; }

        public DateTime StartTime { get; set; } = DateTime.Now;
        public DateTime? EndTime { get; set; }

        // Duration in seconds
        public long Duration { get; set; } = 0;
        public long DurationSeconds => Duration;

        public string DurationFormatted
        {
            get
            {
                var h = Duration / 3600;
                var m = (Duration % 3600) / 60;
                var s = Duration % 60;
                return $"{h:D2}:{m:D2}:{s:D2}";
            }
        }

        [MaxLength(1000)]
        public string? Notes { get; set; }

        public bool IsRunning => EndTime == null;
    }
}
