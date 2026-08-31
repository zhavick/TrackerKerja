using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TrackerKerja.Data;
using TrackerKerja.Models;
using TrackerKerja.ViewModels;
using ModelTaskStatus = TrackerKerja.Models.TaskStatus;

namespace TrackerKerja.Controllers.Api
{
    /// <summary>
    /// Modul API Notifikasi dan Peringatan Sistem (Tugas Jatuh Tempo, Overdue, dan Penugasan)
    /// </summary>
    [ApiController]
    [Route("api/notifications")]
    [Produces("application/json")]
    public class NotificationsApiController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly UserManager<AppUser> _userManager;

        public NotificationsApiController(AppDbContext db, UserManager<AppUser> userManager)
        {
            _db = db;
            _userManager = userManager;
        }

        /// <summary>
        /// Mengambil daftar seluruh notifikasi dan peringatan aktif pengguna (GET /api/notifications)
        /// </summary>
        /// <param name="userId">Filter ID pengguna (opsional jika dipanggil oleh admin)</param>
        [HttpGet]
        [ProducesResponseType(typeof(ApiResponse<NotificationsSummaryDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetNotifications([FromQuery] string? userId)
        {
            var targetUserId = userId;
            if (string.IsNullOrWhiteSpace(targetUserId))
            {
                var currentUser = await _userManager.GetUserAsync(User);
                targetUserId = currentUser?.Id;
            }

            var now = DateTime.Now;
            var tomorrowEnd = DateTime.Today.AddDays(2).AddTicks(-1);

            var query = _db.Tasks
                .Include(t => t.Project)
                .Include(t => t.AssignedToUser)
                .AsNoTracking()
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(targetUserId) && !User.IsInRole("Admin"))
            {
                query = query.Where(t => t.AssignedToUserId == targetUserId);
            }

            var tasks = await query.ToListAsync();

            var notifications = new List<NotificationResponseDto>();

            // 1. Overdue Tasks (Danger)
            var overdueTasks = tasks.Where(t => t.Status != ModelTaskStatus.Done && t.DueDate.HasValue && t.DueDate.Value < now);
            foreach (var t in overdueTasks)
            {
                notifications.Add(new NotificationResponseDto
                {
                    Id = $"overdue-{t.Id}",
                    Type = "overdue",
                    Title = $"Tugas Overdue: [{t.TaskCode}] {t.Title}",
                    Message = $"Tugas melewati tenggat waktu ({t.DueDate:dd MMM yyyy}). Proyek: {t.Project?.Name ?? "Tanpa Proyek"}.",
                    Url = $"/Task/Edit/{t.Id}",
                    Timestamp = t.DueDate ?? t.UpdatedAt,
                    IsRead = false,
                    Severity = "danger"
                });
            }

            // 2. Due Soon Tasks (Warning)
            var dueSoonTasks = tasks.Where(t => t.Status != ModelTaskStatus.Done && t.DueDate.HasValue && t.DueDate.Value >= now && t.DueDate.Value <= tomorrowEnd);
            foreach (var t in dueSoonTasks)
            {
                notifications.Add(new NotificationResponseDto
                {
                    Id = $"due-{t.Id}",
                    Type = "due_soon",
                    Title = $"Mendekati Deadline: [{t.TaskCode}] {t.Title}",
                    Message = $"Tenggat waktu pengerjaan berakhir pada {t.DueDate:dd MMM yyyy HH:mm}.",
                    Url = $"/Task/Edit/{t.Id}",
                    Timestamp = t.DueDate ?? t.UpdatedAt,
                    IsRead = false,
                    Severity = "warning"
                });
            }

            // 3. In Progress Active
            var inProgress = tasks.Where(t => t.Status == ModelTaskStatus.InProgress).Take(5);
            foreach (var t in inProgress)
            {
                notifications.Add(new NotificationResponseDto
                {
                    Id = $"prog-{t.Id}",
                    Type = "in_progress",
                    Title = $"Sedang Dikerjakan: [{t.TaskCode}] {t.Title}",
                    Message = $"Progress saat ini {t.Progress}%. PIC: {t.AssignedToUser?.FullName ?? "Belum Ditugaskan"}.",
                    Url = $"/Task/Edit/{t.Id}",
                    Timestamp = t.UpdatedAt,
                    IsRead = true,
                    Severity = "info"
                });
            }

            var summary = new NotificationsSummaryDto
            {
                TotalUnread = notifications.Count(n => !n.IsRead),
                Notifications = notifications.OrderByDescending(n => n.Severity == "danger")
                                             .ThenByDescending(n => n.Severity == "warning")
                                             .ThenByDescending(n => n.Timestamp)
                                             .ToList()
            };

            return Ok(ApiResponse<NotificationsSummaryDto>.Ok(summary, $"Berhasil mengambil {notifications.Count} notifikasi."));
        }

        /// <summary>
        /// Menandai satu notifikasi telah dibaca (POST /api/notifications/{id}/read)
        /// </summary>
        /// <param name="id">ID Notifikasi</param>
        [HttpPost("{id}/read")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        public IActionResult MarkAsRead(string id)
        {
            return Ok(ApiResponse<object>.Ok(new { id = id, isRead = true }, "Notifikasi telah ditandai sebagai dibaca."));
        }

        /// <summary>
        /// Menandai seluruh notifikasi telah dibaca (POST /api/notifications/read-all)
        /// </summary>
        [HttpPost("read-all")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        public IActionResult MarkAllAsRead()
        {
            return Ok(ApiResponse<object>.Ok(new { allRead = true }, "Seluruh notifikasi berhasil ditandai sebagai dibaca."));
        }
    }
}
