using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TrackerKerja.Data;
using TrackerKerja.Models;
using TrackerKerja.ViewModels;

namespace TrackerKerja.Controllers
{
    [Authorize]
    public class TimesheetController : Controller
    {
        private readonly AppDbContext _db;
        private readonly UserManager<AppUser> _userManager;

        public TimesheetController(AppDbContext db, UserManager<AppUser> userManager)
        {
            _db = db;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index(DateTime? weekDate, int? projectId, string? memberId)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var isAdmin = User.IsInRole("Admin");

            // Non-admin can only see their own tasks & timesheets
            if (!isAdmin)
            {
                memberId = currentUser?.Id;
            }
            else if (string.IsNullOrEmpty(memberId) && currentUser != null)
            {
                // Default admin view to their own tasks unless specifically selected
                memberId = currentUser.Id;
            }

            var targetDate = weekDate ?? DateTime.Today;
            // Monday of the week
            int diff = (7 + (targetDate.DayOfWeek - DayOfWeek.Monday)) % 7;
            var weekStart = targetDate.AddDays(-1 * diff).Date;
            var weekEnd = weekStart.AddDays(7).AddTicks(-1);

            var query = _db.Sessions
                .Include(s => s.Task)
                    .ThenInclude(t => t!.Project)
                .Include(s => s.Task)
                    .ThenInclude(t => t!.Category)
                .Include(s => s.Task)
                    .ThenInclude(t => t!.AssignedToUser)
                .Where(s => s.StartTime >= weekStart && s.StartTime <= weekEnd && s.EndTime != null)
                .AsQueryable();

            if (projectId.HasValue)
            {
                query = query.Where(s => s.Task != null && s.Task.ProjectId == projectId.Value);
            }

            if (!string.IsNullOrEmpty(memberId) && memberId != "all")
            {
                query = query.Where(s => s.Task != null && s.Task.AssignedToUserId == memberId);
            }

            var sessions = await query.OrderByDescending(s => s.StartTime).ToListAsync();

            // Load tasks assigned to the user for quick modal
            var userTasksQuery = _db.Tasks
                .Include(t => t.Project)
                .Include(t => t.Sessions)
                .AsQueryable();
            if (!isAdmin || (!string.IsNullOrEmpty(memberId) && memberId != "all"))
            {
                var targetUserId = !string.IsNullOrEmpty(memberId) ? memberId : currentUser?.Id;
                userTasksQuery = userTasksQuery.Where(t => t.AssignedToUserId == targetUserId);
            }

            var viewModel = new TimesheetViewModel
            {
                WeekStart = weekStart,
                WeekEnd = weekStart.AddDays(6),
                SelectedProjectId = projectId,
                SelectedMemberId = memberId,
                AllProjects = await _db.Projects.OrderBy(p => p.Name).ToListAsync(),
                AllMembers = await _db.Users.OrderBy(u => u.FullName).ToListAsync(),
                UserTasks = await userTasksQuery.OrderBy(t => t.Title).ToListAsync(),
                RecentSessions = sessions
            };

            for (int i = 0; i < 7; i++)
            {
                viewModel.DayDates[i] = weekStart.AddDays(i);
            }

            // Group sessions by Task
            var groupedByTask = sessions
                .Where(s => s.Task != null)
                .GroupBy(s => s.Task!)
                .ToList();

            foreach (var group in groupedByTask)
            {
                var task = group.Key;
                var row = new TimesheetTaskRowDto
                {
                    TaskId = task.Id,
                    TaskTitle = task.Title,
                    ProjectName = task.Project?.Name,
                    CategoryName = task.Category?.Name,
                    AssigneeName = task.AssignedToUser?.FullName,
                    AssigneeAvatar = task.AssignedToUser?.ProfilePictureUrl,
                    Status = task.Status.ToString(),
                    Progress = task.Progress
                };

                for (int i = 0; i < 7; i++)
                {
                    var currentDay = weekStart.AddDays(i).Date;
                    var daySeconds = group
                        .Where(s => s.StartTime.Date == currentDay)
                        .Sum(s => s.Duration);

                    var hours = Math.Round(daySeconds / 3600.0, 2);
                    row.DailyHours[i] = hours;
                    viewModel.DayTotals[i] += hours;
                }

                viewModel.TaskRows.Add(row);
            }

            viewModel.TotalWeekHours = Math.Round(viewModel.DayTotals.Sum(), 2);
            viewModel.TotalSessionsCount = sessions.Count;
            viewModel.ActiveTasksCount = viewModel.TaskRows.Count;

            // Cut-off timesheet reminder (Tgl 25 setiap bulannya)
            var now = DateTime.Now;
            viewModel.IsApproachingCutoff = now.Day >= 18 && now.Day <= 25;
            viewModel.CutoffDaysLeft = Math.Max(0, 25 - now.Day);
            viewModel.CutoffDate = new DateTime(now.Year, now.Month, 25);
            viewModel.TasksMissingTimesheet = viewModel.UserTasks
                .Where(t => t.Status != Models.TaskStatus.Done && (t.Sessions == null || !t.Sessions.Any(s => s.Duration > 0)))
                .ToList();

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddSession(int taskId, DateTime sessionDate, int hours, int minutes, string? notes)
        {
            var task = await _db.Tasks.FindAsync(taskId);
            if (task == null) return NotFound();

            var totalSeconds = (hours * 3600) + (minutes * 60);
            if (totalSeconds <= 0)
            {
                TempData["Error"] = "Durasi waktu harus lebih dari 0.";
                return RedirectToAction("Index");
            }

            var startTime = sessionDate.Date.AddHours(9); // Default start 09:00 AM
            var endTime = startTime.AddSeconds(totalSeconds);

            var session = new WorkSession
            {
                TaskId = taskId,
                StartTime = startTime,
                EndTime = endTime,
                Duration = totalSeconds,
                Notes = string.IsNullOrWhiteSpace(notes) ? "Input Timesheet Manual" : notes.Trim()
            };

            _db.Sessions.Add(session);
            task.UpdatedAt = DateTime.Now;
            await _db.SaveChangesAsync();

            TempData["Success"] = $"Berhasil mencatat {hours} jam {minutes} menit untuk tugas '{task.Title}'.";
            return RedirectToAction("Index", new { weekDate = sessionDate.ToString("yyyy-MM-dd") });
        }

        [HttpGet]
        public async Task<IActionResult> ExportCsv(DateTime? weekDate, int? projectId, string? memberId)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var isAdmin = User.IsInRole("Admin");

            if (!isAdmin)
            {
                memberId = currentUser?.Id;
            }
            else if (string.IsNullOrEmpty(memberId) && currentUser != null)
            {
                memberId = currentUser.Id;
            }

            var targetDate = weekDate ?? DateTime.Today;
            int diff = (7 + (targetDate.DayOfWeek - DayOfWeek.Monday)) % 7;
            var weekStart = targetDate.AddDays(-1 * diff).Date;
            var weekEnd = weekStart.AddDays(7).AddTicks(-1);

            var query = _db.Sessions
                .Include(s => s.Task)
                    .ThenInclude(t => t!.Project)
                .Include(s => s.Task)
                    .ThenInclude(t => t!.AssignedToUser)
                .Where(s => s.StartTime >= weekStart && s.StartTime <= weekEnd && s.EndTime != null)
                .AsQueryable();

            if (projectId.HasValue) query = query.Where(s => s.Task != null && s.Task.ProjectId == projectId.Value);
            if (!string.IsNullOrEmpty(memberId) && memberId != "all") query = query.Where(s => s.Task != null && s.Task.AssignedToUserId == memberId);

            var sessions = await query.OrderBy(s => s.StartTime).ToListAsync();

            var sb = new StringBuilder();
            sb.AppendLine("Tanggal,Waktu Mulai,Waktu Selesai,Durasi (Jam),Nama Tugas,Project,PIC,Catatan");

            foreach (var s in sessions)
            {
                var durationHours = Math.Round(s.Duration / 3600.0, 2);
                sb.AppendLine($"\"{s.StartTime:yyyy-MM-dd}\",\"{s.StartTime:HH:mm:ss}\",\"{s.EndTime:HH:mm:ss}\",\"{durationHours}\",\"{s.Task?.Title?.Replace("\"", "\"\"")}\",\"{s.Task?.Project?.Name?.Replace("\"", "\"\"")}\",\"{s.Task?.AssignedToUser?.FullName?.Replace("\"", "\"\"")}\",\"{s.Notes?.Replace("\"", "\"\"")}\"");
            }

            var bytes = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
            return File(bytes, "text/csv", $"Timesheet_{weekStart:yyyyMMdd}_{weekStart.AddDays(6):yyyyMMdd}.csv");
        }

        // ── EXPORT PERSONAL TIMESHEET REPORT TO EXCEL (.xlsx) ──────
        [HttpGet]
        public async Task<IActionResult> ExportPersonalExcel(DateTime? startDate, DateTime? endDate, int? projectId, string? preset)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return Challenge();

            // Resolve date range based on preset or custom dates
            DateTime start;
            DateTime end;
            var today = DateTime.Today;

            if (preset == "this_week")
            {
                int diff = (7 + (today.DayOfWeek - DayOfWeek.Monday)) % 7;
                start = today.AddDays(-1 * diff).Date;
                end = start.AddDays(6).Date.AddHours(23).AddMinutes(59).AddSeconds(59);
            }
            else if (preset == "last_week")
            {
                int diff = (7 + (today.DayOfWeek - DayOfWeek.Monday)) % 7;
                start = today.AddDays(-1 * diff - 7).Date;
                end = start.AddDays(6).Date.AddHours(23).AddMinutes(59).AddSeconds(59);
            }
            else if (preset == "this_month")
            {
                start = new DateTime(today.Year, today.Month, 1);
                end = start.AddMonths(1).AddDays(-1).Date.AddHours(23).AddMinutes(59).AddSeconds(59);
            }
            else if (preset == "last_month")
            {
                var prevMonth = today.AddMonths(-1);
                start = new DateTime(prevMonth.Year, prevMonth.Month, 1);
                end = start.AddMonths(1).AddDays(-1).Date.AddHours(23).AddMinutes(59).AddSeconds(59);
            }
            else if (preset == "all_time")
            {
                start = new DateTime(2020, 1, 1);
                end = today.AddHours(23).AddMinutes(59).AddSeconds(59);
            }
            else
            {
                // Custom date or default to current month
                start = startDate?.Date ?? new DateTime(today.Year, today.Month, 1);
                end = endDate.HasValue 
                    ? endDate.Value.Date.AddHours(23).AddMinutes(59).AddSeconds(59) 
                    : today.AddHours(23).AddMinutes(59).AddSeconds(59);
            }

            if (end < start)
            {
                end = start.AddHours(23).AddMinutes(59).AddSeconds(59);
            }

            // STRICT PERSONAL ISOLATION: Only query sessions associated with tasks assigned to the logged-in user
            var query = _db.Sessions
                .Include(s => s.Task)
                    .ThenInclude(t => t!.Project)
                .Include(s => s.Task)
                    .ThenInclude(t => t!.Category)
                .Include(s => s.Task)
                    .ThenInclude(t => t!.AssignedToUser)
                .Where(s => s.StartTime >= start && s.StartTime <= end && s.EndTime != null)
                .Where(s => s.Task != null && s.Task.AssignedToUserId == currentUser.Id)
                .AsQueryable();

            string filterProjectName = "Semua Proyek";
            if (projectId.HasValue)
            {
                query = query.Where(s => s.Task != null && s.Task.ProjectId == projectId.Value);
                var proj = await _db.Projects.FindAsync(projectId.Value);
                if (proj != null) filterProjectName = proj.Name;
            }

            var sessions = await query.OrderBy(s => s.StartTime).ToListAsync();

            using var wb = new ClosedXML.Excel.XLWorkbook();

            // ══════════════════════════════════════════════════════
            // SHEET 1: DETAIL SESI TIMESHEET
            // ══════════════════════════════════════════════════════
            var ws = wb.Worksheets.Add("Timesheet Personal");
            ws.ShowGridLines = true;

            // 1. Header Banner
            ws.Range("A1:N1").Merge();
            var titleCell = ws.Cell("A1");
            titleCell.Value = "LAPORAN TIMESHEET KERJA PERSONAL";
            titleCell.Style.Font.Bold = true;
            titleCell.Style.Font.FontSize = 16;
            titleCell.Style.Font.FontColor = ClosedXML.Excel.XLColor.White;
            titleCell.Style.Fill.BackgroundColor = ClosedXML.Excel.XLColor.FromArgb(49, 46, 129); // Deep Indigo
            titleCell.Style.Alignment.Horizontal = ClosedXML.Excel.XLAlignmentHorizontalValues.Center;
            titleCell.Style.Alignment.Vertical = ClosedXML.Excel.XLAlignmentVerticalValues.Center;
            ws.Row(1).Height = 35;

            ws.Range("A2:N2").Merge();
            var subTitleCell = ws.Cell("A2");
            subTitleCell.Value = "Work Tracker Pro • Rekapitulasi Aktivitas & Durasi Kerja Personal";
            subTitleCell.Style.Font.FontSize = 10;
            subTitleCell.Style.Font.Italic = true;
            subTitleCell.Style.Font.FontColor = ClosedXML.Excel.XLColor.FromArgb(224, 231, 255);
            subTitleCell.Style.Fill.BackgroundColor = ClosedXML.Excel.XLColor.FromArgb(67, 56, 202);
            subTitleCell.Style.Alignment.Horizontal = ClosedXML.Excel.XLAlignmentHorizontalValues.Center;
            ws.Row(2).Height = 20;

            // 2. Metadata Info Box (Rows 4-6)
            ws.Cell("A4").Value = "Nama Karyawan";
            ws.Cell("A4").Style.Font.Bold = true;
            ws.Cell("B4").Value = currentUser.FullName;
            ws.Range("B4:D4").Merge();

            ws.Cell("E4").Value = "Email Akun";
            ws.Cell("E4").Style.Font.Bold = true;
            ws.Cell("F4").Value = currentUser.Email;
            ws.Range("F4:H4").Merge();

            ws.Cell("I4").Value = "Periode Laporan";
            ws.Cell("I4").Style.Font.Bold = true;
            ws.Cell("J4").Value = $"{start:dd MMM yyyy} s/d {end:dd MMM yyyy}";
            ws.Range("J4:N4").Merge();

            ws.Cell("A5").Value = "Jabatan (Job Title)";
            ws.Cell("A5").Style.Font.Bold = true;
            ws.Cell("B5").Value = string.IsNullOrEmpty(currentUser.JobTitle) ? "-" : currentUser.JobTitle;
            ws.Range("B5:D5").Merge();

            ws.Cell("E5").Value = "Filter Proyek";
            ws.Cell("E5").Style.Font.Bold = true;
            ws.Cell("F5").Value = filterProjectName;
            ws.Range("F5:H5").Merge();

            ws.Cell("I5").Value = "Tanggal Generate";
            ws.Cell("I5").Style.Font.Bold = true;
            ws.Cell("J5").Value = DateTime.Now.ToString("dd MMMM yyyy HH:mm:ss");
            ws.Range("J5:N5").Merge();

            double totalSecondsAll = sessions.Sum(s => s.Duration);
            double totalHoursAll = Math.Round(totalSecondsAll / 3600.0, 2);
            var totalTimeSpan = TimeSpan.FromSeconds(totalSecondsAll);
            string totalFormatted = $"{(int)totalTimeSpan.TotalHours:D2}:{totalTimeSpan.Minutes:D2}:{totalTimeSpan.Seconds:D2}";

            ws.Cell("A6").Value = "Total Sesi Kerja";
            ws.Cell("A6").Style.Font.Bold = true;
            ws.Cell("B6").Value = $"{sessions.Count} Sesi";
            ws.Range("B6:D6").Merge();

            ws.Cell("E6").Value = "Total Jam Kerja";
            ws.Cell("E6").Style.Font.Bold = true;
            ws.Cell("F6").Value = $"{totalHoursAll:F2} Jam ({totalFormatted})";
            ws.Cell("F6").Style.Font.Bold = true;
            ws.Cell("F6").Style.Font.FontColor = ClosedXML.Excel.XLColor.FromArgb(67, 56, 202);
            ws.Range("F6:H6").Merge();

            var metaRange = ws.Range("A4:N6");
            metaRange.Style.Border.OutsideBorder = ClosedXML.Excel.XLBorderStyleValues.Thin;
            metaRange.Style.Border.OutsideBorderColor = ClosedXML.Excel.XLColor.FromArgb(199, 210, 254);
            metaRange.Style.Fill.BackgroundColor = ClosedXML.Excel.XLColor.FromArgb(248, 250, 252);
            metaRange.Style.Font.FontSize = 10;

            // 3. Table Column Headers (Row 8)
            var headers = new[]
            {
                "No",               // A
                "Tanggal",          // B
                "Hari",             // C
                "Kode Tugas",       // D
                "Nama Tugas",       // E
                "Proyek",           // F
                "Kategori",         // G
                "Status Tugas",     // H
                "Progress (%)",     // I
                "Jam Mulai",        // J
                "Jam Selesai",      // K
                "Durasi (Jam)",     // L
                "Durasi (Format)",  // M
                "Catatan Pekerjaan" // N
            };

            const int headerRow = 8;
            ws.Row(headerRow).Height = 26;

            for (int i = 0; i < headers.Length; i++)
            {
                var cell = ws.Cell(headerRow, i + 1);
                cell.Value = headers[i];
                cell.Style.Font.Bold = true;
                cell.Style.Font.FontSize = 10;
                cell.Style.Font.FontColor = ClosedXML.Excel.XLColor.White;
                cell.Style.Fill.BackgroundColor = ClosedXML.Excel.XLColor.FromArgb(67, 56, 202); // Indigo-700
                cell.Style.Alignment.Horizontal = (i == 0 || i == 1 || i == 2 || i == 3 || i == 7 || i == 8 || i == 9 || i == 10 || i == 12)
                    ? ClosedXML.Excel.XLAlignmentHorizontalValues.Center
                    : (i == 11 ? ClosedXML.Excel.XLAlignmentHorizontalValues.Right : ClosedXML.Excel.XLAlignmentHorizontalValues.Left);
                cell.Style.Alignment.Vertical = ClosedXML.Excel.XLAlignmentVerticalValues.Center;
                cell.Style.Border.OutsideBorder = ClosedXML.Excel.XLBorderStyleValues.Thin;
                cell.Style.Border.OutsideBorderColor = ClosedXML.Excel.XLColor.FromArgb(49, 46, 129);
            }

            // 4. Data Rows (Row 9+)
            int currentRow = 9;
            string[] dayNamesIndo = { "Minggu", "Senin", "Selasa", "Rabu", "Kamis", "Jumat", "Sabtu" };

            if (!sessions.Any())
            {
                ws.Range(currentRow, 1, currentRow, headers.Length).Merge();
                var emptyCell = ws.Cell(currentRow, 1);
                emptyCell.Value = "Tidak ada rekaman sesi kerja pada periode dan filter yang dipilih.";
                emptyCell.Style.Alignment.Horizontal = ClosedXML.Excel.XLAlignmentHorizontalValues.Center;
                emptyCell.Style.Font.Italic = true;
                emptyCell.Style.Font.FontColor = ClosedXML.Excel.XLColor.FromArgb(148, 163, 184);
                emptyCell.Style.Fill.BackgroundColor = ClosedXML.Excel.XLColor.FromArgb(248, 250, 252);
                ws.Row(currentRow).Height = 25;
                currentRow++;
            }
            else
            {
                int no = 1;
                foreach (var s in sessions)
                {
                    var durHours = Math.Round(s.Duration / 3600.0, 2);
                    var durSpan = TimeSpan.FromSeconds(s.Duration);
                    var durFormatted = $"{(int)durSpan.TotalHours:D2}:{durSpan.Minutes:D2}:{durSpan.Seconds:D2}";
                    var dayIndo = dayNamesIndo[(int)s.StartTime.DayOfWeek];

                    ws.Cell(currentRow, 1).Value = no;
                    ws.Cell(currentRow, 2).Value = s.StartTime.ToString("yyyy-MM-dd");
                    ws.Cell(currentRow, 3).Value = dayIndo;
                    ws.Cell(currentRow, 4).Value = s.Task?.TaskCode ?? "-";
                    ws.Cell(currentRow, 5).Value = s.Task?.Title ?? "-";
                    ws.Cell(currentRow, 6).Value = s.Task?.Project?.Name ?? "-";
                    ws.Cell(currentRow, 7).Value = s.Task?.Category?.Name ?? "-";
                    ws.Cell(currentRow, 8).Value = s.Task?.Status.ToString() ?? "-";
                    
                    var progressCell = ws.Cell(currentRow, 9);
                    progressCell.Value = (s.Task?.Progress ?? 0) / 100.0;
                    progressCell.Style.NumberFormat.Format = "0%";

                    ws.Cell(currentRow, 10).Value = s.StartTime.ToString("HH:mm:ss");
                    ws.Cell(currentRow, 11).Value = s.EndTime?.ToString("HH:mm:ss") ?? "-";
                    
                    var durHoursCell = ws.Cell(currentRow, 12);
                    durHoursCell.Value = durHours;
                    durHoursCell.Style.NumberFormat.Format = "#,##0.00";

                    ws.Cell(currentRow, 13).Value = durFormatted;
                    ws.Cell(currentRow, 14).Value = string.IsNullOrEmpty(s.Notes) ? "-" : s.Notes;

                    // Alignments
                    ws.Cell(currentRow, 1).Style.Alignment.Horizontal = ClosedXML.Excel.XLAlignmentHorizontalValues.Center;
                    ws.Cell(currentRow, 2).Style.Alignment.Horizontal = ClosedXML.Excel.XLAlignmentHorizontalValues.Center;
                    ws.Cell(currentRow, 3).Style.Alignment.Horizontal = ClosedXML.Excel.XLAlignmentHorizontalValues.Center;
                    ws.Cell(currentRow, 4).Style.Alignment.Horizontal = ClosedXML.Excel.XLAlignmentHorizontalValues.Center;
                    ws.Cell(currentRow, 8).Style.Alignment.Horizontal = ClosedXML.Excel.XLAlignmentHorizontalValues.Center;
                    ws.Cell(currentRow, 9).Style.Alignment.Horizontal = ClosedXML.Excel.XLAlignmentHorizontalValues.Center;
                    ws.Cell(currentRow, 10).Style.Alignment.Horizontal = ClosedXML.Excel.XLAlignmentHorizontalValues.Center;
                    ws.Cell(currentRow, 11).Style.Alignment.Horizontal = ClosedXML.Excel.XLAlignmentHorizontalValues.Center;
                    ws.Cell(currentRow, 12).Style.Alignment.Horizontal = ClosedXML.Excel.XLAlignmentHorizontalValues.Right;
                    ws.Cell(currentRow, 13).Style.Alignment.Horizontal = ClosedXML.Excel.XLAlignmentHorizontalValues.Center;

                    // Zebra striping
                    var rowRange = ws.Range(currentRow, 1, currentRow, headers.Length);
                    rowRange.Style.Font.FontSize = 9.5;
                    rowRange.Style.Border.OutsideBorder = ClosedXML.Excel.XLBorderStyleValues.Thin;
                    rowRange.Style.Border.InsideBorder = ClosedXML.Excel.XLBorderStyleValues.Thin;
                    rowRange.Style.Border.OutsideBorderColor = ClosedXML.Excel.XLColor.FromArgb(226, 232, 240);
                    rowRange.Style.Border.InsideBorderColor = ClosedXML.Excel.XLColor.FromArgb(226, 232, 240);

                    if (no % 2 == 0)
                    {
                        rowRange.Style.Fill.BackgroundColor = ClosedXML.Excel.XLColor.FromArgb(248, 250, 252);
                    }

                    ws.Row(currentRow).Height = 22;
                    no++;
                    currentRow++;
                }

                // 5. Total Row
                int lastDataRow = currentRow - 1;
                ws.Range(currentRow, 1, currentRow, 11).Merge();
                var totalLabel = ws.Cell(currentRow, 1);
                totalLabel.Value = "TOTAL KESELURUHAN JAM KERJA";
                totalLabel.Style.Font.Bold = true;
                totalLabel.Style.Font.FontSize = 10;
                totalLabel.Style.Alignment.Horizontal = ClosedXML.Excel.XLAlignmentHorizontalValues.Right;
                totalLabel.Style.Alignment.Vertical = ClosedXML.Excel.XLAlignmentVerticalValues.Center;

                var totalHoursFormula = ws.Cell(currentRow, 12);
                totalHoursFormula.FormulaA1 = $"SUM(L9:L{lastDataRow})";
                totalHoursFormula.Style.Font.Bold = true;
                totalHoursFormula.Style.Font.FontSize = 10;
                totalHoursFormula.Style.NumberFormat.Format = "#,##0.00";
                totalHoursFormula.Style.Alignment.Horizontal = ClosedXML.Excel.XLAlignmentHorizontalValues.Right;

                var totalFormattedCell = ws.Cell(currentRow, 13);
                totalFormattedCell.Value = totalFormatted;
                totalFormattedCell.Style.Font.Bold = true;
                totalFormattedCell.Style.Font.FontSize = 10;
                totalFormattedCell.Style.Alignment.Horizontal = ClosedXML.Excel.XLAlignmentHorizontalValues.Center;

                ws.Cell(currentRow, 14).Value = "";

                var totalRowRange = ws.Range(currentRow, 1, currentRow, headers.Length);
                totalRowRange.Style.Fill.BackgroundColor = ClosedXML.Excel.XLColor.FromArgb(238, 242, 255); // Indigo-50
                totalRowRange.Style.Font.FontColor = ClosedXML.Excel.XLColor.FromArgb(49, 46, 129);
                totalRowRange.Style.Border.TopBorder = ClosedXML.Excel.XLBorderStyleValues.Medium;
                totalRowRange.Style.Border.TopBorderColor = ClosedXML.Excel.XLColor.FromArgb(67, 56, 202);
                totalRowRange.Style.Border.BottomBorder = ClosedXML.Excel.XLBorderStyleValues.Double;
                totalRowRange.Style.Border.BottomBorderColor = ClosedXML.Excel.XLColor.FromArgb(67, 56, 202);
                totalRowRange.Style.Border.InsideBorder = ClosedXML.Excel.XLBorderStyleValues.Thin;
                totalRowRange.Style.Border.InsideBorderColor = ClosedXML.Excel.XLColor.FromArgb(199, 210, 254);
                ws.Row(currentRow).Height = 26;
            }

            ws.Columns().AdjustToContents();
            ws.Column(1).Width = 6;   // No
            ws.Column(2).Width = 14;  // Tanggal
            ws.Column(3).Width = 11;  // Hari
            ws.Column(4).Width = 14;  // Kode Tugas
            ws.Column(5).Width = 32;  // Nama Tugas
            ws.Column(6).Width = 22;  // Proyek
            ws.Column(7).Width = 18;  // Kategori
            ws.Column(8).Width = 15;  // Status
            ws.Column(9).Width = 13;  // Progress
            ws.Column(10).Width = 12; // Mulai
            ws.Column(11).Width = 12; // Selesai
            ws.Column(12).Width = 15; // Durasi Jam
            ws.Column(13).Width = 16; // Durasi Format
            ws.Column(14).Width = 30; // Catatan

            // ══════════════════════════════════════════════════════
            // SHEET 2: REKAPITULASI PER PROYEK
            // ══════════════════════════════════════════════════════
            var wsSummary = wb.Worksheets.Add("Rekap per Proyek");
            wsSummary.ShowGridLines = true;

            wsSummary.Range("A1:E1").Merge();
            var sumTitle = wsSummary.Cell("A1");
            sumTitle.Value = "REKAPITULASI JAM KERJA PER PROYEK";
            sumTitle.Style.Font.Bold = true;
            sumTitle.Style.Font.FontSize = 14;
            sumTitle.Style.Font.FontColor = ClosedXML.Excel.XLColor.White;
            sumTitle.Style.Fill.BackgroundColor = ClosedXML.Excel.XLColor.FromArgb(49, 46, 129);
            sumTitle.Style.Alignment.Horizontal = ClosedXML.Excel.XLAlignmentHorizontalValues.Center;
            sumTitle.Style.Alignment.Vertical = ClosedXML.Excel.XLAlignmentVerticalValues.Center;
            wsSummary.Row(1).Height = 30;

            wsSummary.Range("A2:E2").Merge();
            wsSummary.Cell("A2").Value = $"Karyawan: {currentUser.FullName} | Periode: {start:dd MMM yyyy} - {end:dd MMM yyyy}";
            wsSummary.Cell("A2").Style.Font.Italic = true;
            wsSummary.Cell("A2").Style.Font.FontSize = 10;
            wsSummary.Cell("A2").Style.Font.FontColor = ClosedXML.Excel.XLColor.FromArgb(224, 231, 255);
            wsSummary.Cell("A2").Style.Fill.BackgroundColor = ClosedXML.Excel.XLColor.FromArgb(67, 56, 202);
            wsSummary.Cell("A2").Style.Alignment.Horizontal = ClosedXML.Excel.XLAlignmentHorizontalValues.Center;
            wsSummary.Row(2).Height = 20;

            var sumHeaders = new[] { "No", "Nama Proyek", "Total Sesi", "Total Durasi (Jam)", "Persentase Waktu (%)" };
            for (int i = 0; i < sumHeaders.Length; i++)
            {
                var cell = wsSummary.Cell(4, i + 1);
                cell.Value = sumHeaders[i];
                cell.Style.Font.Bold = true;
                cell.Style.Font.FontColor = ClosedXML.Excel.XLColor.White;
                cell.Style.Fill.BackgroundColor = ClosedXML.Excel.XLColor.FromArgb(67, 56, 202);
                cell.Style.Alignment.Horizontal = (i == 0) ? ClosedXML.Excel.XLAlignmentHorizontalValues.Center : (i >= 2 ? ClosedXML.Excel.XLAlignmentHorizontalValues.Right : ClosedXML.Excel.XLAlignmentHorizontalValues.Left);
                cell.Style.Border.OutsideBorder = ClosedXML.Excel.XLBorderStyleValues.Thin;
            }
            wsSummary.Row(4).Height = 24;

            var projectGroups = sessions
                .GroupBy(s => s.Task?.Project?.Name ?? "Tanpa Proyek")
                .OrderByDescending(g => g.Sum(s => s.Duration))
                .ToList();

            int sumRow = 5;
            int projNo = 1;

            if (!projectGroups.Any())
            {
                wsSummary.Range(sumRow, 1, sumRow, sumHeaders.Length).Merge();
                wsSummary.Cell(sumRow, 1).Value = "Tidak ada data.";
                wsSummary.Cell(sumRow, 1).Style.Alignment.Horizontal = ClosedXML.Excel.XLAlignmentHorizontalValues.Center;
                sumRow++;
            }
            else
            {
                foreach (var pg in projectGroups)
                {
                    double pSecs = pg.Sum(s => s.Duration);
                    double pHours = Math.Round(pSecs / 3600.0, 2);
                    double pPct = totalSecondsAll > 0 ? (pSecs / totalSecondsAll) : 0;

                    wsSummary.Cell(sumRow, 1).Value = projNo;
                    wsSummary.Cell(sumRow, 2).Value = pg.Key;
                    wsSummary.Cell(sumRow, 3).Value = pg.Count();
                    
                    var pHoursCell = wsSummary.Cell(sumRow, 4);
                    pHoursCell.Value = pHours;
                    pHoursCell.Style.NumberFormat.Format = "#,##0.00";

                    var pPctCell = wsSummary.Cell(sumRow, 5);
                    pPctCell.Value = pPct;
                    pPctCell.Style.NumberFormat.Format = "0.0%";

                    wsSummary.Cell(sumRow, 1).Style.Alignment.Horizontal = ClosedXML.Excel.XLAlignmentHorizontalValues.Center;
                    wsSummary.Cell(sumRow, 3).Style.Alignment.Horizontal = ClosedXML.Excel.XLAlignmentHorizontalValues.Right;
                    wsSummary.Cell(sumRow, 4).Style.Alignment.Horizontal = ClosedXML.Excel.XLAlignmentHorizontalValues.Right;
                    wsSummary.Cell(sumRow, 5).Style.Alignment.Horizontal = ClosedXML.Excel.XLAlignmentHorizontalValues.Right;

                    var pRowRange = wsSummary.Range(sumRow, 1, sumRow, sumHeaders.Length);
                    pRowRange.Style.Border.OutsideBorder = ClosedXML.Excel.XLBorderStyleValues.Thin;
                    pRowRange.Style.Border.InsideBorder = ClosedXML.Excel.XLBorderStyleValues.Thin;
                    pRowRange.Style.Border.OutsideBorderColor = ClosedXML.Excel.XLColor.FromArgb(226, 232, 240);
                    pRowRange.Style.Border.InsideBorderColor = ClosedXML.Excel.XLColor.FromArgb(226, 232, 240);

                    if (projNo % 2 == 0) pRowRange.Style.Fill.BackgroundColor = ClosedXML.Excel.XLColor.FromArgb(248, 250, 252);
                    wsSummary.Row(sumRow).Height = 20;

                    projNo++;
                    sumRow++;
                }

                // Summary Total Row
                int lastSumDataRow = sumRow - 1;
                wsSummary.Range(sumRow, 1, sumRow, 2).Merge();
                wsSummary.Cell(sumRow, 1).Value = "TOTAL KESELURUHAN";
                wsSummary.Cell(sumRow, 1).Style.Font.Bold = true;
                wsSummary.Cell(sumRow, 1).Style.Alignment.Horizontal = ClosedXML.Excel.XLAlignmentHorizontalValues.Right;

                var totalSumSessions = wsSummary.Cell(sumRow, 3);
                totalSumSessions.FormulaA1 = $"SUM(C5:C{lastSumDataRow})";
                totalSumSessions.Style.Font.Bold = true;
                totalSumSessions.Style.Alignment.Horizontal = ClosedXML.Excel.XLAlignmentHorizontalValues.Right;

                var totalSumHours = wsSummary.Cell(sumRow, 4);
                totalSumHours.FormulaA1 = $"SUM(D5:D{lastSumDataRow})";
                totalSumHours.Style.Font.Bold = true;
                totalSumHours.Style.NumberFormat.Format = "#,##0.00";
                totalSumHours.Style.Alignment.Horizontal = ClosedXML.Excel.XLAlignmentHorizontalValues.Right;

                var totalSumPct = wsSummary.Cell(sumRow, 5);
                totalSumPct.FormulaA1 = $"SUM(E5:E{lastSumDataRow})";
                totalSumPct.Style.Font.Bold = true;
                totalSumPct.Style.NumberFormat.Format = "0.0%";
                totalSumPct.Style.Alignment.Horizontal = ClosedXML.Excel.XLAlignmentHorizontalValues.Right;

                var sumTotalRowRange = wsSummary.Range(sumRow, 1, sumRow, sumHeaders.Length);
                sumTotalRowRange.Style.Fill.BackgroundColor = ClosedXML.Excel.XLColor.FromArgb(238, 242, 255);
                sumTotalRowRange.Style.Font.FontColor = ClosedXML.Excel.XLColor.FromArgb(49, 46, 129);
                sumTotalRowRange.Style.Border.TopBorder = ClosedXML.Excel.XLBorderStyleValues.Medium;
                sumTotalRowRange.Style.Border.TopBorderColor = ClosedXML.Excel.XLColor.FromArgb(67, 56, 202);
                sumTotalRowRange.Style.Border.BottomBorder = ClosedXML.Excel.XLBorderStyleValues.Double;
                sumTotalRowRange.Style.Border.BottomBorderColor = ClosedXML.Excel.XLColor.FromArgb(67, 56, 202);
                wsSummary.Row(sumRow).Height = 24;
            }

            wsSummary.Columns().AdjustToContents();
            wsSummary.Column(1).Width = 8;
            wsSummary.Column(2).Width = 30;
            wsSummary.Column(3).Width = 15;
            wsSummary.Column(4).Width = 20;
            wsSummary.Column(5).Width = 24;

            using var stream = new MemoryStream();
            wb.SaveAs(stream);
            var content = stream.ToArray();

            var cleanUserName = (currentUser.UserName ?? "user").Replace(" ", "_");
            var fileName = $"Timesheet_{cleanUserName}_{start:yyyyMMdd}_{end:yyyyMMdd}.xlsx";

            return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }

        // ── CLEAR / RESET ALL TIMESHEETS (ADMIN ONLY) ────────────
        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ClearAllTimesheets()
        {
            var sessions = await _db.Sessions.ToListAsync();
            _db.Sessions.RemoveRange(sessions);
            await _db.SaveChangesAsync();

            TempData["Success"] = "Semua riwayat timesheet dan pencatatan jam kerja berhasil direset oleh Administrator.";
            return RedirectToAction(nameof(Index));
        }
    }
}
