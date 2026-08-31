using TrackerKerja.Models;

namespace TrackerKerja.ViewModels
{
    public class TimesheetViewModel
    {
        public DateTime WeekStart { get; set; }
        public DateTime WeekEnd { get; set; }
        public int? SelectedProjectId { get; set; }
        public string? SelectedMemberId { get; set; }

        public List<Project> AllProjects { get; set; } = new();
        public List<AppUser> AllMembers { get; set; } = new();
        public List<WorkTask> UserTasks { get; set; } = new();

        public double TotalWeekHours { get; set; }
        public int TotalSessionsCount { get; set; }
        public int ActiveTasksCount { get; set; }

        // Daily totals for the 7 days (Monday to Sunday)
        public double[] DayTotals { get; set; } = new double[7];
        public DateTime[] DayDates { get; set; } = new DateTime[7];
        public string[] DayNames { get; set; } = new[] { "Senin", "Selasa", "Rabu", "Kamis", "Jumat", "Sabtu", "Minggu" };

        // Matrix rows per task
        public List<TimesheetTaskRowDto> TaskRows { get; set; } = new();

        // Detailed session logs
        public List<WorkSession> RecentSessions { get; set; } = new();
    }

    public class TimesheetTaskRowDto
    {
        public int TaskId { get; set; }
        public string TaskTitle { get; set; } = string.Empty;
        public string? ProjectName { get; set; }
        public string? CategoryName { get; set; }
        public string? AssigneeName { get; set; }
        public string? AssigneeAvatar { get; set; }
        public string Status { get; set; } = string.Empty;
        public int Progress { get; set; }

        public double[] DailyHours { get; set; } = new double[7];
        public double TotalHours => DailyHours.Sum();
        public string TotalHoursFormatted
        {
            get
            {
                var totalSeconds = (long)(TotalHours * 3600);
                var h = totalSeconds / 3600;
                var m = (totalSeconds % 3600) / 60;
                return $"{h}j {m}m";
            }
        }
    }
}
