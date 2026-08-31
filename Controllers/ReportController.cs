using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TrackerKerja.Data;

namespace TrackerKerja.Controllers
{
    [Authorize]
    public class ReportController : Controller
    {
        private readonly AppDbContext _db;
        public ReportController(AppDbContext db) { _db = db; }

        public async Task<IActionResult> Index()
        {
            var sessions = await _db.Sessions
                .Include(s => s.Task)
                .ThenInclude(t => t!.Project)
                .Where(s => s.EndTime != null)
                .ToListAsync();

            var projects = await _db.Projects
                .Include(p => p.Tasks)
                .ThenInclude(t => t.Sessions)
                .ToListAsync();

            var users = await _db.Users.OrderBy(u => u.FullName).ToListAsync();

            ViewBag.Sessions = sessions;
            ViewBag.Projects = projects;
            ViewBag.Members = users;
            ViewBag.TotalHours = sessions.Sum(s => s.Duration) / 3600.0;
            ViewBag.TotalTasks = await _db.Tasks.CountAsync();
            ViewBag.DoneTasks = await _db.Tasks.CountAsync(t => t.Status == Models.TaskStatus.Done);
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetGanttData(int? projectId, string? assigneeId, string? status, string? search)
        {
            var query = _db.Tasks
                .Include(t => t.Project)
                .Include(t => t.AssignedToUser)
                .Include(t => t.Sessions)
                .AsQueryable();

            if (projectId.HasValue)
                query = query.Where(t => t.ProjectId == projectId.Value);

            if (!string.IsNullOrWhiteSpace(assigneeId))
                query = query.Where(t => t.AssignedToUserId == assigneeId);

            if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<Models.TaskStatus>(status, out var st))
                query = query.Where(t => t.Status == st);

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim().ToLower();
                query = query.Where(t => t.Title.ToLower().Contains(s) || (t.Description != null && t.Description.ToLower().Contains(s)));
            }

            var tasks = await query.OrderBy(t => t.StartDate ?? t.CreatedAt).ToListAsync();

            var ganttTasks = tasks.Select(t =>
            {
                var startDt = t.StartDate ?? t.CreatedAt.Date;
                var endDt = t.DueDate ?? (t.StartDate.HasValue ? t.StartDate.Value.AddDays(3) : t.CreatedAt.Date.AddDays(3));
                if (endDt < startDt) endDt = startDt.AddDays(1);

                var isOverdue = t.DueDate.HasValue && t.DueDate.Value < DateTime.Now && t.Status != Models.TaskStatus.Done;
                var statusText = isOverdue ? "Overdue" : t.Status.ToString();

                var customClass = statusText.ToLower() switch
                {
                    "done" => "gantt-status-done",
                    "inprogress" => "gantt-status-inprogress",
                    "overdue" => "gantt-status-overdue",
                    _ => "gantt-status-todo"
                };

                return new
                {
                    id = t.Id.ToString(),
                    code = t.TaskCode,
                    name = t.Title,
                    start = startDt.ToString("yyyy-MM-dd"),
                    end = endDt.ToString("yyyy-MM-dd"),
                    progress = t.Progress,
                    status = statusText,
                    priority = t.Priority.ToString(),
                    projectId = t.ProjectId,
                    projectName = t.Project?.Name ?? "Tanpa Proyek",
                    projectColor = t.Project?.Color ?? "#6366F1",
                    assigneeId = t.AssignedToUserId,
                    assigneeName = t.AssignedToUser?.FullName ?? "Belum Ditugaskan",
                    assigneeAvatarColor = t.AssignedToUser?.AvatarColor ?? "#6366F1",
                    dependencies = t.ParentTaskId.HasValue ? t.ParentTaskId.Value.ToString() : "",
                    custom_class = customClass,
                    isParent = t.IsParent,
                    parentTaskId = t.ParentTaskId,
                    parentCode = t.ParentCode,
                    obstacle = t.Obstacle,
                    solution = t.Solution,
                    milestone = t.Milestone ?? "Implementation",
                    durationFormatted = t.TotalDurationFormatted
                };
            }).ToList();

            return Json(new { tasks = ganttTasks, total = ganttTasks.Count });
        }

        [HttpGet]
        public async Task<IActionResult> GetChartData(string period = "week")
        {
            var now = DateTime.Now;
            DateTime start;
            string format;
            int days;

            switch (period)
            {
                case "month":
                    start = now.AddDays(-29);
                    format = "dd/MM";
                    days = 30;
                    break;
                default: // week
                    start = now.AddDays(-6);
                    format = "ddd";
                    days = 7;
                    break;
            }

            var sessions = await _db.Sessions
                .Where(s => s.StartTime >= start && s.EndTime != null)
                .ToListAsync();

            var labels = new List<string>();
            var data = new List<double>();

            for (int i = 0; i < days; i++)
            {
                var day = start.AddDays(i);
                labels.Add(day.ToString(format));
                var hours = sessions
                    .Where(s => s.StartTime.Date == day.Date)
                    .Sum(s => s.Duration) / 3600.0;
                data.Add(Math.Round(hours, 1));
            }

            return Json(new { labels, data });
        }
    }
}
