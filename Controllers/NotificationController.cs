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

            // 4. Timesheet Reminders (Cut-off tanggal 18-25)
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

            var overdueCount = dueDateTasks.Count(d => d.isOverdue);
            var totalAlerts = overdueCount + dueDateTasks.Count
                            + (isApproachingCutoff ? timesheetMissingTasks.Count : 0);

            return Json(new
            {
                success = true,
                totalAlerts,
                overdueCount,
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

        // ── AJAX: LIGHTWEIGHT UNREAD COUNT (fast badge polling) ───────
        [HttpGet]
        public async Task<IActionResult> GetUnreadCount()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var currentUserId = currentUser?.Id ?? "";
            var isAdmin = User.IsInRole("Admin");
            var now = DateTime.Now;
            var tomorrowEnd = DateTime.Today.AddDays(2).AddTicks(-1);

            var query = _db.Tasks.AsQueryable();
            if (!isAdmin) query = query.Where(t => t.AssignedToUserId == currentUserId);

            var tasks = await query
                .Select(t => new { t.Status, t.DueDate })
                .ToListAsync();

            var overdueCount = tasks.Count(t => t.DueDate.HasValue && t.DueDate.Value < now && t.Status != Models.TaskStatus.Done);
            var dueSoonCount = tasks.Count(t => t.DueDate.HasValue && t.DueDate.Value >= now && t.DueDate.Value <= tomorrowEnd && t.Status != Models.TaskStatus.Done);
            var total = overdueCount + dueSoonCount;

            return Json(new
            {
                success = true,
                total,
                overdueCount,
                dueSoonCount,
                hasUrgent = overdueCount > 0
            });
        }

        // ── AJAX: TEAM ACTIVITY FEED (last 48 hours) ─────────────────
        [HttpGet]
        public async Task<IActionResult> GetTeamActivity()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var isAdmin = User.IsInRole("Admin");
            var userCompanyId = currentUser?.CompanyId;
            var cutoffTime = DateTime.Now.AddHours(-48);

            var tasksQuery = _db.Tasks
                .Include(t => t.Project)
                .Include(t => t.AssignedToUser)
                .Include(t => t.Sessions)
                .AsQueryable();

            if (!isAdmin)
                tasksQuery = tasksQuery.Where(t =>
                    t.CompanyId == userCompanyId ||
                    (t.Project != null && t.Project.CompanyId == userCompanyId));

            var recentTasks = await tasksQuery
                .Where(t => t.UpdatedAt >= cutoffTime)
                .OrderByDescending(t => t.UpdatedAt)
                .Take(20)
                .ToListAsync();

            var activities = recentTasks.Select(t =>
            {
                var minutesAgo = (int)(DateTime.Now - t.UpdatedAt).TotalMinutes;
                var timeAgo = minutesAgo < 60
                    ? $"{minutesAgo}m lalu"
                    : minutesAgo < 1440
                        ? $"{minutesAgo / 60}j lalu"
                        : $"{minutesAgo / 1440}h lalu";

                string icon, colorClass, action;
                if (t.Status == Models.TaskStatus.Done)
                {
                    icon = "fa-check-circle"; colorClass = "emerald";
                    action = "menyelesaikan";
                }
                else if (t.Status == Models.TaskStatus.InProgress)
                {
                    icon = "fa-play-circle"; colorClass = "indigo";
                    action = "sedang mengerjakan";
                }
                else if (t.DueDate.HasValue && t.DueDate.Value < DateTime.Now)
                {
                    icon = "fa-exclamation-circle"; colorClass = "rose";
                    action = "OVERDUE:";
                }
                else
                {
                    icon = "fa-edit"; colorClass = "slate";
                    action = "memperbarui";
                }

                var assigneeName = t.AssignedToUser?.FullName ?? "Belum Ditugaskan";
                var nameParts = assigneeName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                var shortName = nameParts.Length >= 2 ? $"{nameParts[0]} {nameParts[1][0]}." : (nameParts.Length > 0 ? nameParts[0] : "?");
                var initials = t.AssignedToUser?.Initials ?? assigneeName.Substring(0, Math.Min(2, assigneeName.Length)).ToUpper();
                var avatarColor = t.AssignedToUser?.AvatarColor ?? "#6366F1";
                var hasActiveSession = t.Sessions.Any(s => s.EndTime == null);

                return new
                {
                    id = t.Id,
                    title = t.Title,
                    taskCode = t.TaskCode,
                    projectName = t.Project?.Name ?? "Tanpa Proyek",
                    status = t.Status.ToString(),
                    assigneeName = shortName,
                    initials,
                    avatarColor,
                    action,
                    icon,
                    colorClass,
                    timeAgo,
                    updatedAt = t.UpdatedAt.ToString("HH:mm dd/MM"),
                    isOverdue = t.DueDate.HasValue && t.DueDate.Value < DateTime.Now && t.Status != Models.TaskStatus.Done,
                    hasActiveSession
                };
            }).ToList();

            return Json(new { success = true, activities, total = activities.Count });
        }
    }
}
