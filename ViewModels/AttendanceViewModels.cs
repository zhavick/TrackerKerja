using System.ComponentModel.DataAnnotations;
using TrackerKerja.Models;

namespace TrackerKerja.ViewModels
{
    public class AttendanceIndexViewModel
    {
        public int Month { get; set; } = DateTime.Today.Month;
        public int Year { get; set; } = DateTime.Today.Year;
        public string? SelectedMemberId { get; set; }
        public string? SelectedType { get; set; }
        public bool IsAdmin { get; set; }
        public AppUser? CurrentUser { get; set; }

        public List<AppUser> Members { get; set; } = new();
        public List<AttendanceRecord> Attendances { get; set; } = new();

        // Status Kehadiran Hari Ini untuk User Login
        public AttendanceRecord? TodayRecord { get; set; }

        // Metrik Ringkasan Bulanan
        public int TotalPresentDays { get; set; }
        public int TotalWfoDays { get; set; }
        public int TotalWfhDays { get; set; }
        public int TotalLeaveDays { get; set; }
        public int TotalSickDays { get; set; }
        public int TotalPermissionDays { get; set; }
        public double AverageDailyHours { get; set; }
        public double TotalMonthHours { get; set; }
    }

    public class ManualAttendanceInputModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Pengguna wajib dipilih.")]
        public string UserId { get; set; } = string.Empty;

        [Required(ErrorMessage = "Tanggal wajib diisi.")]
        public DateTime Date { get; set; } = DateTime.Today;

        public AttendanceType Type { get; set; } = AttendanceType.Present;

        public WorkLocationType WorkLocation { get; set; } = WorkLocationType.WFO;

        // Waktu format "HH:mm" (e.g. "08:30")
        public string? ClockInTime { get; set; }
        public string? ClockOutTime { get; set; }

        public string? LeaveReason { get; set; }
        public string? Notes { get; set; }
    }

    public class LeaveRequestModel
    {
        public string? UserId { get; set; }

        [Required(ErrorMessage = "Tanggal mulai wajib diisi.")]
        public DateTime StartDate { get; set; } = DateTime.Today;

        [Required(ErrorMessage = "Tanggal selesai wajib diisi.")]
        public DateTime EndDate { get; set; } = DateTime.Today;

        public AttendanceType Type { get; set; } = AttendanceType.Leave;

        [Required(ErrorMessage = "Alasan / jenis cuti wajib diisi.")]
        [MaxLength(200)]
        public string LeaveReason { get; set; } = "Cuti Tahunan";

        [MaxLength(1000)]
        public string? Notes { get; set; }
    }

    public class AttendanceDailySummaryDto
    {
        public DateTime Date { get; set; }
        public bool HasRecord { get; set; }
        public int? RecordId { get; set; }
        public AttendanceType Type { get; set; }
        public string TypeDisplayName { get; set; } = string.Empty;
        public WorkLocationType WorkLocation { get; set; }
        public DateTime? ClockIn { get; set; }
        public DateTime? ClockOut { get; set; }
        public double TotalHours { get; set; }
        public string? LeaveReason { get; set; }
        public string? Notes { get; set; }
        public string BadgeColorClass { get; set; } = "bg-slate-100 text-slate-700 border-slate-200";
        public string IconClass { get; set; } = "fa-user-check";

        public string ClockInText => ClockIn.HasValue ? ClockIn.Value.ToString("HH:mm") : "—";
        public string ClockOutText => ClockOut.HasValue ? ClockOut.Value.ToString("HH:mm") : (ClockIn.HasValue ? "Aktif" : "—");

        public string ShortSummary
        {
            get
            {
                if (!HasRecord) return "Belum Presensi";
                if (Type == AttendanceType.Present)
                {
                    var timeRange = ClockIn.HasValue ? $"{ClockIn.Value:HH:mm} - {(ClockOut.HasValue ? ClockOut.Value.ToString("HH:mm") : "...")}" : "Hadir";
                    return $"{WorkLocation} ({timeRange})";
                }
                return string.IsNullOrWhiteSpace(LeaveReason) ? TypeDisplayName : LeaveReason;
            }
        }
    }
}
