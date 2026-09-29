using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TrackerKerja.Data;
using TrackerKerja.Models;
using ClosedXML.Excel;

namespace TrackerKerja.Controllers
{
    [Authorize]
    public class ReportController : Controller
    {
        private readonly AppDbContext _db;
        private readonly UserManager<AppUser> _userManager;

        public ReportController(AppDbContext db, UserManager<AppUser> userManager)
        {
            _db = db;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var isAdmin = User.IsInRole("Admin");
            var userCompanyId = currentUser?.CompanyId;

            var sessionsQuery = _db.Sessions
                .Include(s => s.Task)
                .ThenInclude(t => t!.Project)
                .Where(s => s.EndTime != null)
                .AsQueryable();

            var projectsQuery = _db.Projects
                .Include(p => p.Tasks)
                .ThenInclude(t => t.Sessions)
                .AsQueryable();

            var usersQuery = _db.Users.OrderBy(u => u.FullName).AsQueryable();
            var tasksQuery = _db.Tasks.AsQueryable();

            if (!isAdmin)
            {
                sessionsQuery = sessionsQuery.Where(s => s.Task != null && (s.Task.CompanyId == userCompanyId || (s.Task.Project != null && s.Task.Project.CompanyId == userCompanyId)));
                projectsQuery = projectsQuery.Where(p => p.CompanyId == userCompanyId);
                usersQuery = usersQuery.Where(u => u.CompanyId == userCompanyId);
                tasksQuery = tasksQuery.Where(t => t.CompanyId == userCompanyId || (t.Project != null && t.Project.CompanyId == userCompanyId));
            }

            var sessions = await sessionsQuery.ToListAsync();
            var projects = await projectsQuery.ToListAsync();
            var users = await usersQuery.ToListAsync();

            ViewBag.Sessions = sessions;
            ViewBag.Projects = projects;
            ViewBag.Members = users;
            ViewBag.TotalHours = sessions.Sum(s => s.Duration) / 3600.0;
            ViewBag.TotalTasks = await tasksQuery.CountAsync();
            ViewBag.DoneTasks = await tasksQuery.CountAsync(t => t.Status == Models.TaskStatus.Done);
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetGanttData(int? projectId, string? assigneeId, string? status, string? search)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var isAdmin = User.IsInRole("Admin");
            var userCompanyId = currentUser?.CompanyId;

            var query = _db.Tasks
                .Include(t => t.Project)
                .Include(t => t.AssignedToUser)
                .Include(t => t.Sessions)
                .AsQueryable();

            if (!isAdmin)
            {
                query = query.Where(t => t.CompanyId == userCompanyId || (t.Project != null && t.Project.CompanyId == userCompanyId));
            }

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

            var currentUser = await _userManager.GetUserAsync(User);
            var isAdmin = User.IsInRole("Admin");
            var userCompanyId = currentUser?.CompanyId;

            var sessionsQuery = _db.Sessions
                .Include(s => s.Task)
                    .ThenInclude(t => t!.Project)
                .Where(s => s.StartTime >= start && s.EndTime != null)
                .AsQueryable();

            if (!isAdmin)
            {
                sessionsQuery = sessionsQuery.Where(s => s.Task != null && (s.Task.CompanyId == userCompanyId || (s.Task.Project != null && s.Task.Project.CompanyId == userCompanyId)));
            }

            var sessions = await sessionsQuery.ToListAsync();

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

        // ── EXPORT EXCEL REKAPITULASI PRODUKTIVITAS TIM & PROYEK ─────
        [HttpGet]
        public async Task<IActionResult> ExportProductivityExcel(int? projectId)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var isAdmin = User.IsInRole("Admin");
            var userCompanyId = currentUser?.CompanyId;

            var tasksQuery = _db.Tasks
                .Include(t => t.Project)
                .Include(t => t.AssignedToUser)
                .Include(t => t.Sessions)
                .AsQueryable();

            var projectsQuery = _db.Projects
                .Include(p => p.Tasks)
                    .ThenInclude(t => t.Sessions)
                .AsQueryable();

            var usersQuery = _db.Users.AsQueryable();

            if (!isAdmin)
            {
                tasksQuery = tasksQuery.Where(t => t.CompanyId == userCompanyId || (t.Project != null && t.Project.CompanyId == userCompanyId));
                projectsQuery = projectsQuery.Where(p => p.CompanyId == userCompanyId);
                usersQuery = usersQuery.Where(u => u.CompanyId == userCompanyId);
            }

            if (projectId.HasValue)
            {
                tasksQuery = tasksQuery.Where(t => t.ProjectId == projectId.Value);
                projectsQuery = projectsQuery.Where(p => p.Id == projectId.Value);
            }

            var projects = await projectsQuery.OrderBy(p => p.Name).ToListAsync();
            var userList = await usersQuery.OrderBy(u => u.FullName).ToListAsync();
            var tasks = await tasksQuery.ToListAsync();

            using var wb = new XLWorkbook();

            // ══════════════════════════════════════════════════════════
            // SHEET 1: REKAPITULASI PROYEK
            // ══════════════════════════════════════════════════════════
            var wsProjects = wb.Worksheets.Add("Rekap Proyek");
            wsProjects.ShowGridLines = true;

            wsProjects.Cell(1, 1).Value = "LAPORAN REKAPITULASI PROYEK & PRODUKTIVITAS";
            wsProjects.Range(1, 1, 1, 9).Merge();
            wsProjects.Cell(1, 1).Style.Font.Bold = true;
            wsProjects.Cell(1, 1).Style.Font.FontSize = 14;
            wsProjects.Cell(1, 1).Style.Font.FontColor = XLColor.White;
            wsProjects.Cell(1, 1).Style.Fill.BackgroundColor = XLColor.FromArgb(49, 46, 129); // Indigo-900
            wsProjects.Cell(1, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            wsProjects.Row(1).Height = 32;

            wsProjects.Cell(2, 1).Value = $"Dicetak pada: {DateTime.Now:dd MMMM yyyy HH:mm} WIB | Oleh: {currentUser?.FullName ?? "Pengguna"}";
            wsProjects.Range(2, 1, 2, 9).Merge();
            wsProjects.Cell(2, 1).Style.Font.FontSize = 10;
            wsProjects.Cell(2, 1).Style.Font.FontColor = XLColor.FromArgb(199, 210, 254);
            wsProjects.Cell(2, 1).Style.Fill.BackgroundColor = XLColor.FromArgb(67, 56, 202); // Indigo-700
            wsProjects.Cell(2, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            wsProjects.Row(2).Height = 20;

            var projHeaders = new[] { "No", "Kode Proyek", "Nama Proyek", "Status", "Total Tugas", "Selesai", "In Progress", "Overdue", "Jam Kerja" };
            for (int i = 0; i < projHeaders.Length; i++)
            {
                var cell = wsProjects.Cell(4, i + 1);
                cell.Value = projHeaders[i];
                cell.Style.Font.Bold = true;
                cell.Style.Font.FontSize = 10;
                cell.Style.Font.FontColor = XLColor.White;
                cell.Style.Fill.BackgroundColor = XLColor.FromArgb(79, 70, 229); // Indigo-600
                cell.Style.Alignment.Horizontal = i == 2 ? XLAlignmentHorizontalValues.Left : XLAlignmentHorizontalValues.Center;
                cell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                cell.Style.Border.OutsideBorderColor = XLColor.FromArgb(49, 46, 129);
            }
            wsProjects.Row(4).Height = 24;

            int row = 5;
            int no = 1;
            var now = DateTime.Now;

            foreach (var p in projects)
            {
                var pTasks = p.Tasks?.ToList() ?? new List<WorkTask>();
                var totalPTasks = pTasks.Count;
                var donePTasks = pTasks.Count(t => t.Status == Models.TaskStatus.Done);
                var inProgPTasks = pTasks.Count(t => t.Status == Models.TaskStatus.InProgress);
                var overduePTasks = pTasks.Count(t => t.DueDate.HasValue && t.DueDate.Value < now && t.Status != Models.TaskStatus.Done);
                var totalHours = pTasks.SelectMany(t => t.Sessions ?? new List<WorkSession>())
                                       .Where(s => s.EndTime != null)
                                       .Sum(s => s.Duration) / 3600.0;

                wsProjects.Cell(row, 1).Value = no++;
                wsProjects.Cell(row, 2).Value = $"PRJ-{p.Id:D3}";
                wsProjects.Cell(row, 3).Value = p.Name;
                wsProjects.Cell(row, 4).Value = p.Status.ToString();
                wsProjects.Cell(row, 5).Value = totalPTasks;
                wsProjects.Cell(row, 6).Value = donePTasks;
                wsProjects.Cell(row, 7).Value = inProgPTasks;
                wsProjects.Cell(row, 8).Value = overduePTasks;
                wsProjects.Cell(row, 9).Value = Math.Round(totalHours, 1);

                wsProjects.Cell(row, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                wsProjects.Cell(row, 2).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                wsProjects.Cell(row, 4).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                wsProjects.Cell(row, 5).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                wsProjects.Cell(row, 6).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                wsProjects.Cell(row, 7).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                wsProjects.Cell(row, 8).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                wsProjects.Cell(row, 9).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
                wsProjects.Cell(row, 9).Style.NumberFormat.Format = "#,##0.0";

                var rowRange = wsProjects.Range(row, 1, row, 9);
                rowRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                rowRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
                rowRange.Style.Border.OutsideBorderColor = XLColor.FromArgb(226, 232, 240);
                rowRange.Style.Border.InsideBorderColor = XLColor.FromArgb(226, 232, 240);
                if (row % 2 == 0)
                {
                    rowRange.Style.Fill.BackgroundColor = XLColor.FromArgb(248, 250, 252);
                }
                row++;
            }

            if (projects.Count > 0)
            {
                wsProjects.Cell(row, 1).Value = "TOTAL";
                wsProjects.Range(row, 1, row, 4).Merge();
                wsProjects.Cell(row, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                wsProjects.Cell(row, 1).Style.Font.Bold = true;

                wsProjects.Cell(row, 5).FormulaA1 = $"=SUM(E5:E{row - 1})";
                wsProjects.Cell(row, 6).FormulaA1 = $"=SUM(F5:F{row - 1})";
                wsProjects.Cell(row, 7).FormulaA1 = $"=SUM(G5:G{row - 1})";
                wsProjects.Cell(row, 8).FormulaA1 = $"=SUM(H5:H{row - 1})";
                wsProjects.Cell(row, 9).FormulaA1 = $"=SUM(I5:I{row - 1})";

                for (int c = 5; c <= 8; c++)
                {
                    wsProjects.Cell(row, c).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    wsProjects.Cell(row, c).Style.Font.Bold = true;
                }
                wsProjects.Cell(row, 9).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
                wsProjects.Cell(row, 9).Style.Font.Bold = true;
                wsProjects.Cell(row, 9).Style.NumberFormat.Format = "#,##0.0";

                var totRange = wsProjects.Range(row, 1, row, 9);
                totRange.Style.Fill.BackgroundColor = XLColor.FromArgb(238, 242, 255);
                totRange.Style.Border.OutsideBorder = XLBorderStyleValues.Medium;
                totRange.Style.Border.OutsideBorderColor = XLColor.FromArgb(99, 102, 241);
                totRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
            }

            wsProjects.Columns().AdjustToContents();

            // ══════════════════════════════════════════════════════════
            // SHEET 2: PRODUKTIVITAS ANGGOTA TIM
            // ══════════════════════════════════════════════════════════
            var wsUsers = wb.Worksheets.Add("Produktivitas Anggota");
            wsUsers.ShowGridLines = true;

            wsUsers.Cell(1, 1).Value = "PRODUKTIVITAS & KINERJA ANGGOTA TIM";
            wsUsers.Range(1, 1, 1, 9).Merge();
            wsUsers.Cell(1, 1).Style.Font.Bold = true;
            wsUsers.Cell(1, 1).Style.Font.FontSize = 14;
            wsUsers.Cell(1, 1).Style.Font.FontColor = XLColor.White;
            wsUsers.Cell(1, 1).Style.Fill.BackgroundColor = XLColor.FromArgb(15, 23, 42); // Slate-900
            wsUsers.Cell(1, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            wsUsers.Row(1).Height = 32;

            wsUsers.Cell(2, 1).Value = $"Dicetak pada: {DateTime.Now:dd MMMM yyyy HH:mm} WIB | Total Anggota: {userList.Count}";
            wsUsers.Range(2, 1, 2, 9).Merge();
            wsUsers.Cell(2, 1).Style.Font.FontSize = 10;
            wsUsers.Cell(2, 1).Style.Font.FontColor = XLColor.FromArgb(203, 213, 225);
            wsUsers.Cell(2, 1).Style.Fill.BackgroundColor = XLColor.FromArgb(51, 65, 85); // Slate-700
            wsUsers.Cell(2, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            wsUsers.Row(2).Height = 20;

            var userHeaders = new[] { "No", "Nama Lengkap", "Email", "Total Tugas", "Selesai", "In Progress", "Overdue", "Jam Kerja", "Tingkat Selesai (%)" };
            for (int i = 0; i < userHeaders.Length; i++)
            {
                var cell = wsUsers.Cell(4, i + 1);
                cell.Value = userHeaders[i];
                cell.Style.Font.Bold = true;
                cell.Style.Font.FontSize = 10;
                cell.Style.Font.FontColor = XLColor.White;
                cell.Style.Fill.BackgroundColor = XLColor.FromArgb(30, 41, 59); // Slate-800
                cell.Style.Alignment.Horizontal = i == 1 || i == 2 ? XLAlignmentHorizontalValues.Left : XLAlignmentHorizontalValues.Center;
                cell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                cell.Style.Border.OutsideBorderColor = XLColor.FromArgb(15, 23, 42);
            }
            wsUsers.Row(4).Height = 24;

            int uRow = 5;
            int uNo = 1;

            foreach (var u in userList)
            {
                var uTasks = tasks.Where(t => t.AssignedToUserId == u.Id).ToList();
                var totalUTasks = uTasks.Count;
                var doneUTasks = uTasks.Count(t => t.Status == Models.TaskStatus.Done);
                var inProgUTasks = uTasks.Count(t => t.Status == Models.TaskStatus.InProgress);
                var overdueUTasks = uTasks.Count(t => t.DueDate.HasValue && t.DueDate.Value < now && t.Status != Models.TaskStatus.Done);
                var uHours = uTasks.SelectMany(t => t.Sessions ?? new List<WorkSession>())
                                   .Where(s => s.EndTime != null && s.UserId == u.Id)
                                   .Sum(s => s.Duration) / 3600.0;
                var compPct = totalUTasks > 0 ? (double)doneUTasks / totalUTasks : 0.0;

                wsUsers.Cell(uRow, 1).Value = uNo++;
                wsUsers.Cell(uRow, 2).Value = u.FullName;
                wsUsers.Cell(uRow, 3).Value = u.Email;
                wsUsers.Cell(uRow, 4).Value = totalUTasks;
                wsUsers.Cell(uRow, 5).Value = doneUTasks;
                wsUsers.Cell(uRow, 6).Value = inProgUTasks;
                wsUsers.Cell(uRow, 7).Value = overdueUTasks;
                wsUsers.Cell(uRow, 8).Value = Math.Round(uHours, 1);
                wsUsers.Cell(uRow, 9).Value = compPct;

                wsUsers.Cell(uRow, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                wsUsers.Cell(uRow, 4).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                wsUsers.Cell(uRow, 5).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                wsUsers.Cell(uRow, 6).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                wsUsers.Cell(uRow, 7).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                wsUsers.Cell(uRow, 8).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
                wsUsers.Cell(uRow, 8).Style.NumberFormat.Format = "#,##0.0";
                wsUsers.Cell(uRow, 9).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
                wsUsers.Cell(uRow, 9).Style.NumberFormat.Format = "0.0%";

                var uRowRange = wsUsers.Range(uRow, 1, uRow, 9);
                uRowRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                uRowRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
                uRowRange.Style.Border.OutsideBorderColor = XLColor.FromArgb(226, 232, 240);
                uRowRange.Style.Border.InsideBorderColor = XLColor.FromArgb(226, 232, 240);
                if (uRow % 2 == 0)
                {
                    uRowRange.Style.Fill.BackgroundColor = XLColor.FromArgb(248, 250, 252);
                }
                uRow++;
            }

            wsUsers.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            wb.SaveAs(stream);
            var content = stream.ToArray();
            var excelFilename = $"laporan-produktivitas-{DateTime.Now:yyyyMMdd-HHmm}.xlsx";
            return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", excelFilename);
        }
    }
}
