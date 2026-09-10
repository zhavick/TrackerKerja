using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TrackerKerja.Data;
using TrackerKerja.Models;
using TrackerKerja.Services;
using TrackerKerja.ViewModels;

namespace TrackerKerja.Controllers
{
    [Authorize]
    public class CalendarController : Controller
    {
        private readonly AppDbContext _db;
        private readonly UserManager<AppUser> _userManager;

        public CalendarController(AppDbContext db, UserManager<AppUser> userManager)
        {
            _db = db;
            _userManager = userManager;
        }

        public IActionResult Index() => View();

        [HttpGet]
        public async Task<IActionResult> GetEvents(string? start, string? end, string? filter = null)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var currentUserId = currentUser?.Id ?? "";
            var isAdmin = User.IsInRole("Admin");

            var query = _db.Tasks
                .Include(t => t.Project)
                .Include(t => t.AssignedToUser)
                .Where(t => t.DueDate != null);

            // Filter hak akses:
            // 1. Member non-admin hanya dapat melihat tugas yang ditugaskan kepada dirinya sendiri
            if (!isAdmin)
            {
                query = query.Where(t => t.AssignedToUserId == currentUserId);
            }
            // 2. Admin dapat melihat seluruh tugas (default/all) atau menyaring tugas miliknya sendiri (mine)
            else if (string.Equals(filter, "mine", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(filter, "my", StringComparison.OrdinalIgnoreCase))
            {
                query = query.Where(t => t.AssignedToUserId == currentUserId);
            }

            var tasks = await query.ToListAsync();

            var colors = new Dictionary<string, string>
            {
                { "Done", "#10B981" },
                { "InProgress", "#6366F1" },
                { "Todo", "#F59E0B" },
                { "Overdue", "#EF4444" }
            };

            var events = tasks.Select(t =>
            {
                var status = t.DueDate < DateTime.Now && t.Status != Models.TaskStatus.Done
                    ? "Overdue" : t.Status.ToString();

                return new CalendarEventViewModel
                {
                    Id = t.Id,
                    Title = t.Title,
                    Start = t.StartDate?.ToString("yyyy-MM-dd") ?? t.DueDate?.ToString("yyyy-MM-dd"),
                    End = t.DueDate?.ToString("yyyy-MM-dd"),
                    Color = colors.GetValueOrDefault(status, "#6366F1"),
                    Status = status,
                    Priority = t.Priority.ToString(),
                    ProjectName = t.Project?.Name,
                    AssigneeName = t.AssignedToUser?.FullName ?? "Belum Ditugaskan",
                    AssigneeAvatar = t.AssignedToUser?.ProfilePictureUrl,
                    Url = $"/Task/Edit/{t.Id}"
                };
            }).ToList();

            return Json(events);
        }

        [HttpPost]
        public async Task<IActionResult> UpdateTaskDate(int taskId, string newDate)
        {
            var task = await _db.Tasks.FindAsync(taskId);
            if (task == null) return Json(new { success = false, message = "Tugas tidak ditemukan." });

            var currentUser = await _userManager.GetUserAsync(User);
            var isAdmin = User.IsInRole("Admin");

            if (!TaskPermissionHelper.CanEditTask(currentUser, isAdmin, task))
            {
                return Json(new { success = false, message = "Akses Ditolak: Anda hanya dapat mengubah jadwal tugas milik Anda sendiri." });
            }

            if (DateTime.TryParse(newDate, out var date))
            {
                task.DueDate = date;
                task.UpdatedAt = DateTime.Now;
                await _db.SaveChangesAsync();
                return Json(new { success = true });
            }
            return Json(new { success = false, message = "Format tanggal tidak valid." });
        }
    }
}
