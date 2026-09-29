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
            var userCompanyId = currentUser?.CompanyId;

            var query = _db.Tasks
                .Include(t => t.Project)
                .Include(t => t.AssignedToUser)
                .Where(t => t.DueDate != null);

            // Filter hak akses:
            // 1. Member non-admin hanya dapat melihat tugas yang ditugaskan kepada dirinya sendiri dalam perusahaannya
            if (!isAdmin)
            {
                query = query.Where(t => (t.CompanyId == userCompanyId || (t.Project != null && t.Project.CompanyId == userCompanyId)) && t.AssignedToUserId == currentUserId);
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
            var task = await _db.Tasks.Include(t => t.Project).FirstOrDefaultAsync(t => t.Id == taskId);
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

        // ── AJAX / DOWNLOAD: EXPORT JADWAL TUGAS KE ICAL (.ICS) ─────
        [HttpGet]
        public async Task<IActionResult> ExportIcs(string? filter = null)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var currentUserId = currentUser?.Id ?? "";
            var isAdmin = User.IsInRole("Admin");
            var userCompanyId = currentUser?.CompanyId;

            var query = _db.Tasks
                .Include(t => t.Project)
                .Include(t => t.AssignedToUser)
                .Where(t => t.DueDate != null);

            if (!isAdmin)
            {
                query = query.Where(t => (t.CompanyId == userCompanyId || (t.Project != null && t.Project.CompanyId == userCompanyId)) && t.AssignedToUserId == currentUserId);
            }
            else if (string.Equals(filter, "mine", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(filter, "my", StringComparison.OrdinalIgnoreCase))
            {
                query = query.Where(t => t.AssignedToUserId == currentUserId);
            }

            var tasks = await query.OrderBy(t => t.DueDate).ToListAsync();

            var sb = new System.Text.StringBuilder();
            sb.AppendLine("BEGIN:VCALENDAR");
            sb.AppendLine("VERSION:2.0");
            sb.AppendLine("PRODID:-//TrackerKerja//Work Tracker Pro//ID");
            sb.AppendLine("CALSCALE:GREGORIAN");
            sb.AppendLine("METHOD:PUBLISH");
            sb.AppendLine("X-WR-CALNAME:Jadwal Tugas - TrackerKerja");
            sb.AppendLine("X-WR-TIMEZONE:Asia/Jakarta");

            var baseUrl = $"{Request.Scheme}://{Request.Host}";

            foreach (var t in tasks)
            {
                var due = t.DueDate!.Value;
                var start = t.StartDate ?? due.Date;
                var dtStamp = DateTime.UtcNow.ToString("yyyyMMddTHHmmssZ");
                var dtStart = start.ToString("yyyyMMdd");
                var dtEnd = due.AddDays(1).ToString("yyyyMMdd"); // All-day event end is exclusive in iCal

                var summary = t.Project != null ? $"[{t.Project.Name}] {t.Title}" : t.Title;
                if (!string.IsNullOrEmpty(t.TaskCode)) summary = $"[{t.TaskCode}] " + summary;

                var desc = $"Prioritas: {t.Priority}\\nStatus: {t.Status}\\nPIC: {t.AssignedToUser?.FullName ?? "Belum Ditugaskan"}\\nProgres: {t.Progress}%";
                if (!string.IsNullOrWhiteSpace(t.Description))
                {
                    var cleanDesc = t.Description.Replace("\r", "").Replace("\n", "\\n").Replace(",", "\\,");
                    desc += "\\n\\nDeskripsi: " + cleanDesc;
                }

                int priorityVal = t.Priority switch
                {
                    TaskPriority.Critical => 1,
                    TaskPriority.High => 3,
                    TaskPriority.Medium => 5,
                    _ => 9
                };

                sb.AppendLine("BEGIN:VEVENT");
                sb.AppendLine($"UID:task-{t.Id}@trackerkerja");
                sb.AppendLine($"DTSTAMP:{dtStamp}");
                sb.AppendLine($"DTSTART;VALUE=DATE:{dtStart}");
                sb.AppendLine($"DTEND;VALUE=DATE:{dtEnd}");
                sb.AppendLine($"SUMMARY:{EscapeIcs(summary)}");
                sb.AppendLine($"DESCRIPTION:{desc}");
                sb.AppendLine($"PRIORITY:{priorityVal}");
                sb.AppendLine($"STATUS:{(t.Status == Models.TaskStatus.Done ? "COMPLETED" : "CONFIRMED")}");
                sb.AppendLine($"URL:{baseUrl}/Task/Edit/{t.Id}");
                sb.AppendLine("END:VEVENT");
            }

            sb.AppendLine("END:VCALENDAR");

            var bytes = System.Text.Encoding.UTF8.GetBytes(sb.ToString());
            var filename = $"jadwal-tugas-{DateTime.Now:yyyyMMdd-HHmm}.ics";
            return File(bytes, "text/calendar", filename);
        }

        private static string EscapeIcs(string text)
        {
            if (string.IsNullOrEmpty(text)) return "";
            return text.Replace("\\", "\\\\")
                       .Replace(";", "\\;")
                       .Replace(",", "\\,")
                       .Replace("\r\n", "\\n")
                       .Replace("\n", "\\n");
        }
    }
}
