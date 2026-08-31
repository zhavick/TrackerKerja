using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TrackerKerja.Data;
using TrackerKerja.Models;
using TrackerKerja.ViewModels;

namespace TrackerKerja.Controllers.Api
{
    [ApiController]
    [Route("api/timesheets")]
    [Produces("application/json")]
    public class TimesheetsApiController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly UserManager<AppUser> _userManager;

        public TimesheetsApiController(AppDbContext db, UserManager<AppUser> userManager)
        {
            _db = db;
            _userManager = userManager;
        }

        /// <summary>
        /// Mengambil daftar sesi kerja / timesheet dengan filter dan pagination (GET /api/timesheets)
        /// </summary>
        /// <param name="taskId">Filter ID Tugas</param>
        /// <param name="projectId">Filter ID Proyek</param>
        /// <param name="userId">Filter ID Pengguna / PIC</param>
        /// <param name="startDate">Filter tanggal mulai (format: yyyy-MM-dd)</param>
        /// <param name="endDate">Filter tanggal selesai (format: yyyy-MM-dd)</param>
        /// <param name="page">Nomor halaman (default: 1)</param>
        /// <param name="pageSize">Ukuran halaman (default: 20)</param>
        [HttpGet]
        [ProducesResponseType(typeof(ApiResponse<PagedResult<TimesheetResponseDto>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAll(
            [FromQuery] int? taskId,
            [FromQuery] int? projectId,
            [FromQuery] string? userId,
            [FromQuery] DateTime? startDate,
            [FromQuery] DateTime? endDate,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 20;

            var query = _db.Sessions
                .Include(s => s.Task)
                    .ThenInclude(t => t!.Project)
                .Include(s => s.Task)
                    .ThenInclude(t => t!.AssignedToUser)
                .AsNoTracking()
                .AsQueryable();

            if (taskId.HasValue)
                query = query.Where(s => s.TaskId == taskId.Value);

            if (projectId.HasValue)
                query = query.Where(s => s.Task != null && s.Task.ProjectId == projectId.Value);

            if (!string.IsNullOrWhiteSpace(userId))
                query = query.Where(s => s.Task != null && s.Task.AssignedToUserId == userId);

            if (startDate.HasValue)
                query = query.Where(s => s.StartTime >= startDate.Value.Date);

            if (endDate.HasValue)
            {
                var endOfDay = endDate.Value.Date.AddDays(1).AddTicks(-1);
                query = query.Where(s => s.StartTime <= endOfDay);
            }

            var totalItems = await query.CountAsync();
            var sessions = await query
                .OrderByDescending(s => s.StartTime)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var dtos = sessions.Select(MapToResponseDto).ToList();

            var result = new PagedResult<TimesheetResponseDto>
            {
                Items = dtos,
                TotalItems = totalItems,
                Page = page,
                PageSize = pageSize
            };

            return Ok(ApiResponse<PagedResult<TimesheetResponseDto>>.Ok(result, $"Berhasil mengambil {dtos.Count} sesi timesheet."));
        }

        /// <summary>
        /// Mengambil ringkasan statistik waktu kerja hari ini, minggu ini, dan bulan ini (GET /api/timesheets/summary)
        /// </summary>
        [HttpGet("summary")]
        [ProducesResponseType(typeof(ApiResponse<TimesheetSummaryDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetSummary()
        {
            var now = DateTime.Now;
            var todayStart = DateTime.Today;
            var todayEnd = todayStart.AddDays(1).AddTicks(-1);

            int diff = (7 + (now.DayOfWeek - DayOfWeek.Monday)) % 7;
            var weekStart = now.AddDays(-1 * diff).Date;

            var monthStart = new DateTime(now.Year, now.Month, 1);

            var todaySecs = await _db.Sessions
                .Where(s => s.StartTime >= todayStart && s.StartTime <= todayEnd)
                .SumAsync(s => s.Duration);

            var weekSecs = await _db.Sessions
                .Where(s => s.StartTime >= weekStart)
                .SumAsync(s => s.Duration);

            var monthSecs = await _db.Sessions
                .Where(s => s.StartTime >= monthStart)
                .SumAsync(s => s.Duration);

            var runningCount = await _db.Sessions.CountAsync(s => s.EndTime == null);

            var format = (long secs) =>
            {
                var h = secs / 3600;
                var m = (secs % 3600) / 60;
                return $"{h}j {m}m";
            };

            var summary = new TimesheetSummaryDto
            {
                TodaySeconds = todaySecs,
                TodayFormatted = format(todaySecs),
                WeekSeconds = weekSecs,
                WeekFormatted = format(weekSecs),
                MonthSeconds = monthSecs,
                MonthFormatted = format(monthSecs),
                ActiveRunningTimers = runningCount
            };

            return Ok(ApiResponse<TimesheetSummaryDto>.Ok(summary, "Ringkasan waktu kerja berhasil diambil."));
        }

        /// <summary>
        /// Mengambil detail satu sesi kerja (GET /api/timesheets/{id})
        /// </summary>
        /// <param name="id">ID Sesi Timesheet</param>
        [HttpGet("{id:int}")]
        [ProducesResponseType(typeof(ApiResponse<TimesheetResponseDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetById(int id)
        {
            var session = await _db.Sessions
                .Include(s => s.Task)
                    .ThenInclude(t => t!.Project)
                .Include(s => s.Task)
                    .ThenInclude(t => t!.AssignedToUser)
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.Id == id);

            if (session == null)
            {
                return NotFound(ApiResponse<TimesheetResponseDto>.Fail($"Sesi kerja dengan ID {id} tidak ditemukan."));
            }

            return Ok(ApiResponse<TimesheetResponseDto>.Ok(MapToResponseDto(session), "Detail sesi kerja berhasil diambil."));
        }

        /// <summary>
        /// Mencatat sesi kerja manual (POST /api/timesheets)
        /// </summary>
        /// <param name="dto">Payload sesi timesheet</param>
        [HttpPost]
        [ProducesResponseType(typeof(ApiResponse<TimesheetResponseDto>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Create([FromBody] CreateTimesheetRequestDto dto)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                return BadRequest(ApiResponse<TimesheetResponseDto>.Fail("Validasi gagal.", errors));
            }

            if (!await _db.Tasks.AnyAsync(t => t.Id == dto.TaskId))
            {
                return BadRequest(ApiResponse<TimesheetResponseDto>.Fail($"Tugas dengan ID {dto.TaskId} tidak ditemukan."));
            }

            var duration = dto.Duration;
            if (duration == 0 && dto.EndTime.HasValue && dto.EndTime.Value > dto.StartTime)
            {
                duration = (long)(dto.EndTime.Value - dto.StartTime).TotalSeconds;
            }

            var session = new WorkSession
            {
                TaskId = dto.TaskId,
                StartTime = dto.StartTime,
                EndTime = dto.EndTime,
                Duration = duration,
                Notes = dto.Notes?.Trim()
            };

            _db.Sessions.Add(session);
            await _db.SaveChangesAsync();

            var created = await _db.Sessions
                .Include(s => s.Task)
                    .ThenInclude(t => t!.Project)
                .Include(s => s.Task)
                    .ThenInclude(t => t!.AssignedToUser)
                .AsNoTracking()
                .FirstAsync(s => s.Id == session.Id);

            return CreatedAtAction(nameof(GetById), new { id = session.Id }, ApiResponse<TimesheetResponseDto>.Ok(MapToResponseDto(created), "Sesi kerja berhasil dicatat."));
        }

        /// <summary>
        /// Mengambil seluruh timer yang sedang aktif berjalan untuk pengguna saat ini (GET /api/timesheets/active-timers)
        /// </summary>
        [HttpGet("active-timers")]
        [ProducesResponseType(typeof(ApiResponse<ActiveTimersResponseDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetActiveTimers()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var currentUserId = currentUser?.Id;

            var runningSessions = await _db.Sessions
                .Include(s => s.Task)
                    .ThenInclude(t => t!.Project)
                .Include(s => s.Task)
                    .ThenInclude(t => t!.Category)
                .Where(s => s.EndTime == null && (s.UserId == currentUserId || (s.UserId == null && s.Task != null && s.Task.AssignedToUserId == currentUserId)))
                .OrderByDescending(s => s.StartTime)
                .ToListAsync();

            var timers = runningSessions.Select(s => new ActiveTimerItemDto
            {
                SessionId = s.Id,
                TaskId = s.TaskId,
                TaskCode = s.Task?.TaskCode ?? $"TSK-{s.TaskId:D4}",
                TaskTitle = s.Task?.Title ?? "Tugas",
                ProjectName = s.Task?.Project?.Name,
                CategoryName = s.Task?.Category?.Name,
                Priority = s.Task?.Priority.ToString(),
                StartTime = s.StartTime,
                ElapsedSeconds = Math.Max(0, (long)(DateTime.Now - s.StartTime).TotalSeconds),
                ElapsedFormatted = FormatSeconds(Math.Max(0, (long)(DateTime.Now - s.StartTime).TotalSeconds))
            }).ToList();

            var response = new ActiveTimersResponseDto
            {
                TotalActiveTimers = timers.Count,
                ActiveTimers = timers
            };

            return Ok(ApiResponse<ActiveTimersResponseDto>.Ok(response, $"Ditemukan {timers.Count} timer aktif."));
        }

        /// <summary>
        /// Memulai live timer untuk suatu tugas secara bersamaan (POST /api/timesheets/start)
        /// </summary>
        /// <param name="dto">Task ID yang akan dimulai timernya</param>
        [HttpPost("start")]
        [ProducesResponseType(typeof(ApiResponse<TimesheetResponseDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> StartTimer([FromBody] StartTimerRequestDto dto)
        {
            var task = await _db.Tasks.FindAsync(dto.TaskId);
            if (task == null)
            {
                return BadRequest(ApiResponse<TimesheetResponseDto>.Fail($"Tugas dengan ID {dto.TaskId} tidak ditemukan."));
            }

            var currentUser = await _userManager.GetUserAsync(User);
            var currentUserId = currentUser?.Id;

            // Cek apakah timer sudah berjalan untuk task ini pada user yang sama
            var existingSession = await _db.Sessions.FirstOrDefaultAsync(s => 
                s.TaskId == dto.TaskId && 
                s.EndTime == null && 
                (s.UserId == currentUserId || (s.UserId == null && task.AssignedToUserId == currentUserId)));

            if (existingSession != null)
            {
                var existing = await _db.Sessions
                    .Include(s => s.Task).ThenInclude(t => t!.Project)
                    .Include(s => s.Task).ThenInclude(t => t!.AssignedToUser)
                    .AsNoTracking()
                    .FirstAsync(s => s.Id == existingSession.Id);
                return Ok(ApiResponse<TimesheetResponseDto>.Ok(MapToResponseDto(existing), $"Timer sudah aktif berjalan untuk tugas '{task.Title}'."));
            }

            var session = new WorkSession
            {
                TaskId = dto.TaskId,
                UserId = currentUserId,
                StartTime = DateTime.Now,
                EndTime = null,
                Duration = 0
            };

            // Jika status tugas masih Todo, otomatis ubah jadi InProgress
            if (task.Status == Models.TaskStatus.Todo)
            {
                task.Status = Models.TaskStatus.InProgress;
                task.UpdatedAt = DateTime.Now;
            }

            _db.Sessions.Add(session);
            await _db.SaveChangesAsync();

            var created = await _db.Sessions
                .Include(s => s.Task)
                    .ThenInclude(t => t!.Project)
                .Include(s => s.Task)
                    .ThenInclude(t => t!.AssignedToUser)
                .AsNoTracking()
                .FirstAsync(s => s.Id == session.Id);

            return Ok(ApiResponse<TimesheetResponseDto>.Ok(MapToResponseDto(created), $"Live timer untuk tugas '{task.Title}' berhasil dimulai."));
        }

        /// <summary>
        /// Menghentikan live timer yang sedang berjalan (POST /api/timesheets/stop)
        /// </summary>
        /// <param name="dto">Sesi ID atau Task ID</param>
        [HttpPost("stop")]
        [ProducesResponseType(typeof(ApiResponse<TimesheetResponseDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> StopTimer([FromBody] StopTimerRequestDto dto)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var currentUserId = currentUser?.Id;

            WorkSession? session = null;
            if (dto.SessionId.HasValue)
            {
                session = await _db.Sessions.FirstOrDefaultAsync(s => s.Id == dto.SessionId.Value && s.EndTime == null);
            }
            else if (dto.TaskId.HasValue)
            {
                session = await _db.Sessions.FirstOrDefaultAsync(s => 
                    s.TaskId == dto.TaskId.Value && 
                    s.EndTime == null && 
                    (s.UserId == currentUserId || (s.UserId == null && s.Task != null && s.Task.AssignedToUserId == currentUserId)));
            }
            else
            {
                session = await _db.Sessions.OrderByDescending(s => s.StartTime).FirstOrDefaultAsync(s => 
                    s.EndTime == null && 
                    (s.UserId == currentUserId || (s.UserId == null && s.Task != null && s.Task.AssignedToUserId == currentUserId)));
            }

            if (session == null)
            {
                return NotFound(ApiResponse<TimesheetResponseDto>.Fail("Tidak ditemukan sesi timer yang sedang berjalan aktif."));
            }

            session.EndTime = DateTime.Now;
            session.Duration = Math.Max(1, (long)(session.EndTime.Value - session.StartTime).TotalSeconds);
            if (!string.IsNullOrWhiteSpace(dto.Notes))
            {
                session.Notes = dto.Notes.Trim();
            }

            await _db.SaveChangesAsync();

            var stopped = await _db.Sessions
                .Include(s => s.Task)
                    .ThenInclude(t => t!.Project)
                .Include(s => s.Task)
                    .ThenInclude(t => t!.AssignedToUser)
                .AsNoTracking()
                .FirstAsync(s => s.Id == session.Id);

            return Ok(ApiResponse<TimesheetResponseDto>.Ok(MapToResponseDto(stopped), $"Timer berhasil dihentikan. Durasi: {stopped.DurationFormatted}"));
        }

        /// <summary>
        /// Memperbarui catatan atau durasi sesi kerja (PUT /api/timesheets/{id})
        /// </summary>
        /// <param name="id">ID Sesi Timesheet</param>
        /// <param name="dto">Payload pembaruan sesi</param>
        [HttpPut("{id:int}")]
        [ProducesResponseType(typeof(ApiResponse<TimesheetResponseDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateTimesheetRequestDto dto)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                return BadRequest(ApiResponse<TimesheetResponseDto>.Fail("Validasi gagal.", errors));
            }

            var session = await _db.Sessions.FirstOrDefaultAsync(s => s.Id == id);
            if (session == null)
            {
                return NotFound(ApiResponse<TimesheetResponseDto>.Fail($"Sesi kerja dengan ID {id} tidak ditemukan."));
            }

            session.StartTime = dto.StartTime;
            session.EndTime = dto.EndTime;
            session.Duration = dto.Duration;
            session.Notes = dto.Notes?.Trim();

            await _db.SaveChangesAsync();

            var updated = await _db.Sessions
                .Include(s => s.Task)
                    .ThenInclude(t => t!.Project)
                .Include(s => s.Task)
                    .ThenInclude(t => t!.AssignedToUser)
                .AsNoTracking()
                .FirstAsync(s => s.Id == id);

            return Ok(ApiResponse<TimesheetResponseDto>.Ok(MapToResponseDto(updated), "Sesi kerja berhasil diperbarui."));
        }

        /// <summary>
        /// Menghapus sesi kerja (DELETE /api/timesheets/{id})
        /// </summary>
        /// <param name="id">ID Sesi Timesheet</param>
        [HttpDelete("{id:int}")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Delete(int id)
        {
            var session = await _db.Sessions.FirstOrDefaultAsync(s => s.Id == id);
            if (session == null)
            {
                return NotFound(ApiResponse<object>.Fail($"Sesi kerja dengan ID {id} tidak ditemukan."));
            }

            _db.Sessions.Remove(session);
            await _db.SaveChangesAsync();

            return Ok(ApiResponse<object>.Ok(new { id = id }, $"Sesi kerja ID {id} berhasil dihapus."));
        }

        /// <summary>
        /// Menghapus seluruh data sesi timesheet (DELETE /api/timesheets/all)
        /// </summary>
        [HttpDelete("all")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        public async Task<IActionResult> ClearAll()
        {
            var sessions = await _db.Sessions.ToListAsync();
            var count = sessions.Count;

            _db.Sessions.RemoveRange(sessions);
            await _db.SaveChangesAsync();

            return Ok(ApiResponse<object>.Ok(new { deletedCount = count }, $"Seluruh {count} sesi timesheet berhasil dibersihkan."));
        }

        /// <summary>
        /// Mengunduh laporan rekapitulasi timesheet dalam format Excel (.xlsx) (GET /api/timesheets/export-excel)
        /// </summary>
        /// <param name="projectId">Filter ID Proyek</param>
        /// <param name="userId">Filter ID Pengguna</param>
        /// <param name="startDate">Filter Tanggal Mulai</param>
        /// <param name="endDate">Filter Tanggal Selesai</param>
        [HttpGet("export-excel")]
        [ProducesResponseType(typeof(FileResult), StatusCodes.Status200OK)]
        public async Task<IActionResult> ExportExcel(
            [FromQuery] int? projectId,
            [FromQuery] string? userId,
            [FromQuery] DateTime? startDate,
            [FromQuery] DateTime? endDate)
        {
            var query = _db.Sessions
                .Include(s => s.Task)
                    .ThenInclude(t => t!.Project)
                .Include(s => s.Task)
                    .ThenInclude(t => t!.AssignedToUser)
                .AsNoTracking()
                .AsQueryable();

            if (projectId.HasValue) query = query.Where(s => s.Task != null && s.Task.ProjectId == projectId.Value);
            if (!string.IsNullOrWhiteSpace(userId)) query = query.Where(s => s.Task != null && s.Task.AssignedToUserId == userId);
            if (startDate.HasValue) query = query.Where(s => s.StartTime >= startDate.Value.Date);
            if (endDate.HasValue) query = query.Where(s => s.StartTime <= endDate.Value.Date.AddDays(1).AddTicks(-1));

            var sessions = await query.OrderByDescending(s => s.StartTime).ToListAsync();

            using var wb = new ClosedXML.Excel.XLWorkbook();
            var ws = wb.Worksheets.Add("Laporan Timesheet");

            var headers = new[] { "No", "Kode Tugas", "Judul Tugas", "Proyek", "PIC / Anggota Tim", "Waktu Mulai", "Waktu Selesai", "Durasi (Detik)", "Format Durasi", "Catatan Aktivitas" };
            for (int i = 0; i < headers.Length; i++)
            {
                var cell = ws.Cell(1, i + 1);
                cell.Value = headers[i];
                cell.Style.Font.Bold = true;
                cell.Style.Font.FontColor = ClosedXML.Excel.XLColor.White;
                cell.Style.Fill.BackgroundColor = ClosedXML.Excel.XLColor.FromHtml("#6366F1");
                cell.Style.Alignment.Horizontal = ClosedXML.Excel.XLAlignmentHorizontalValues.Center;
            }

            for (int i = 0; i < sessions.Count; i++)
            {
                var s = sessions[i];
                var r = i + 2;
                ws.Cell(r, 1).Value = i + 1;
                ws.Cell(r, 2).Value = s.Task?.TaskCode ?? "-";
                ws.Cell(r, 3).Value = s.Task?.Title ?? "-";
                ws.Cell(r, 4).Value = s.Task?.Project?.Name ?? "Tanpa Proyek";
                ws.Cell(r, 5).Value = s.Task?.AssignedToUser?.FullName ?? "Belum Ditugaskan";
                ws.Cell(r, 6).Value = s.StartTime.ToString("yyyy-MM-dd HH:mm:ss");
                ws.Cell(r, 7).Value = s.EndTime?.ToString("yyyy-MM-dd HH:mm:ss") ?? "Berjalan (Active)";
                ws.Cell(r, 8).Value = s.Duration;
                ws.Cell(r, 9).Value = s.DurationFormatted;
                ws.Cell(r, 10).Value = s.Notes ?? "-";
            }

            ws.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            wb.SaveAs(stream);
            var content = stream.ToArray();

            return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"Timesheet_Report_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx");
        }

        /// <summary>
        /// Mengunduh laporan rekapitulasi timesheet dalam format CSV (GET /api/timesheets/export-csv)
        /// </summary>
        /// <param name="projectId">Filter ID Proyek</param>
        /// <param name="userId">Filter ID Pengguna</param>
        /// <param name="startDate">Filter Tanggal Mulai</param>
        /// <param name="endDate">Filter Tanggal Selesai</param>
        [HttpGet("export-csv")]
        [ProducesResponseType(typeof(FileResult), StatusCodes.Status200OK)]
        public async Task<IActionResult> ExportCsv(
            [FromQuery] int? projectId,
            [FromQuery] string? userId,
            [FromQuery] DateTime? startDate,
            [FromQuery] DateTime? endDate)
        {
            var query = _db.Sessions
                .Include(s => s.Task)
                    .ThenInclude(t => t!.Project)
                .Include(s => s.Task)
                    .ThenInclude(t => t!.AssignedToUser)
                .AsNoTracking()
                .AsQueryable();

            if (projectId.HasValue) query = query.Where(s => s.Task != null && s.Task.ProjectId == projectId.Value);
            if (!string.IsNullOrWhiteSpace(userId)) query = query.Where(s => s.Task != null && s.Task.AssignedToUserId == userId);
            if (startDate.HasValue) query = query.Where(s => s.StartTime >= startDate.Value.Date);
            if (endDate.HasValue) query = query.Where(s => s.StartTime <= endDate.Value.Date.AddDays(1).AddTicks(-1));

            var sessions = await query.OrderByDescending(s => s.StartTime).ToListAsync();

            var sb = new System.Text.StringBuilder();
            sb.AppendLine("No,Kode Tugas,Judul Tugas,Proyek,PIC,Waktu Mulai,Waktu Selesai,Durasi Detik,Format Durasi,Catatan");

            for (int i = 0; i < sessions.Count; i++)
            {
                var s = sessions[i];
                var taskTitle = (s.Task?.Title ?? "").Replace("\"", "\"\"");
                var proj = (s.Task?.Project?.Name ?? "").Replace("\"", "\"\"");
                var pic = (s.Task?.AssignedToUser?.FullName ?? "").Replace("\"", "\"\"");
                var notes = (s.Notes ?? "").Replace("\"", "\"\"");

                sb.AppendLine($"{i + 1},\"{s.Task?.TaskCode}\",\"{taskTitle}\",\"{proj}\",\"{pic}\",\"{s.StartTime:yyyy-MM-dd HH:mm:ss}\",\"{s.EndTime:yyyy-MM-dd HH:mm:ss}\",{s.Duration},\"{s.DurationFormatted}\",\"{notes}\"");
            }

            var bytes = System.Text.Encoding.UTF8.GetBytes(sb.ToString());
            return File(bytes, "text/csv", $"Timesheet_Report_{DateTime.Now:yyyyMMdd_HHmmss}.csv");
        }

        private static TimesheetResponseDto MapToResponseDto(WorkSession s)
        {
            return new TimesheetResponseDto
            {
                Id = s.Id,
                TaskId = s.TaskId,
                TaskTitle = s.Task?.Title ?? "-",
                TaskCode = s.Task?.TaskCode ?? "-",
                ProjectId = s.Task?.ProjectId,
                ProjectName = s.Task?.Project?.Name,
                ProjectColor = s.Task?.Project?.Color,
                AssigneeName = s.Task?.AssignedToUser?.FullName,
                StartTime = s.StartTime,
                EndTime = s.EndTime,
                DurationSeconds = s.Duration,
                DurationFormatted = s.DurationFormatted,
                Notes = s.Notes,
                IsRunning = s.IsRunning
            };
        }

        private static string FormatSeconds(long seconds)
        {
            var h = seconds / 3600;
            var m = (seconds % 3600) / 60;
            var s = seconds % 60;
            return $"{h:D2}:{m:D2}:{s:D2}";
        }
    }
}
