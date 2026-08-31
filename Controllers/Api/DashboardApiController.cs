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
    /// Modul API Dashboard Eksekutif dan Sinkronisasi Background Worker
    /// </summary>
    [ApiController]
    [Route("api/dashboard")]
    [Produces("application/json")]
    public class DashboardApiController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly UserManager<AppUser> _userManager;

        public DashboardApiController(AppDbContext db, UserManager<AppUser> userManager)
        {
            _db = db;
            _userManager = userManager;
        }

        /// <summary>
        /// Mengambil ringkasan metrik dashboard, statistik tugas, dan distribusi proyek (GET /api/dashboard/summary)
        /// </summary>
        [HttpGet("summary")]
        [ProducesResponseType(typeof(ApiResponse<DashboardSummaryResponseDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetSummary()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var currentUserId = currentUser?.Id ?? "";
            var isAdmin = User.IsInRole("Admin");

            var allTasks = await _db.Tasks
                .Include(t => t.Project)
                .Include(t => t.Sessions)
                .AsNoTracking()
                .ToListAsync();

            var allProjects = await _db.Projects
                .Include(p => p.Tasks)
                .AsNoTracking()
                .ToListAsync();

            var totalMembers = await _db.Users.CountAsync();

            var today = DateTime.Today;
            var todaySessions = await _db.Sessions
                .Where(s => s.StartTime.Date == today)
                .ToListAsync();

            var teamTodaySec = todaySessions.Sum(s => s.Duration);
            var teamTodayHours = teamTodaySec / 3600;
            var teamTodayMins = (teamTodaySec % 3600) / 60;

            var myTasks = allTasks.Where(t => t.AssignedToUserId == currentUserId).ToList();
            var myTodaySessions = todaySessions.Where(s => s.Task != null && s.Task.AssignedToUserId == currentUserId).ToList();
            var myTodaySec = myTodaySessions.Sum(s => s.Duration);
            var myTodayHours = myTodaySec / 3600;
            var myTodayMins = (myTodaySec % 3600) / 60;

            var projectDistributions = allProjects.Select(p =>
            {
                var pTasks = p.Tasks.ToList();
                var pDone = pTasks.Count(t => t.Status == ModelTaskStatus.Done);
                var pct = pTasks.Count > 0 ? (int)Math.Round((double)pDone / pTasks.Count * 100) : 0;
                var pSec = pTasks.SelectMany(t => t.Sessions ?? Enumerable.Empty<WorkSession>()).Sum(s => s.Duration);

                return new ProjectProgressReportDto
                {
                    ProjectId = p.Id,
                    ProjectName = p.Name,
                    Color = p.Color,
                    TotalTasks = pTasks.Count,
                    CompletedTasks = pDone,
                    ProgressPercent = pct,
                    TotalHours = Math.Round(pSec / 3600.0, 1)
                };
            }).ToList();

            var summary = new DashboardSummaryResponseDto
            {
                CurrentUserName = currentUser?.FullName ?? "Pengguna",
                IsAdmin = isAdmin,
                TotalTasks = allTasks.Count,
                DoneTasks = allTasks.Count(t => t.Status == ModelTaskStatus.Done),
                InProgressTasks = allTasks.Count(t => t.Status == ModelTaskStatus.InProgress),
                TodoTasks = allTasks.Count(t => t.Status == ModelTaskStatus.Todo),
                OverdueTasks = allTasks.Count(t => t.DueDate.HasValue && t.DueDate.Value < DateTime.Now && t.Status != ModelTaskStatus.Done),
                TotalProjects = allProjects.Count,
                TotalMembers = totalMembers,
                TodayTeamWorkSeconds = teamTodaySec,
                TodayTeamWorkFormatted = $"{teamTodayHours}j {teamTodayMins}m",
                MyTotalTasks = myTasks.Count,
                MyInProgressTasks = myTasks.Count(t => t.Status == ModelTaskStatus.InProgress),
                MyDoneTasks = myTasks.Count(t => t.Status == ModelTaskStatus.Done),
                MyTodayWorkFormatted = $"{myTodayHours}j {myTodayMins}m",
                ProjectsDistribution = projectDistributions,
                LastSyncTimestamp = DateTime.Now
            };

            return Ok(ApiResponse<DashboardSummaryResponseDto>.Ok(summary, "Ringkasan metrik dashboard berhasil diambil."));
        }

        /// <summary>
        /// Memicu sinkronisasi evaluasi status tugas dan background worker secara instan (POST /api/dashboard/sync)
        /// </summary>
        [HttpPost("sync")]
        [ProducesResponseType(typeof(ApiResponse<TriggerSyncResponseDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> RunSync()
        {
            var tasks = await _db.Tasks.ToListAsync();
            int syncedCount = 0;

            foreach (var task in tasks)
            {
                bool modified = false;
                if (task.Status == ModelTaskStatus.Done && task.Progress != 100)
                {
                    task.Progress = 100;
                    modified = true;
                }
                else if (task.Progress >= 100 && task.Status != ModelTaskStatus.Done)
                {
                    task.Status = ModelTaskStatus.Done;
                    task.Progress = 100;
                    modified = true;
                }

                if (modified)
                {
                    task.UpdatedAt = DateTime.Now;
                    syncedCount++;
                }
            }

            if (syncedCount > 0)
            {
                await _db.SaveChangesAsync();
            }

            var result = new TriggerSyncResponseDto
            {
                IsSuccess = true,
                SyncTimestamp = DateTime.Now,
                TasksEvaluatedCount = tasks.Count,
                StatusAutoUpdatedCount = syncedCount,
                Message = $"Sinkronisasi selesai. {syncedCount} tugas disinkronkan."
            };

            return Ok(ApiResponse<TriggerSyncResponseDto>.Ok(result, result.Message));
        }
    }
}
