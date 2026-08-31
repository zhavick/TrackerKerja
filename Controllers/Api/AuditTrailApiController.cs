using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TrackerKerja.Data;
using TrackerKerja.Models;
using TrackerKerja.ViewModels;

namespace TrackerKerja.Controllers.Api
{
    [ApiController]
    [Route("api/audit-trail")]
    [Produces("application/json")]
    public class AuditTrailApiController : ControllerBase
    {
        private readonly AppDbContext _db;

        public AuditTrailApiController(AppDbContext db)
        {
            _db = db;
        }

        /// <summary>
        /// Mengambil daftar riwayat audit trail dengan filter dan pagination (GET /api/audit-trail)
        /// </summary>
        /// <param name="search">Pencarian path, user email, controller, atau IP address</param>
        /// <param name="controllerName">Filter nama Controller</param>
        /// <param name="httpMethod">Filter HTTP Method (GET, POST, PUT, DELETE)</param>
        /// <param name="userEmail">Filter email pengguna</param>
        /// <param name="date">Filter tanggal (format: yyyy-MM-dd)</param>
        /// <param name="page">Nomor halaman (default: 1)</param>
        /// <param name="pageSize">Jumlah per halaman (default: 30, max: 100)</param>
        [HttpGet]
        [ProducesResponseType(typeof(ApiResponse<PagedResult<AuditLogResponseDto>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAll(
            [FromQuery] string? search,
            [FromQuery] string? controllerName,
            [FromQuery] string? httpMethod,
            [FromQuery] string? userEmail,
            [FromQuery] string? date,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 30)
        {
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 30;
            if (pageSize > 100) pageSize = 100;

            var query = _db.AuditLogs.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim().ToLower();
                query = query.Where(a =>
                    a.Path.ToLower().Contains(s) ||
                    (a.UserEmail != null && a.UserEmail.ToLower().Contains(s)) ||
                    a.ControllerName.ToLower().Contains(s) ||
                    a.ActionName.ToLower().Contains(s) ||
                    (a.IpAddress != null && a.IpAddress.Contains(s)));
            }

            if (!string.IsNullOrWhiteSpace(controllerName))
                query = query.Where(a => a.ControllerName == controllerName);

            if (!string.IsNullOrWhiteSpace(httpMethod))
                query = query.Where(a => a.HttpMethod == httpMethod);

            if (!string.IsNullOrWhiteSpace(userEmail))
                query = query.Where(a => a.UserEmail == userEmail);

            if (!string.IsNullOrWhiteSpace(date) && DateTime.TryParse(date, out var filterDate))
            {
                var nextDay = filterDate.Date.AddDays(1);
                query = query.Where(a => a.Timestamp >= filterDate.Date && a.Timestamp < nextDay);
            }

            var totalItems = await query.CountAsync();
            var logs = await query
                .OrderByDescending(a => a.Timestamp)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var dtos = logs.Select(MapToResponseDto).ToList();

            var result = new PagedResult<AuditLogResponseDto>
            {
                Items = dtos,
                TotalItems = totalItems,
                Page = page,
                PageSize = pageSize
            };

            return Ok(ApiResponse<PagedResult<AuditLogResponseDto>>.Ok(result, $"Berhasil mengambil {dtos.Count} log audit trail."));
        }

        /// <summary>
        /// Mengambil detail satu entri audit log (GET /api/audit-trail/{id})
        /// </summary>
        /// <param name="id">ID Audit Log</param>
        [HttpGet("{id:int}")]
        [ProducesResponseType(typeof(ApiResponse<AuditLogResponseDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetById(int id)
        {
            var log = await _db.AuditLogs.AsNoTracking().FirstOrDefaultAsync(a => a.Id == id);
            if (log == null)
            {
                return NotFound(ApiResponse<AuditLogResponseDto>.Fail($"Audit log dengan ID {id} tidak ditemukan."));
            }

            return Ok(ApiResponse<AuditLogResponseDto>.Ok(MapToResponseDto(log), "Detail audit log berhasil diambil."));
        }

        /// <summary>
        /// Mengambil ringkasan statistik aktivitas sistem audit (GET /api/audit-trail/stats)
        /// </summary>
        [HttpGet("stats")]
        [ProducesResponseType(typeof(ApiResponse<AuditStatsDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetStats()
        {
            var totalLogs = await _db.AuditLogs.CountAsync();
            var today = DateTime.Today;
            var todayCount = await _db.AuditLogs.CountAsync(a => a.Timestamp >= today);

            var avgDuration = totalLogs > 0 ? await _db.AuditLogs.AverageAsync(a => (double)a.DurationMs) : 0;

            var topControllers = await _db.AuditLogs
                .GroupBy(a => a.ControllerName)
                .Select(g => new { Controller = g.Key, Count = g.Count() })
                .OrderByDescending(g => g.Count)
                .Take(5)
                .ToDictionaryAsync(g => g.Controller, g => g.Count);

            var httpMethods = await _db.AuditLogs
                .GroupBy(a => a.HttpMethod)
                .Select(g => new { Method = g.Key, Count = g.Count() })
                .OrderByDescending(g => g.Count)
                .ToDictionaryAsync(g => g.Method, g => g.Count);

            var stats = new AuditStatsDto
            {
                TotalLogs = totalLogs,
                TotalToday = todayCount,
                AverageDurationMs = Math.Round(avgDuration, 2),
                TopControllers = topControllers,
                HttpMethodsCount = httpMethods
            };

            return Ok(ApiResponse<AuditStatsDto>.Ok(stats, "Statistik audit trail berhasil diambil."));
        }

        /// <summary>
        /// Mengunduh seluruh log audit trail dalam format CSV (GET /api/audit-trail/export-csv)
        /// </summary>
        /// <param name="controllerName">Filter nama Controller</param>
        /// <param name="httpMethod">Filter HTTP Method</param>
        [HttpGet("export-csv")]
        [ProducesResponseType(typeof(FileResult), StatusCodes.Status200OK)]
        public async Task<IActionResult> ExportCsv([FromQuery] string? controllerName, [FromQuery] string? httpMethod)
        {
            var query = _db.AuditLogs.AsNoTracking().AsQueryable();
            if (!string.IsNullOrWhiteSpace(controllerName)) query = query.Where(a => a.ControllerName == controllerName);
            if (!string.IsNullOrWhiteSpace(httpMethod)) query = query.Where(a => a.HttpMethod == httpMethod);

            var logs = await query.OrderByDescending(a => a.Timestamp).ToListAsync();

            var sb = new System.Text.StringBuilder();
            sb.AppendLine("ID,Waktu,Email Pengguna,Controller,Action,HTTP Method,Path,Status Code,Durasi (ms),IP Address");

            foreach (var a in logs)
            {
                var email = (a.UserEmail ?? "").Replace("\"", "\"\"");
                var path = (a.Path ?? "").Replace("\"", "\"\"");
                sb.AppendLine($"{a.Id},\"{a.Timestamp:yyyy-MM-dd HH:mm:ss}\",\"{email}\",\"{a.ControllerName}\",\"{a.ActionName}\",\"{a.HttpMethod}\",\"{path}\",{a.StatusCode},{a.DurationMs},\"{a.IpAddress}\"");
            }

            var bytes = System.Text.Encoding.UTF8.GetBytes(sb.ToString());
            return File(bytes, "text/csv", $"Audit_Trail_{DateTime.Now:yyyyMMdd_HHmmss}.csv");
        }

        /// <summary>
        /// Menghapus log audit trail lama untuk pemeliharaan storage (DELETE /api/audit-trail/clear)
        /// </summary>
        /// <param name="keepDays">Jumlah hari data yang dipertahankan (default: 30 hari)</param>
        [HttpDelete("clear")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        public async Task<IActionResult> ClearOldLogs([FromQuery] int keepDays = 30)
        {
            if (keepDays < 1) keepDays = 30;
            var cutoff = DateTime.Now.AddDays(-keepDays);

            var oldLogs = await _db.AuditLogs.Where(a => a.Timestamp < cutoff).ToListAsync();
            var count = oldLogs.Count;

            _db.AuditLogs.RemoveRange(oldLogs);
            await _db.SaveChangesAsync();

            return Ok(ApiResponse<object>.Ok(new { deletedCount = count, cutoffDate = cutoff }, $"Berhasil membersihkan {count} log audit trail sebelum {cutoff:yyyy-MM-dd}."));
        }

        private static AuditLogResponseDto MapToResponseDto(AuditLog a)
        {
            return new AuditLogResponseDto
            {
                Id = a.Id,
                UserId = a.UserId,
                UserEmail = a.UserEmail,
                UserName = a.UserName,
                ControllerName = a.ControllerName,
                ActionName = a.ActionName,
                HttpMethod = a.HttpMethod,
                Path = a.Path,
                QueryString = a.QueryString,
                IpAddress = a.IpAddress,
                StatusCode = a.StatusCode,
                DurationMs = a.DurationMs,
                Timestamp = a.Timestamp,
                Details = a.Details
            };
        }
    }
}
