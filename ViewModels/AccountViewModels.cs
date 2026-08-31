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

        public string Email { get; set; } = string.Empty;
        public string Initials { get; set; } = "?";
        public DateTime CreatedAt { get; set; }

        // Stats
        public int TotalTasks { get; set; }
        public int DoneTasks { get; set; }
        public int TotalProjects { get; set; }
        public double TotalHours { get; set; }
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

    // ── Import ViewModels ────────────────────────────────────
    public class ImportPreviewRow
    {
        public int RowNumber { get; set; }
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
        public bool IsValid { get; set; } = true;
        public string? ErrorMessage { get; set; }
        public string? WarningMessage { get; set; }
    }

    public class ImportResultViewModel
    {
        public string FileName { get; set; } = string.Empty;
        public int TotalRows { get; set; }
        public int SuccessRows { get; set; }
        public int FailedRows { get; set; }
        public List<ImportPreviewRow> Rows { get; set; } = new();
        public List<string> Errors { get; set; } = new();
    }
}
