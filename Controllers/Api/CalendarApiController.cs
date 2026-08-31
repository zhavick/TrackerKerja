using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TrackerKerja.Data;
using TrackerKerja.Models;
using TrackerKerja.ViewModels;
using ModelTaskStatus = TrackerKerja.Models.TaskStatus;

namespace TrackerKerja.Controllers.Api
{
    /// <summary>
    /// Modul API Kalender dan Timeline Pengerjaan Tugas
    /// </summary>
    [ApiController]
    [Route("api/calendar")]
    [Produces("application/json")]
    public class CalendarApiController : ControllerBase
    {
        private readonly AppDbContext _db;

        public CalendarApiController(AppDbContext db)
        {
            _db = db;
        }

        /// <summary>
        /// Mengambil feed event tugas untuk kalender FullCalendar / Timeline (GET /api/calendar/events)
        /// </summary>
        /// <param name="start">Tanggal awal rentang kalender (format ISO: yyyy-MM-dd)</param>
        /// <param name="end">Tanggal akhir rentang kalender (format ISO: yyyy-MM-dd)</param>
        /// <param name="projectId">Filter ID Proyek</param>
        /// <param name="assigneeId">Filter ID Pengguna PIC</param>
        [HttpGet("events")]
        [ProducesResponseType(typeof(ApiResponse<List<CalendarEventDto>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetEvents(
            [FromQuery] DateTime? start,
            [FromQuery] DateTime? end,
            [FromQuery] int? projectId,
            [FromQuery] string? assigneeId)
        {
            var query = _db.Tasks
                .Include(t => t.Project)
                .Include(t => t.AssignedToUser)
                .AsNoTracking()
                .AsQueryable();

            if (projectId.HasValue)
                query = query.Where(t => t.ProjectId == projectId.Value);

            if (!string.IsNullOrWhiteSpace(assigneeId))
                query = query.Where(t => t.AssignedToUserId == assigneeId);

            if (start.HasValue)
                query = query.Where(t => (t.DueDate ?? t.StartDate ?? t.CreatedAt) >= start.Value);

            if (end.HasValue)
                query = query.Where(t => (t.StartDate ?? t.CreatedAt) <= end.Value);

            var tasks = await query.ToListAsync();

            var events = tasks.Select(t =>
            {
                var startDate = t.StartDate ?? t.CreatedAt;
                var endDate = t.DueDate ?? startDate.AddDays(1);

                var color = t.Priority switch
                {
                    TaskPriority.Critical => "#EF4444",
                    TaskPriority.High => "#F59E0B",
                    TaskPriority.Low => "#10B981",
                    _ => t.Project?.Color ?? "#6366F1"
                };

                return new CalendarEventDto
                {
                    Id = t.Id,
                    Title = $"[{t.TaskCode}] {t.Title}",
                    TaskCode = t.TaskCode,
                    Start = startDate.ToString("yyyy-MM-ddTHH:mm:ss"),
                    End = endDate.ToString("yyyy-MM-ddTHH:mm:ss"),
                    AllDay = true,
                    BackgroundColor = color,
                    BorderColor = color,
                    TextColor = "#FFFFFF",
                    Status = t.Status.ToString(),
                    Priority = t.Priority.ToString(),
                    ProjectName = t.Project?.Name ?? "Tanpa Proyek",
                    AssigneeName = t.AssignedToUser?.FullName ?? "Belum Ditugaskan",
                    Milestone = t.Milestone ?? "Implementation",
                    Progress = t.Progress,
                    Url = $"/Task/Edit/{t.Id}"
                };
            }).ToList();

            return Ok(ApiResponse<List<CalendarEventDto>>.Ok(events, $"Berhasil mengambil {events.Count} event kalender."));
        }
    }
}
