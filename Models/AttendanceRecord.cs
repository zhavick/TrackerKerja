using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TrackerKerja.Models
{
    public enum AttendanceType
    {
        Present = 1,      // Hadir
        Leave = 2,        // Cuti (Tahunan, Cuti Khusus, dll)
        Sick = 3,         // Sakit
        Permission = 4,   // Izin Keperluan Pribadi
        BusinessTrip = 5, // Perjalanan Dinas / Dinas Luar
        Holiday = 6       // Libur Nasional / Cuti Bersama
    }

    public enum WorkLocationType
    {
        WFO = 1,     // Work From Office
        WFH = 2,     // Work From Home
        Dinas = 3,   // Di Luar Kantor / Lapangan
        Remote = 4   // Remote Luar Kota
    }

    public enum AttendanceApprovalStatus
    {
        Approved = 1,
        Pending = 2,
        Rejected = 3
    }

    public class AttendanceRecord
    {
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;
        public virtual AppUser? User { get; set; }

        [Required]
        public DateTime Date { get; set; }

        public AttendanceType Type { get; set; } = AttendanceType.Present;

        public WorkLocationType WorkLocation { get; set; } = WorkLocationType.WFO;

        // Jam Kedatangan (Clock-In)
        public DateTime? ClockIn { get; set; }

        // Jam Pulang (Clock-Out)
        public DateTime? ClockOut { get; set; }

        // Total durasi jam kehadiran kantor / kerja harian (dalam satuan jam, misal 8.5)
        public double TotalHours { get; set; } = 0;

        // Kategori / Alasan Spesifik (misal: "Cuti Tahunan", "Cuti Melahirkan", "Izin Acara Keluarga", "Sakit Demam")
        [MaxLength(200)]
        public string? LeaveReason { get; set; }

        [MaxLength(1000)]
        public string? Notes { get; set; }

        public AttendanceApprovalStatus Status { get; set; } = AttendanceApprovalStatus.Approved;

        public string? ApprovedByUserId { get; set; }
        public virtual AppUser? ApprovedByUser { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime UpdatedAt { get; set; } = DateTime.Now;

        // Computed Helpers
        [NotMapped]
        public bool IsClockedIn => ClockIn.HasValue;

        [NotMapped]
        public bool IsClockedOut => ClockOut.HasValue;

        [NotMapped]
        public string DurationFormatted
        {
            get
            {
                if (TotalHours <= 0) return "0j 0m";
                var totalSecs = (long)(TotalHours * 3600);
                var h = totalSecs / 3600;
                var m = (totalSecs % 3600) / 60;
                return $"{h}j {m}m";
            }
        }

        [NotMapped]
        public string TypeDisplayName
        {
            get
            {
                return Type switch
                {
                    AttendanceType.Present => $"Hadir ({WorkLocation})",
                    AttendanceType.Leave => string.IsNullOrWhiteSpace(LeaveReason) ? "Cuti" : LeaveReason,
                    AttendanceType.Sick => string.IsNullOrWhiteSpace(LeaveReason) ? "Sakit" : $"Sakit ({LeaveReason})",
                    AttendanceType.Permission => string.IsNullOrWhiteSpace(LeaveReason) ? "Izin" : $"Izin ({LeaveReason})",
                    AttendanceType.BusinessTrip => "Dinas Luar",
                    AttendanceType.Holiday => "Libur",
                    _ => "Presensi"
                };
            }
        }

        [NotMapped]
        public string TypeBadgeColor
        {
            get
            {
                return Type switch
                {
                    AttendanceType.Present when WorkLocation == WorkLocationType.WFO => "bg-emerald-100 text-emerald-800 border-emerald-200",
                    AttendanceType.Present when WorkLocation == WorkLocationType.WFH => "bg-sky-100 text-sky-800 border-sky-200",
                    AttendanceType.Present => "bg-teal-100 text-teal-800 border-teal-200",
                    AttendanceType.Leave => "bg-amber-100 text-amber-800 border-amber-200",
                    AttendanceType.Sick => "bg-rose-100 text-rose-800 border-rose-200",
                    AttendanceType.Permission => "bg-purple-100 text-purple-800 border-purple-200",
                    AttendanceType.BusinessTrip => "bg-indigo-100 text-indigo-800 border-indigo-200",
                    AttendanceType.Holiday => "bg-slate-100 text-slate-700 border-slate-200",
                    _ => "bg-slate-100 text-slate-700 border-slate-200"
                };
            }
        }

        [NotMapped]
        public string TypeIcon
        {
            get
            {
                return Type switch
                {
                    AttendanceType.Present when WorkLocation == WorkLocationType.WFO => "fa-building",
                    AttendanceType.Present when WorkLocation == WorkLocationType.WFH => "fa-house-laptop",
                    AttendanceType.Present => "fa-briefcase",
                    AttendanceType.Leave => "fa-umbrella-beach",
                    AttendanceType.Sick => "fa-user-nurse",
                    AttendanceType.Permission => "fa-file-signature",
                    AttendanceType.BusinessTrip => "fa-plane-departure",
                    AttendanceType.Holiday => "fa-calendar-day",
                    _ => "fa-user-check"
                };
            }
        }
    }
}
