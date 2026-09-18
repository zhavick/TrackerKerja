using System.ComponentModel.DataAnnotations;

namespace TrackerKerja.ViewModels
{
    // ── Account ViewModels ────────────────────────────────────
    public class LoginViewModel
    {
        [Required(ErrorMessage = "Email wajib diisi")]
        [EmailAddress(ErrorMessage = "Format email tidak valid")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password wajib diisi")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        public bool RememberMe { get; set; }
        public string? ReturnUrl { get; set; }
    }

    public class RegisterViewModel
    {
        [Required(ErrorMessage = "Nama lengkap wajib diisi")]
        [MaxLength(150)]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email wajib diisi")]
        [EmailAddress(ErrorMessage = "Format email tidak valid")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Jabatan wajib diisi")]
        [MaxLength(100)]
        public string JobTitle { get; set; } = string.Empty;

        // Multi-Tenancy Registration
        public string CompanyOption { get; set; } = "new"; // "new" atau "existing"
        public int? CompanyId { get; set; }

        [MaxLength(150)]
        public string? NewCompanyName { get; set; }

        [MaxLength(50)]
        public string? NewCompanyCode { get; set; }

        [Required(ErrorMessage = "Password wajib diisi")]
        [MinLength(6, ErrorMessage = "Password minimal 6 karakter")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "Konfirmasi password wajib diisi")]
        [DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "Password tidak cocok")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }

    public class ProfileViewModel
    {
        [Required(ErrorMessage = "Nama lengkap wajib diisi")]
        [MaxLength(150)]
        public string FullName { get; set; } = string.Empty;

        [MaxLength(100)]
        public string JobTitle { get; set; } = string.Empty;

        [MaxLength(7)]
        public string AvatarColor { get; set; } = "#6366F1";

        public string? ProfilePictureUrl { get; set; }
        public IFormFile? ProfilePicture { get; set; }

        public string? CoverPictureUrl { get; set; }
        public IFormFile? CoverPicture { get; set; }
        public bool RemoveCover { get; set; } = false;

        public string Email { get; set; } = string.Empty;
        public string Initials { get; set; } = "?";
        public DateTime CreatedAt { get; set; }

        // Stats
        public int TotalTasks { get; set; }
        public int DoneTasks { get; set; }
        public int TotalProjects { get; set; }
        public double TotalHours { get; set; }

        // Gamification & Badges
        public GamificationProfileDto Gamification { get; set; } = new();
    }

    public class ChangePasswordViewModel
    {
        [Required(ErrorMessage = "Password lama wajib diisi")]
        [DataType(DataType.Password)]
        public string CurrentPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password baru wajib diisi")]
        [MinLength(6, ErrorMessage = "Password minimal 6 karakter")]
        [DataType(DataType.Password)]
        public string NewPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "Konfirmasi password wajib diisi")]
        [DataType(DataType.Password)]
        [Compare("NewPassword", ErrorMessage = "Password tidak cocok")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }

    // ── Import & Sync ViewModels ─────────────────────────────
    public class ImportPreviewRow
    {
        public int RowNumber { get; set; }
        public string SheetName { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string? Category { get; set; }
        public string? Project { get; set; }
        public string? Assignee { get; set; }
        public string? AssigneeUserId { get; set; }
        public string Priority { get; set; } = "Medium";
        public string Status { get; set; } = "Todo";
        public int Progress { get; set; } = 0;
        public string? StartDate { get; set; }
        public string? EndDate { get; set; }
        public string? Deadline { get; set; }
        public string? Obstacle { get; set; }
        public string? Solution { get; set; }
        public string? Requirement { get; set; }
        public string? ModuleName { get; set; }
        public string? Milestone { get; set; }
        public string? BugType { get; set; }
        public string? DeveloperEmails { get; set; }
        public string? BaEmails { get; set; }
        public string? InfraEmails { get; set; }
        public string? MasterDataEmails { get; set; }
        public string? TesterEmails { get; set; }
        public string? TwEmails { get; set; }
        public string? NotesTracker { get; set; }
        public string? Pic { get; set; }
        public bool IsValid { get; set; } = true;
        public string? ErrorMessage { get; set; }
        public string? WarningMessage { get; set; }

        // Diff & Sync Metadata
        public string ActionType { get; set; } = "INSERT"; // "INSERT", "UPDATE", "UNCHANGED", "ERROR"
        public int? ExistingTaskId { get; set; }
        public List<string> ChangedFields { get; set; } = new();
    }

    public class ImportResultViewModel
    {
        public string FileName { get; set; } = string.Empty;
        public string SourceType { get; set; } = "Upload"; // "Upload", "Link", "LocalFile"
        public string? SourceUrl { get; set; }
        public int TotalRows { get; set; }
        public int SuccessRows { get; set; }
        public int FailedRows { get; set; }
        public int InsertCount { get; set; }
        public int UpdateCount { get; set; }
        public int UnchangedCount { get; set; }
        public List<string> ProcessedSheets { get; set; } = new();
        public List<ImportPreviewRow> Rows { get; set; } = new();
        public List<string> Errors { get; set; } = new();
    }
}
