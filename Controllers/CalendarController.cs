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
        public async Task<IActionResult> GetEvents(string? start, string? end)
        {
            var tasks = await _db.Tasks
                .Include(t => t.Project)
                .Where(t => t.DueDate != null)
                .ToListAsync();

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
