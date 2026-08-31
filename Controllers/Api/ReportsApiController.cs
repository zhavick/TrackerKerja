using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TrackerKerja.Data;
using TrackerKerja.Models;
using TrackerKerja.ViewModels;
using ModelTaskStatus = TrackerKerja.Models.TaskStatus;

namespace TrackerKerja.Controllers.Api
{
    [ApiController]
    [Route("api/reports")]
    [Produces("application/json")]
    public class ReportsApiController : ControllerBase
    {
        private readonly AppDbContext _db;

        public ReportsApiController(AppDbContext db)
        {
            _db = db;
        }

        /// <summary>
        /// Mengambil data ringkasan laporan dashboard eksekutif (GET /api/reports/dashboard)
        /// </summary>
        [HttpGet("dashboard")]
        [ProducesResponseType(typeof(ApiResponse<ReportDashboardDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetDashboardReport()
        {
            var tasks = await _db.Tasks.AsNoTracking().ToListAsync();
            var projects = await _db.Projects.Include(p => p.Tasks).ThenInclude(t => t.Sessions).AsNoTracking().ToListAsync();
            var sessions = await _db.Sessions.Where(s => s.EndTime != null).AsNoTracking().ToListAsync();

            var totalTasks = tasks.Count;
            var doneTasks = tasks.Count(t => t.Status == ModelTaskStatus.Done);
            var inProgressTasks = tasks.Count(t => t.Status == ModelTaskStatus.InProgress);
            var todoTasks = tasks.Count(t => t.Status == ModelTaskStatus.Todo);
            var overdueTasks = tasks.Count(t => t.Status == ModelTaskStatus.Overdue);

            var totalHours = Math.Round(sessions.Sum(s => s.Duration) / 3600.0, 1);
            var today = DateTime.Today;
            var todayHours = Math.Round(sessions.Where(s => s.StartTime.Date == today).Sum(s => s.Duration) / 3600.0, 1);

            // Last 7 days chart
            var chartLabels = new List<string>();
            var chartHours = new List<double>();
            for (int i = 6; i >= 0; i--)
            {
                var day = DateTime.Today.AddDays(-i);
                chartLabels.Add(day.ToString("ddd, dd MMM"));
                var h = sessions.Where(s => s.StartTime.Date == day).Sum(s => s.Duration) / 3600.0;
                chartHours.Add(Math.Round(h, 1));
            }

            var projectsSummary = projects.Select(p => new ProjectProgressReportDto
            {
                ProjectId = p.Id,
                ProjectName = p.Name,
                Color = p.Color,
                TotalTasks = p.TotalTasks,
                CompletedTasks = p.CompletedTasks,
                ProgressPercent = p.ProgressPercent,
                TotalHours = Math.Round(p.Tasks.SelectMany(t => t.Sessions).Sum(s => s.Duration) / 3600.0, 1)
            }).ToList();

            var dto = new ReportDashboardDto
            {
                TotalTasks = totalTasks,
                DoneTasks = doneTasks,
                InProgressTasks = inProgressTasks,
                TodoTasks = todoTasks,
                OverdueTasks = overdueTasks,
                CompletionRatePercent = totalTasks > 0 ? Math.Round((doneTasks / (double)totalTasks) * 100, 1) : 0,
                TotalProjects = projects.Count,
                ActiveProjects = projects.Count(p => p.Status == ProjectStatus.Active),
                TotalHoursTracked = totalHours,
                TodayHoursTracked = todayHours,
                ChartLabels = chartLabels,
                ChartHours = chartHours,
                ProjectsSummary = projectsSummary
            };

            return Ok(ApiResponse<ReportDashboardDto>.Ok(dto, "Laporan dashboard eksekutif berhasil diambil."));
        }

        /// <summary>
        /// Mengambil tren jam kerja harian untuk grafik (GET /api/reports/chart-data)
        /// </summary>
        /// <param name="period">Rentang waktu: 'week' (7 hari) atau 'month' (30 hari)</param>
        [HttpGet("chart-data")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetChartData([FromQuery] string period = "week")
        {
            var now = DateTime.Now;
            DateTime start;
            string format;
            int days;

            if (string.Equals(period, "month", StringComparison.OrdinalIgnoreCase))
            {
                start = now.AddDays(-29).Date;
                format = "dd/MM";
                days = 30;
            }
            else
            {
                start = now.AddDays(-6).Date;
                format = "ddd";
                days = 7;
            }

            var sessions = await _db.Sessions
                .Where(s => s.StartTime >= start && s.EndTime != null)
                .AsNoTracking()
                .ToListAsync();

            var labels = new List<string>();
            var data = new List<double>();

            for (int i = 0; i < days; i++)
            {
                var day = start.AddDays(i);
                labels.Add(day.ToString(format));
                var hours = sessions.Where(s => s.StartTime.Date == day.Date).Sum(s => s.Duration) / 3600.0;
                data.Add(Math.Round(hours, 1));
            }

            return Ok(ApiResponse<object>.Ok(new { labels, data, period }, "Data grafik berhasil diambil."));
        }

        /// <summary>
        /// Mengambil ringkasan beban kerja dan distribusi tugas per anggota tim (GET /api/reports/members-workload)
        /// </summary>
        [HttpGet("members-workload")]
        [ProducesResponseType(typeof(ApiResponse<List<MemberWorkloadReportDto>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetMembersWorkload()
        {
            var users = await _db.Users.OrderBy(u => u.FullName).AsNoTracking().ToListAsync();
            var allTasks = await _db.Tasks.Include(t => t.Sessions).AsNoTracking().ToListAsync();

            var list = users.Select(u =>
            {
                var uTasks = allTasks.Where(t => t.AssignedToUserId == u.Id).ToList();
                var totalSecs = uTasks.SelectMany(t => t.Sessions).Sum(s => s.DurationSeconds);
                return new MemberWorkloadReportDto
                {
                    MemberId = u.Id,
                    MemberName = u.FullName,
                    JobTitle = u.JobTitle,
                    TodoTasks = uTasks.Count(t => t.Status == ModelTaskStatus.Todo),
                    InProgressTasks = uTasks.Count(t => t.Status == ModelTaskStatus.InProgress),
                    DoneTasks = uTasks.Count(t => t.Status == ModelTaskStatus.Done),
                    TotalTasks = uTasks.Count,
                    TotalHours = Math.Round(totalSecs / 3600.0, 1)
                };
            }).ToList();

            return Ok(ApiResponse<List<MemberWorkloadReportDto>>.Ok(list, "Laporan beban kerja tim berhasil diambil."));
        }

        /// <summary>
        /// Mengambil data proyeksi timeline seluruh tugas untuk Gantt Chart (GET /api/reports/gantt)
        /// </summary>
        /// <param name="projectId">Filter ID Proyek</param>
        /// <param name="assigneeId">Filter ID Pengguna / PIC</param>
        /// <param name="status">Filter status tugas</param>
        /// <param name="startDate">Filter batas tanggal awal</param>
        /// <param name="endDate">Filter batas tanggal akhir</param>
        [HttpGet("gantt")]
        [ProducesResponseType(typeof(ApiResponse<GanttReportResponseDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetGanttData(
            [FromQuery] int? projectId,
            [FromQuery] string? assigneeId,
            [FromQuery] ModelTaskStatus? status,
            [FromQuery] DateTime? startDate,
            [FromQuery] DateTime? endDate)
        {
            var query = _db.Tasks
                .Include(t => t.Project)
                .Include(t => t.AssignedToUser)
                .Include(t => t.Sessions)
                .AsNoTracking()
                .AsQueryable();

            if (projectId.HasValue)
                query = query.Where(t => t.ProjectId == projectId.Value);

            if (!string.IsNullOrWhiteSpace(assigneeId))
                query = query.Where(t => t.AssignedToUserId == assigneeId);

            if (status.HasValue)
                query = query.Where(t => t.Status == status.Value);

            if (startDate.HasValue)
                query = query.Where(t => (t.DueDate ?? t.StartDate ?? t.CreatedAt) >= startDate.Value.Date);

            if (endDate.HasValue)
                query = query.Where(t => (t.StartDate ?? t.CreatedAt) <= endDate.Value.Date);

            var tasks = await query.OrderBy(t => t.StartDate ?? t.CreatedAt).ToListAsync();

            var ganttTasks = new List<GanttTaskDto>();
            DateTime? overallMin = null;
            DateTime? overallMax = null;

            foreach (var t in tasks)
            {
                var startDt = t.StartDate ?? t.CreatedAt.Date;
                var endDt = t.DueDate ?? (t.StartDate.HasValue ? t.StartDate.Value.AddDays(3) : t.CreatedAt.Date.AddDays(3));

                if (endDt < startDt)
                {
                    endDt = startDt.AddDays(1);
                }

                if (!overallMin.HasValue || startDt < overallMin.Value) overallMin = startDt;
                if (!overallMax.HasValue || endDt > overallMax.Value) overallMax = endDt;

                var isOverdue = t.DueDate.HasValue && t.DueDate.Value < DateTime.Now && t.Status != ModelTaskStatus.Done;
                var statusStr = isOverdue ? "Overdue" : t.Status.ToString();

                var customClass = statusStr.ToLower() switch
                {
                    "done" => "gantt-status-done",
                    "inprogress" => "gantt-status-inprogress",
                    "overdue" => "gantt-status-overdue",
                    _ => "gantt-status-todo"
                };

                ganttTasks.Add(new GanttTaskDto
                {
                    Id = t.Id,
                    Code = t.TaskCode,
                    Name = t.Title,
                    Start = startDt.ToString("yyyy-MM-dd"),
                    End = endDt.ToString("yyyy-MM-dd"),
                    Progress = t.Progress,
                    Status = statusStr,
                    Priority = t.Priority.ToString(),
                    ProjectId = t.ProjectId,
                    ProjectName = t.Project?.Name ?? "Tanpa Proyek",
                    ProjectColor = t.Project?.Color ?? "#6366F1",
                    AssigneeId = t.AssignedToUserId,
                    AssigneeName = t.AssignedToUser?.FullName ?? "Belum Ditugaskan",
                    AssigneeAvatarColor = t.AssignedToUser?.AvatarColor ?? "#6366F1",
                    Dependencies = t.ParentTaskId.HasValue ? t.ParentTaskId.Value.ToString() : "",
                    CustomClass = customClass,
                    IsParent = t.IsParent,
                    ParentTaskId = t.ParentTaskId,
                    ParentCode = t.ParentCode,
                    Obstacle = t.Obstacle,
                    Solution = t.Solution,
                    Milestone = t.Milestone ?? "Implementation",
                    DurationFormatted = t.TotalDurationFormatted
                });
            }

            var response = new GanttReportResponseDto
            {
                Tasks = ganttTasks,
                TotalTasks = ganttTasks.Count,
                CompletedTasks = ganttTasks.Count(t => t.Status == "Done"),
                InProgressTasks = ganttTasks.Count(t => t.Status == "InProgress"),
                OverdueTasks = ganttTasks.Count(t => t.Status == "Overdue"),
                MinDate = overallMin?.ToString("yyyy-MM-dd") ?? DateTime.Today.ToString("yyyy-MM-dd"),
                MaxDate = overallMax?.ToString("yyyy-MM-dd") ?? DateTime.Today.AddDays(30).ToString("yyyy-MM-dd")
            };

            return Ok(ApiResponse<GanttReportResponseDto>.Ok(response, $"Proyeksi {ganttTasks.Count} tugas untuk Gantt chart berhasil diambil."));
        }
    }
}
