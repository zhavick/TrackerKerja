using System.ComponentModel.DataAnnotations;
using TrackerKerja.Models;
using ModelTaskStatus = TrackerKerja.Models.TaskStatus;

namespace TrackerKerja.ViewModels
{
    /// <summary>
    /// Format standar kembalian API Task Tracker
    /// </summary>
    /// <typeparam name="T">Tipe data payload</typeparam>
    public class ApiResponse<T>
    {
        public bool Success { get; set; } = true;
        public string Message { get; set; } = string.Empty;
        public T? Data { get; set; }
        public List<string>? Errors { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        public static ApiResponse<T> Ok(T data, string message = "Sukses") => new()
        {
            Success = true,
            Message = message,
            Data = data,
            Timestamp = DateTime.UtcNow
        };

        public static ApiResponse<T> Fail(string message, List<string>? errors = null) => new()
        {
            Success = false,
            Message = message,
            Errors = errors ?? new List<string>(),
            Timestamp = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Ringkasan Project untuk relasi Task
    /// </summary>
    public class ProjectShortDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Color { get; set; } = "#6366F1";
    }

    /// <summary>
    /// Ringkasan Kategori untuk relasi Task
    /// </summary>
    public class CategoryShortDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Color { get; set; } = "#6366F1";
    }

    /// <summary>
    /// Ringkasan Pengguna (PIC / Assignee) untuk relasi Task
    /// </summary>
    public class UserShortDto
    {
        public string Id { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string JobTitle { get; set; } = string.Empty;
        public string AvatarColor { get; set; } = "#6366F1";
    }

    /// <summary>
    /// DTO Respons lengkap data Tugas (Task)
    /// </summary>
    public class TaskResponseDto
    {
        public int Id { get; set; }
        public string TaskCode { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? Obstacle { get; set; }
        public string? Solution { get; set; }

        public string Priority { get; set; } = "Medium";
        public string Status { get; set; } = "Todo";
        public int Progress { get; set; }

        public DateTime? StartDate { get; set; }
        public DateTime? DueDate { get; set; }
        public string? Tags { get; set; }
        public List<string> TagsList { get; set; } = new();

        public string Milestone { get; set; } = "Implementation";

        public int? ProjectId { get; set; }
        public ProjectShortDto? Project { get; set; }

        public int? CategoryId { get; set; }
        public CategoryShortDto? Category { get; set; }

        public string? AssignedToUserId { get; set; }
        public UserShortDto? AssignedToUser { get; set; }

        public int? ParentTaskId { get; set; }
        public string? ParentCode { get; set; }
        public bool IsParent { get; set; }
        public bool HasParent { get; set; }
        public int ChildTasksCount { get; set; }
        public int CompletedChildTasksCount { get; set; }

        public long TotalDurationSeconds { get; set; }
        public string TotalDurationFormatted { get; set; } = "00:00:00";

        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    /// <summary>
    /// DTO Request pembuatan Tugas baru (POST)
    /// </summary>
    public class CreateTaskRequestDto
    {
        [Required(ErrorMessage = "Judul tugas wajib diisi.")]
        [MaxLength(300, ErrorMessage = "Judul tugas maksimal 300 karakter.")]
        public string Title { get; set; } = string.Empty;

        [MaxLength(2000, ErrorMessage = "Deskripsi maksimal 2000 karakter.")]
        public string? Description { get; set; }

        [MaxLength(4000, ErrorMessage = "Kendala (Obstacle) maksimal 4000 karakter.")]
        public string? Obstacle { get; set; }

        [MaxLength(4000, ErrorMessage = "Solusi maksimal 4000 karakter.")]
        public string? Solution { get; set; }

        public int? ProjectId { get; set; }
        public int? CategoryId { get; set; }
        public string? AssignedToUserId { get; set; }

        [MaxLength(100)]
        public string? Milestone { get; set; } = "Implementation";

        public TaskPriority Priority { get; set; } = TaskPriority.Medium;
        public ModelTaskStatus Status { get; set; } = ModelTaskStatus.Todo;

        [Range(0, 100, ErrorMessage = "Progress harus berada di rentang 0 sampai 100.")]
        public int Progress { get; set; } = 0;

        public DateTime? StartDate { get; set; }
        public DateTime? DueDate { get; set; }

        public string? Tags { get; set; }
        public int? ParentTaskId { get; set; }
    }

    /// <summary>
    /// DTO Request pembaruan data Tugas (PUT)
    /// </summary>
    public class UpdateTaskRequestDto
    {
        [Required(ErrorMessage = "Judul tugas wajib diisi.")]
        [MaxLength(300, ErrorMessage = "Judul tugas maksimal 300 karakter.")]
        public string Title { get; set; } = string.Empty;

        [MaxLength(2000, ErrorMessage = "Deskripsi maksimal 2000 karakter.")]
        public string? Description { get; set; }

        [MaxLength(4000, ErrorMessage = "Kendala (Obstacle) maksimal 4000 karakter.")]
        public string? Obstacle { get; set; }

        [MaxLength(4000, ErrorMessage = "Solusi maksimal 4000 karakter.")]
        public string? Solution { get; set; }

        public int? ProjectId { get; set; }
        public int? CategoryId { get; set; }
        public string? AssignedToUserId { get; set; }

        [MaxLength(100)]
        public string? Milestone { get; set; } = "Implementation";

        public TaskPriority Priority { get; set; } = TaskPriority.Medium;
        public ModelTaskStatus Status { get; set; } = ModelTaskStatus.Todo;

        [Range(0, 100, ErrorMessage = "Progress harus berada di rentang 0 sampai 100.")]
        public int Progress { get; set; } = 0;

        public DateTime? StartDate { get; set; }
        public DateTime? DueDate { get; set; }

        public string? Tags { get; set; }
        public int? ParentTaskId { get; set; }
    }

    /// <summary>
    /// DTO Request pembaruan status dan progress tugas (PUT / PATCH status)
    /// </summary>
    public class UpdateTaskStatusDto
    {
        [Required(ErrorMessage = "Status baru wajib diisi.")]
        public ModelTaskStatus Status { get; set; }

        [Range(0, 100, ErrorMessage = "Progress harus berada di rentang 0 sampai 100.")]
        public int? Progress { get; set; }
    }

    /// <summary>
    /// DTO Ringkasan statistik tugas
    /// </summary>
    public class TaskSummaryDto
    {
        public int TotalTasks { get; set; }
        public int TodoTasks { get; set; }
        public int InProgressTasks { get; set; }
        public int DoneTasks { get; set; }
        public int OverdueTasks { get; set; }
        public long TotalWorkSeconds { get; set; }
        public string TotalWorkFormatted { get; set; } = "0j 0m";
    }

    /// <summary>
    /// DTO Pagination response untuk daftar Tugas
    /// </summary>
    public class PagedResult<T>
    {
        public List<T> Items { get; set; } = new();
        public int TotalItems { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalPages => (int)Math.Ceiling(TotalItems / (double)Math.Max(1, PageSize));
        public bool HasNextPage => Page < TotalPages;
        public bool HasPreviousPage => Page > 1;
    }
}
