using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TrackerKerja.Data;
using TrackerKerja.Models;

namespace TrackerKerja.Controllers
{
    [Authorize]
    public class NotificationController : Controller
    {
        private readonly AppDbContext _db;
        private readonly UserManager<AppUser> _userManager;

        public NotificationController(AppDbContext db, UserManager<AppUser> userManager)
        {
            _db = db;
            _userManager = userManager;
        }

        [HttpGet]
        public async Task<IActionResult> GetSummary()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var currentUserId = currentUser?.Id ?? "";
            var isAdmin = User.IsInRole("Admin");

            var now = DateTime.Now;
            var today = DateTime.Today;
            var tomorrowEnd = today.AddDays(2).AddTicks(-1);

            // Fetch tasks
            var query = _db.Tasks
                .Include(t => t.Project)
                .Include(t => t.AssignedToUser)
                .Include(t => t.Sessions)
                .AsQueryable();

            if (!isAdmin)
            {
                query = query.Where(t => t.AssignedToUserId == currentUserId);
            }

            var tasks = await query.ToListAsync();

            // 1. Due Date / Overdue Tasks
            var dueDateTasks = tasks
                .Where(t => t.Status != Models.TaskStatus.Done && t.DueDate.HasValue && t.DueDate.Value <= tomorrowEnd)
                .OrderBy(t => t.DueDate)
                .Select(t => new
                {
                    id = t.Id,
                    taskCode = t.TaskCode,
                    title = t.Title,
                    projectName = t.Project?.Name ?? "Tanpa Proyek",
                    dueDateFormatted = t.DueDate?.ToString("dd MMM yyyy"),
                    isOverdue = t.DueDate < now,
                    priority = t.Priority.ToString(),
                    progress = t.Progress,
                    assignedTo = t.AssignedToUser?.FullName ?? "Belum Ditugaskan"
                })
                .Take(10)
                .ToList();

            // 2. In Progress Tasks
            var inProgressTasks = tasks
                .Where(t => t.Status == Models.TaskStatus.InProgress)
                .OrderByDescending(t => t.UpdatedAt)
                .Select(t => new
                {
                    id = t.Id,
                    taskCode = t.TaskCode,
                    title = t.Title,
                    projectName = t.Project?.Name ?? "Tanpa Proyek",
                    dueDateFormatted = t.DueDate?.ToString("dd MMM yyyy") ?? "—",
                    progress = t.Progress,
                    priority = t.Priority.ToString(),
                    assignedTo = t.AssignedToUser?.FullName ?? "Belum Ditugaskan"
                })
                .Take(10)
                .ToList();

            // 3. Todo Tasks
            var todoTasks = tasks
                .Where(t => t.Status == Models.TaskStatus.Todo)
                .OrderByDescending(t => t.CreatedAt)
                .Select(t => new
                {
                    id = t.Id,
                    taskCode = t.TaskCode,
                    title = t.Title,
                    projectName = t.Project?.Name ?? "Tanpa Proyek",
                    dueDateFormatted = t.DueDate?.ToString("dd MMM yyyy") ?? "—",
                    priority = t.Priority.ToString(),
                    assignedTo = t.AssignedToUser?.FullName ?? "Belum Ditugaskan"
                })
                .Take(10)
                .ToList();

            // 4. Timesheet Reminders (Mendekati tanggal 25 setiap bulannya: tanggal 18 s/d 25)
            var isApproachingCutoff = now.Day >= 18 && now.Day <= 25;
            var cutoffDaysLeft = Math.Max(0, 25 - now.Day);
            var timesheetMissingTasks = tasks
                .Where(t => t.Status != Models.TaskStatus.Done && (t.Sessions == null || !t.Sessions.Any(s => s.Duration > 0)))
                .OrderByDescending(t => t.Priority)
                .ThenByDescending(t => t.UpdatedAt)
                .Select(t => new
                {
                    id = t.Id,
                    taskCode = t.TaskCode,
                    title = t.Title,
                    projectName = t.Project?.Name ?? "Tanpa Proyek",
                    dueDateFormatted = t.DueDate?.ToString("dd MMM yyyy") ?? "—",
                    status = t.Status.ToString(),
                    priority = t.Priority.ToString(),
                    progress = t.Progress,
                    assignedTo = t.AssignedToUser?.FullName ?? "Belum Ditugaskan"
                })
                .Take(10)
                .ToList();

            var totalAlerts = dueDateTasks.Count(d => d.isOverdue) 
                            + dueDateTasks.Count 
                            + inProgressTasks.Count 
                            + (isApproachingCutoff ? timesheetMissingTasks.Count : 0);

            return Json(new
            {
                success = true,
                totalAlerts,
                dueDateCount = dueDateTasks.Count,
                inProgressCount = inProgressTasks.Count,
                todoCount = todoTasks.Count,
                timesheetCount = timesheetMissingTasks.Count,
                isApproachingCutoff,
                cutoffDaysLeft,
                dueDateTasks,
                inProgressTasks,
                todoTasks,
                timesheetTasks = timesheetMissingTasks
            });
        }
    }
}
