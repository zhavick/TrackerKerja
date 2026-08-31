using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using TrackerKerja.Models;
using ModelTaskStatus = TrackerKerja.Models.TaskStatus;

namespace TrackerKerja.ViewModels
{
    #region Project DTOs
    public class ProjectResponseDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string Color { get; set; } = "#6366F1";
        public DateTime? Deadline { get; set; }
        public string Status { get; set; } = "Active";
        public int TotalTasks { get; set; }
        public int CompletedTasks { get; set; }
        public int ProgressPercent { get; set; }
        public long TotalWorkSeconds { get; set; }
        public string TotalWorkFormatted { get; set; } = "00:00:00";
        public DateTime CreatedAt { get; set; }
    }

    public class CreateProjectRequestDto
    {
        [Required(ErrorMessage = "Nama proyek wajib diisi.")]
        [MaxLength(200, ErrorMessage = "Nama proyek maksimal 200 karakter.")]
        public string Name { get; set; } = string.Empty;

        [MaxLength(1000, ErrorMessage = "Deskripsi maksimal 1000 karakter.")]
        public string? Description { get; set; }

        [MaxLength(7)]
        public string Color { get; set; } = "#6366F1";

        public DateTime? Deadline { get; set; }
        public ProjectStatus Status { get; set; } = ProjectStatus.Active;
    }

    public class UpdateProjectRequestDto
    {
        [Required(ErrorMessage = "Nama proyek wajib diisi.")]
        [MaxLength(200, ErrorMessage = "Nama proyek maksimal 200 karakter.")]
        public string Name { get; set; } = string.Empty;

        [MaxLength(1000, ErrorMessage = "Deskripsi maksimal 1000 karakter.")]
        public string? Description { get; set; }

        [MaxLength(7)]
        public string Color { get; set; } = "#6366F1";

        public DateTime? Deadline { get; set; }
        public ProjectStatus Status { get; set; } = ProjectStatus.Active;
    }
    #endregion

    #region Note DTOs
    public class NoteResponseDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string ContentHtml { get; set; } = string.Empty;
        public string PlainTextPreview { get; set; } = string.Empty;
        public string Category { get; set; } = "General";
        public string Color { get; set; } = "#6366F1";
        public bool IsPinned { get; set; }
        public bool IsStandalone { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        public string? AuthorUserId { get; set; }
        public UserShortDto? AuthorUser { get; set; }

        public int? TaskId { get; set; }
        public string? TaskTitle { get; set; }
        public string? TaskCode { get; set; }

        public int AttachmentsCount { get; set; }
        public List<NoteAttachmentDto> Attachments { get; set; } = new();
    }

    public class NoteAttachmentDto
    {
        public int Id { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public long FileSize { get; set; }
        public string? ContentType { get; set; }
        public string? FileExtension { get; set; }
        public DateTime UploadedAt { get; set; }
    }

    public class CreateNoteRequestDto
    {
        [Required(ErrorMessage = "Judul catatan wajib diisi.")]
        [MaxLength(200, ErrorMessage = "Judul maksimal 200 karakter.")]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "Konten catatan wajib diisi.")]
        public string ContentHtml { get; set; } = string.Empty;

        [MaxLength(50)]
        public string Category { get; set; } = "General";

        [MaxLength(7)]
        public string Color { get; set; } = "#6366F1";

        public bool IsPinned { get; set; } = false;
        public int? TaskId { get; set; }
        public string? AuthorUserId { get; set; }
    }

    public class UpdateNoteRequestDto
    {
        [Required(ErrorMessage = "Judul catatan wajib diisi.")]
        [MaxLength(200, ErrorMessage = "Judul maksimal 200 karakter.")]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "Konten catatan wajib diisi.")]
        public string ContentHtml { get; set; } = string.Empty;

        [MaxLength(50)]
        public string Category { get; set; } = "General";

        [MaxLength(7)]
        public string Color { get; set; } = "#6366F1";

        public bool IsPinned { get; set; } = false;
        public int? TaskId { get; set; }
    }
    #endregion

    #region Timesheet DTOs
    public class TimesheetResponseDto
    {
        public int Id { get; set; }
        public int TaskId { get; set; }
        public string TaskTitle { get; set; } = string.Empty;
        public string TaskCode { get; set; } = string.Empty;
        public int? ProjectId { get; set; }
        public string? ProjectName { get; set; }
        public string? ProjectColor { get; set; }
        public string? AssigneeName { get; set; }

        public DateTime StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public long DurationSeconds { get; set; }
        public string DurationFormatted { get; set; } = "00:00:00";
        public string? Notes { get; set; }
        public bool IsRunning { get; set; }
    }

    public class CreateTimesheetRequestDto
    {
        [Required(ErrorMessage = "Task ID wajib diisi.")]
        public int TaskId { get; set; }

        [Required(ErrorMessage = "Waktu mulai (StartTime) wajib diisi.")]
        public DateTime StartTime { get; set; }

        public DateTime? EndTime { get; set; }

        [Range(0, long.MaxValue, ErrorMessage = "Durasi dalam detik harus >= 0.")]
        public long Duration { get; set; }

        [MaxLength(1000)]
        public string? Notes { get; set; }
    }

    public class UpdateTimesheetRequestDto
    {
        [Required(ErrorMessage = "Waktu mulai (StartTime) wajib diisi.")]
        public DateTime StartTime { get; set; }

        public DateTime? EndTime { get; set; }

        [Range(0, long.MaxValue, ErrorMessage = "Durasi dalam detik harus >= 0.")]
        public long Duration { get; set; }

        [MaxLength(1000)]
        public string? Notes { get; set; }
    }

    public class StartTimerRequestDto
    {
        [Required(ErrorMessage = "Task ID wajib diisi.")]
        public int TaskId { get; set; }
    }

    public class StopTimerRequestDto
    {
        public int? SessionId { get; set; }
        public int? TaskId { get; set; }
        public string? Notes { get; set; }
    }

    public class TimesheetSummaryDto
    {
        public long TodaySeconds { get; set; }
        public string TodayFormatted { get; set; } = "0j 0m";
        public long WeekSeconds { get; set; }
        public string WeekFormatted { get; set; } = "0j 0m";
        public long MonthSeconds { get; set; }
        public string MonthFormatted { get; set; } = "0j 0m";
        public int ActiveRunningTimers { get; set; }
    }
    public class ActiveTimerItemDto
    {
        public int SessionId { get; set; }
        public int TaskId { get; set; }
        public string TaskTitle { get; set; } = string.Empty;
        public string TaskCode { get; set; } = string.Empty;
        public string? ProjectName { get; set; }
        public string? CategoryName { get; set; }
        public string? Priority { get; set; }
        public DateTime StartTime { get; set; }
        public long ElapsedSeconds { get; set; }
        public string ElapsedFormatted { get; set; } = "00:00:00";
    }

    public class ActiveTimersResponseDto
    {
        public int TotalActiveTimers { get; set; }
        public List<ActiveTimerItemDto> ActiveTimers { get; set; } = new();
    }
    #endregion

    #region Member DTOs
    public class MemberResponseDto
    {
        public string Id { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string JobTitle { get; set; } = string.Empty;
        public string AvatarColor { get; set; } = "#6366F1";
        public string? ProfilePictureUrl { get; set; }
        public string Role { get; set; } = "User";
        public string Initials { get; set; } = "?";
        public DateTime CreatedAt { get; set; }

        public int TotalTasks { get; set; }
        public int ActiveTasks { get; set; }
        public int DoneTasks { get; set; }
        public double TotalHoursWorked { get; set; }
        public int NotesContributedCount { get; set; }
    }

    public class CreateMemberRequestDto
    {
        [Required(ErrorMessage = "Nama lengkap wajib diisi.")]
        [MaxLength(150)]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email wajib diisi.")]
        [EmailAddress(ErrorMessage = "Format email tidak valid.")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Jabatan (Job Title) wajib diisi.")]
        [MaxLength(100)]
        public string JobTitle { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password wajib diisi.")]
        [MinLength(6, ErrorMessage = "Password minimal 6 karakter.")]
        public string Password { get; set; } = string.Empty;

        public string Role { get; set; } = "User";

        [MaxLength(7)]
        public string AvatarColor { get; set; } = "#6366F1";
    }

    public class UpdateMemberRequestDto
    {
        [Required(ErrorMessage = "Nama lengkap wajib diisi.")]
        [MaxLength(150)]
        public string FullName { get; set; } = string.Empty;

        [MaxLength(100)]
        public string JobTitle { get; set; } = string.Empty;

        [MaxLength(7)]
        public string AvatarColor { get; set; } = "#6366F1";

        public string? Role { get; set; }
    }

    public class AdminResetMemberPasswordDto
    {
        [Required(ErrorMessage = "Password baru wajib diisi.")]
        [MinLength(6, ErrorMessage = "Password baru minimal 6 karakter.")]
        public string NewPassword { get; set; } = string.Empty;
    }
    #endregion

    #region Report DTOs
    public class ReportDashboardDto
    {
        public int TotalTasks { get; set; }
        public int DoneTasks { get; set; }
        public int InProgressTasks { get; set; }
        public int TodoTasks { get; set; }
        public int OverdueTasks { get; set; }
        public double CompletionRatePercent { get; set; }

        public int TotalProjects { get; set; }
        public int ActiveProjects { get; set; }

        public double TotalHoursTracked { get; set; }
        public double TodayHoursTracked { get; set; }

        public List<string> ChartLabels { get; set; } = new();
        public List<double> ChartHours { get; set; } = new();

        public List<ProjectProgressReportDto> ProjectsSummary { get; set; } = new();
    }

    public class ProjectProgressReportDto
    {
        public int ProjectId { get; set; }
        public string ProjectName { get; set; } = string.Empty;
        public string Color { get; set; } = "#6366F1";
        public int TotalTasks { get; set; }
        public int CompletedTasks { get; set; }
        public int ProgressPercent { get; set; }
        public double TotalHours { get; set; }
    }

    public class MemberWorkloadReportDto
    {
        public string MemberId { get; set; } = string.Empty;
        public string MemberName { get; set; } = string.Empty;
        public string JobTitle { get; set; } = string.Empty;
        public int TodoTasks { get; set; }
        public int InProgressTasks { get; set; }
        public int DoneTasks { get; set; }
        public int TotalTasks { get; set; }
        public double TotalHours { get; set; }
    }

    public class GanttTaskDto
    {
        public int Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Start { get; set; } = string.Empty;
        public string End { get; set; } = string.Empty;
        public int Progress { get; set; }
        public string Status { get; set; } = "Todo";
        public string Priority { get; set; } = "Medium";
        public int? ProjectId { get; set; }
        public string ProjectName { get; set; } = "Tanpa Proyek";
        public string ProjectColor { get; set; } = "#6366F1";
        public string? AssigneeId { get; set; }
        public string AssigneeName { get; set; } = "Belum Ditugaskan";
        public string AssigneeAvatarColor { get; set; } = "#6366F1";
        public string Dependencies { get; set; } = string.Empty;
        public string CustomClass { get; set; } = "gantt-status-todo";
        public bool IsParent { get; set; }
        public int? ParentTaskId { get; set; }
        public string? ParentCode { get; set; }
        public string? Obstacle { get; set; }
        public string? Solution { get; set; }
        public string Milestone { get; set; } = "Implementation";
        public string DurationFormatted { get; set; } = "00:00:00";
    }

    public class GanttReportResponseDto
    {
        public List<GanttTaskDto> Tasks { get; set; } = new();
        public int TotalTasks { get; set; }
        public int CompletedTasks { get; set; }
        public int InProgressTasks { get; set; }
        public int OverdueTasks { get; set; }
        public string MinDate { get; set; } = string.Empty;
        public string MaxDate { get; set; } = string.Empty;
    }
    #endregion

    #region Audit Trail DTOs
    public class AuditLogResponseDto
    {
        public int Id { get; set; }
        public string? UserId { get; set; }
        public string? UserEmail { get; set; }
        public string? UserName { get; set; }
        public string ControllerName { get; set; } = string.Empty;
        public string ActionName { get; set; } = string.Empty;
        public string HttpMethod { get; set; } = string.Empty;
        public string Path { get; set; } = string.Empty;
        public string? QueryString { get; set; }
        public string? IpAddress { get; set; }
        public int StatusCode { get; set; }
        public long DurationMs { get; set; }
        public DateTime Timestamp { get; set; }
        public string? Details { get; set; }
    }

    public class AuditStatsDto
    {
        public int TotalLogs { get; set; }
        public int TotalToday { get; set; }
        public double AverageDurationMs { get; set; }
        public Dictionary<string, int> TopControllers { get; set; } = new();
        public Dictionary<string, int> HttpMethodsCount { get; set; } = new();
    }
    #endregion

    #region Configuration & Maintenance DTOs
    public class SystemSettingResponseDto
    {
        public string GlobalBaseUrl { get; set; } = "http://localhost:5000";
        public string? Description { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public class UpdateBaseUrlRequestDto
    {
        [Required(ErrorMessage = "Global Base URL wajib diisi.")]
        [Url(ErrorMessage = "Format URL tidak valid. Contoh: http://localhost:5000 atau https://tracker.domain.com")]
        public string BaseUrl { get; set; } = string.Empty;
    }

    public class DatabaseCapacityInfoDto
    {
        public string DatabaseFileName { get; set; } = "trackerkerja.db";
        public string DatabaseFilePath { get; set; } = string.Empty;
        public long FileSizeBytes { get; set; }
        public string FileSizeFormatted { get; set; } = "0 KB";
        public DateTime LastModified { get; set; }
        public int PageSize { get; set; }
        public int PageCount { get; set; }
        public int FreelistCount { get; set; }
        public long ReclaimableBytes { get; set; }
        public string ReclaimableFormatted { get; set; } = "0 KB";
        public string JournalMode { get; set; } = "DELETE";
        public Dictionary<string, int> TableStats { get; set; } = new();
        public long AttachmentsSizeBytes { get; set; }
        public string AttachmentsSizeFormatted { get; set; } = "0 KB";
    }

    public class ShrinkDatabaseResponseDto
    {
        public long InitialSizeBytes { get; set; }
        public string InitialSizeFormatted { get; set; } = "0 KB";
        public long FinalSizeBytes { get; set; }
        public string FinalSizeFormatted { get; set; } = "0 KB";
        public long ReclaimedBytes { get; set; }
        public string ReclaimedFormatted { get; set; } = "0 KB";
        public double ReclaimedPercent { get; set; }
        public long ExecutionDurationMs { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.Now;
    }

    public class ResetDatabaseRequestDto
    {
        [Required(ErrorMessage = "Mode reset wajib dipilih ('transactional' atau 'factory').")]
        public string Mode { get; set; } = "transactional";

        [Required(ErrorMessage = "Kode konfirmasi wajib diisi.")]
        public string ConfirmationCode { get; set; } = string.Empty;
    }

    public class ResetDatabaseResponseDto
    {
        public string Mode { get; set; } = string.Empty;
        public List<string> ClearedTables { get; set; } = new();
        public Dictionary<string, int> DeletedCounts { get; set; } = new();
        public string FinalDatabaseSizeFormatted { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
    }

    public class ApiDocSummaryDto
    {
        public string Title { get; set; } = "Work Tracker Pro REST API";
        public string Version { get; set; } = "v1";
        public string SwaggerUiUrl { get; set; } = "/swagger";
        public string OpenApiJsonUrl { get; set; } = "/swagger/v1/swagger.json";
        public string GlobalBaseUrl { get; set; } = "http://localhost:5000";
        public int TotalEndpoints { get; set; }
        public Dictionary<string, List<string>> ModuleEndpoints { get; set; } = new();
    }
    #endregion

    #region Auth & Account DTOs
    public class LoginRequestDto
    {
        [Required(ErrorMessage = "Email wajib diisi.")]
        [EmailAddress(ErrorMessage = "Format email tidak valid.")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password wajib diisi.")]
        public string Password { get; set; } = string.Empty;

        public bool RememberMe { get; set; } = false;
    }

    public class LoginResponseDto
    {
        public bool IsSuccess { get; set; }
        public string UserId { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string JobTitle { get; set; } = string.Empty;
        public string Role { get; set; } = "User";
        public string AvatarColor { get; set; } = "#6366F1";
        public string? ProfilePictureUrl { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    public class UserProfileDto
    {
        public string Id { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string JobTitle { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        public string Role { get; set; } = "User";
        public string AvatarColor { get; set; } = "#6366F1";
        public string? ProfilePictureUrl { get; set; }
        public DateTime CreatedAt { get; set; }
        public int TotalAssignedTasks { get; set; }
        public int CompletedTasks { get; set; }
        public double TotalHoursLogged { get; set; }
    }

    public class ChangePasswordRequestDto
    {
        [Required(ErrorMessage = "Password lama wajib diisi.")]
        public string CurrentPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password baru wajib diisi.")]
        [MinLength(6, ErrorMessage = "Password baru minimal 6 karakter.")]
        public string NewPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "Konfirmasi password baru wajib diisi.")]
        [Compare("NewPassword", ErrorMessage = "Password baru dan konfirmasi password tidak cocok.")]
        public string ConfirmNewPassword { get; set; } = string.Empty;
    }

    public class UpdateProfileRequestDto
    {
        [Required(ErrorMessage = "Nama lengkap wajib diisi.")]
        [MaxLength(150)]
        public string FullName { get; set; } = string.Empty;

        [MaxLength(100)]
        public string? JobTitle { get; set; }

        [MaxLength(30)]
        public string? PhoneNumber { get; set; }

        [MaxLength(7)]
        public string? AvatarColor { get; set; }

        [MaxLength(500)]
        public string? ProfilePictureUrl { get; set; }
    }
    #endregion

    #region Master Data DTOs
    public class MasterCategoryResponseDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Color { get; set; } = "#6366F1";
        public string? Description { get; set; }
        public int TasksCount { get; set; }
    }

    public class CreateMasterCategoryRequestDto
    {
        [Required(ErrorMessage = "Nama kategori wajib diisi.")]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(7)]
        public string Color { get; set; } = "#6366F1";

        [MaxLength(500)]
        public string? Description { get; set; }
    }

    public class UpdateMasterCategoryRequestDto
    {
        [Required(ErrorMessage = "Nama kategori wajib diisi.")]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(7)]
        public string Color { get; set; } = "#6366F1";

        [MaxLength(500)]
        public string? Description { get; set; }
    }

    public class MasterPriorityResponseDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Color { get; set; }
        public string? Icon { get; set; }
        public int OrderIndex { get; set; }
        public string? Description { get; set; }
        public bool IsDefault { get; set; }
        public int TasksCount { get; set; }
    }

    public class CreateMasterPriorityRequestDto
    {
        [Required(ErrorMessage = "Nama prioritas wajib diisi.")]
        [MaxLength(50)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(7)]
        public string? Color { get; set; } = "#F59E0B";

        [MaxLength(50)]
        public string? Icon { get; set; } = "fa-flag";

        public int OrderIndex { get; set; } = 0;

        [MaxLength(500)]
        public string? Description { get; set; }

        public bool IsDefault { get; set; } = false;
    }

    public class UpdateMasterPriorityRequestDto
    {
        [Required(ErrorMessage = "Nama prioritas wajib diisi.")]
        [MaxLength(50)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(7)]
        public string? Color { get; set; }

        [MaxLength(50)]
        public string? Icon { get; set; }

        public int OrderIndex { get; set; }

        [MaxLength(500)]
        public string? Description { get; set; }

        public bool IsDefault { get; set; }
    }

    public class MasterStatusResponseDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Color { get; set; }
        public bool IsDoneState { get; set; }
        public int OrderIndex { get; set; }
        public string? Description { get; set; }
        public bool IsDefault { get; set; }
        public int TasksCount { get; set; }
    }

    public class CreateMasterStatusRequestDto
    {
        [Required(ErrorMessage = "Nama status wajib diisi.")]
        [MaxLength(50)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(7)]
        public string? Color { get; set; } = "#6366F1";

        public bool IsDoneState { get; set; } = false;
        public int OrderIndex { get; set; } = 0;

        [MaxLength(500)]
        public string? Description { get; set; }

        public bool IsDefault { get; set; } = false;
    }

    public class UpdateMasterStatusRequestDto
    {
        [Required(ErrorMessage = "Nama status wajib diisi.")]
        [MaxLength(50)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(7)]
        public string? Color { get; set; }

        public bool IsDoneState { get; set; }
        public int OrderIndex { get; set; }

        [MaxLength(500)]
        public string? Description { get; set; }

        public bool IsDefault { get; set; }
    }

    public class MasterMilestoneResponseDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Phase { get; set; } = string.Empty;
        public string? Color { get; set; }
        public string? Icon { get; set; }
        public int OrderIndex { get; set; }
        public string? Description { get; set; }
        public bool IsDefault { get; set; }
        public int TasksCount { get; set; }
    }

    public class CreateMasterMilestoneRequestDto
    {
        [Required(ErrorMessage = "Nama milestone SDLC wajib diisi.")]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(100)]
        public string Phase { get; set; } = "Implementation";

        [MaxLength(7)]
        public string? Color { get; set; } = "#6366F1";

        [MaxLength(50)]
        public string? Icon { get; set; } = "fa-code";

        public int OrderIndex { get; set; } = 0;

        [MaxLength(500)]
        public string? Description { get; set; }

        public bool IsDefault { get; set; } = false;
    }

    public class UpdateMasterMilestoneRequestDto
    {
        [Required(ErrorMessage = "Nama milestone SDLC wajib diisi.")]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(100)]
        public string Phase { get; set; } = string.Empty;

        [MaxLength(7)]
        public string? Color { get; set; }

        [MaxLength(50)]
        public string? Icon { get; set; }

        public int OrderIndex { get; set; }

        [MaxLength(500)]
        public string? Description { get; set; }

        public bool IsDefault { get; set; }
    }

    public class MasterDataSummaryDto
    {
        public List<MasterCategoryResponseDto> Categories { get; set; } = new();
        public List<MasterPriorityResponseDto> Priorities { get; set; } = new();
        public List<MasterStatusResponseDto> Statuses { get; set; } = new();
        public List<MasterMilestoneResponseDto> Milestones { get; set; } = new();
    }
    #endregion

    #region Calendar DTOs
    public class CalendarEventDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string TaskCode { get; set; } = string.Empty;
        public string Start { get; set; } = string.Empty;
        public string? End { get; set; }
        public bool AllDay { get; set; } = true;
        public string BackgroundColor { get; set; } = "#6366F1";
        public string BorderColor { get; set; } = "#6366F1";
        public string TextColor { get; set; } = "#FFFFFF";
        public string Status { get; set; } = "Todo";
        public string Priority { get; set; } = "Medium";
        public string? ProjectName { get; set; }
        public string? AssigneeName { get; set; }
        public string Milestone { get; set; } = "Implementation";
        public int Progress { get; set; }
        public string Url { get; set; } = string.Empty;
    }
    #endregion

    #region Import & Export DTOs
    public class FileUploadDto
    {
        [Required(ErrorMessage = "File upload wajib dipilih.")]
        public IFormFile File { get; set; } = null!;
    }

    public class ImportPreviewRowDto
    {
        public int RowNumber { get; set; }
        public bool IsValid { get; set; } = true;
        public string Title { get; set; } = string.Empty;
        public string? Category { get; set; }
        public string? Project { get; set; }
        public string? Assignee { get; set; }
        public string? AssigneeUserId { get; set; }
        public string Priority { get; set; } = "Medium";
        public string Status { get; set; } = "Todo";
        public int Progress { get; set; } = 0;
        public string? StartDate { get; set; }
        public string? Deadline { get; set; }
        public string? Milestone { get; set; }
        public string? ErrorMessage { get; set; }
        public string? WarningMessage { get; set; }
    }

    public class ImportPreviewResponseDto
    {
        public string FileName { get; set; } = string.Empty;
        public int TotalRows { get; set; }
        public int SuccessRows { get; set; }
        public int FailedRows { get; set; }
        public List<ImportPreviewRowDto> Rows { get; set; } = new();
    }

    public class ExecuteImportRequestDto
    {
        [Required(ErrorMessage = "Daftar baris data tugas yang akan diimpor wajib diisi.")]
        public List<ImportPreviewRowDto> Rows { get; set; } = new();

        public int? DefaultProjectId { get; set; }
        public string? DefaultAssigneeId { get; set; }
    }

    public class ExecuteImportResponseDto
    {
        public int ImportedCount { get; set; }
        public int SkippedCount { get; set; }
        public List<int> CreatedTaskIds { get; set; } = new();
        public string Message { get; set; } = string.Empty;
    }
    #endregion

    #region JSON Tools DTOs
    public class FormatJsonRequestDto
    {
        [Required(ErrorMessage = "Konten JSON wajib diisi.")]
        public string Content { get; set; } = string.Empty;
        public int IndentSize { get; set; } = 2;
    }

    public class FormatJsonResponseDto
    {
        public string FormattedContent { get; set; } = string.Empty;
        public bool IsValid { get; set; }
        public string? ErrorMessage { get; set; }
        public long OriginalSizeBytes { get; set; }
        public long FormattedSizeBytes { get; set; }
    }

    public class MinifyJsonRequestDto
    {
        [Required(ErrorMessage = "Konten JSON wajib diisi.")]
        public string Content { get; set; } = string.Empty;
    }

    public class MinifyJsonResponseDto
    {
        public string MinifiedContent { get; set; } = string.Empty;
        public bool IsValid { get; set; }
        public string? ErrorMessage { get; set; }
        public long OriginalSizeBytes { get; set; }
        public long MinifiedSizeBytes { get; set; }
        public double CompressionRatioPercent { get; set; }
    }

    public class ValidateJsonRequestDto
    {
        [Required(ErrorMessage = "Konten JSON wajib diisi.")]
        public string Content { get; set; } = string.Empty;
    }

    public class ValidateJsonResponseDto
    {
        public bool IsValid { get; set; }
        public string? ErrorMessage { get; set; }
        public long? LineNumber { get; set; }
        public long? BytePosition { get; set; }
    }

    public class SaveJsonSnippetRequestDto
    {
        [Required(ErrorMessage = "Nama snippet JSON wajib diisi.")]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Konten JSON wajib diisi.")]
        public string Content { get; set; } = string.Empty;
    }

    public class JsonHistoryResponseDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public string SizeFormatted { get; set; } = "0 Bytes";
    }
    #endregion

    #region Notifications DTOs
    public class NotificationResponseDto
    {
        public string Id { get; set; } = string.Empty;
        public string Type { get; set; } = "task_due";
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string? Url { get; set; }
        public DateTime Timestamp { get; set; }
        public bool IsRead { get; set; }
        public string Severity { get; set; } = "info"; // info, warning, danger
    }

    public class NotificationsSummaryDto
    {
        public int TotalUnread { get; set; }
        public List<NotificationResponseDto> Notifications { get; set; } = new();
    }
    #endregion

    #region Dashboard & Sync DTOs
    public class DashboardSummaryResponseDto
    {
        public string CurrentUserName { get; set; } = string.Empty;
        public bool IsAdmin { get; set; }
        public int TotalTasks { get; set; }
        public int DoneTasks { get; set; }
        public int InProgressTasks { get; set; }
        public int TodoTasks { get; set; }
        public int OverdueTasks { get; set; }
        public int TotalProjects { get; set; }
        public int TotalMembers { get; set; }
        public long TodayTeamWorkSeconds { get; set; }
        public string TodayTeamWorkFormatted { get; set; } = "00:00:00";
        public int MyTotalTasks { get; set; }
        public int MyInProgressTasks { get; set; }
        public int MyDoneTasks { get; set; }
        public string MyTodayWorkFormatted { get; set; } = "00:00:00";
        public List<ProjectProgressReportDto> ProjectsDistribution { get; set; } = new();
        public DateTime LastSyncTimestamp { get; set; } = DateTime.Now;
    }

    public class TriggerSyncResponseDto
    {
        public bool IsSuccess { get; set; }
        public DateTime SyncTimestamp { get; set; }
        public int TasksEvaluatedCount { get; set; }
        public int StatusAutoUpdatedCount { get; set; }
        public string Message { get; set; } = string.Empty;
    }
    #endregion

    #region Tasks & Timesheets Extra DTOs
    public class BulkDeleteTasksRequestDto
    {
        [Required(ErrorMessage = "Daftar ID tugas yang akan dihapus wajib diisi.")]
        public List<int> TaskIds { get; set; } = new();
    }

    public class BulkDeleteTasksResponseDto
    {
        public int DeletedCount { get; set; }
        public List<int> DeletedIds { get; set; } = new();
        public string Message { get; set; } = string.Empty;
    }

    public class KanbanColumnDto
    {
        public string Status { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string BadgeColor { get; set; } = string.Empty;
        public int Count { get; set; }
        public List<TaskResponseDto> Tasks { get; set; } = new();
    }

    public class KanbanBoardResponseDto
    {
        public int TotalTasks { get; set; }
        public List<KanbanColumnDto> Columns { get; set; } = new();
    }

    public class AddTaskSessionRequestDto
    {
        [Required(ErrorMessage = "Waktu mulai sesi wajib diisi.")]
        public DateTime StartTime { get; set; }

        public DateTime? EndTime { get; set; }

        [Range(0, long.MaxValue, ErrorMessage = "Durasi dalam detik harus >= 0.")]
        public long DurationSeconds { get; set; }

        [MaxLength(500)]
        public string? Notes { get; set; }
    }

    public class TaskSessionResponseDto
    {
        public int Id { get; set; }
        public int TaskId { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public long DurationSeconds { get; set; }
        public string DurationFormatted { get; set; } = "00:00:00";
        public string? Notes { get; set; }
        public bool IsRunning { get; set; }
        public string? UserName { get; set; }
    }
    #endregion
}
