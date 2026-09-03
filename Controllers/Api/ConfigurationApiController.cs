using System.Data.Common;
using System.Diagnostics;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TrackerKerja.Data;
using TrackerKerja.Models;
using TrackerKerja.ViewModels;

namespace TrackerKerja.Controllers.Api
{
    [ApiController]
    [Route("api/configuration")]
    [Produces("application/json")]
    public class ConfigurationApiController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly IConfiguration _config;
        private readonly IWebHostEnvironment _env;
        private readonly UserManager<AppUser> _userManager;
        private readonly Services.IDatabaseExportService _exportService;

        public ConfigurationApiController(
            AppDbContext db,
            IConfiguration config,
            IWebHostEnvironment env,
            UserManager<AppUser> userManager,
            Services.IDatabaseExportService exportService)
        {
            _db = db;
            _config = config;
            _env = env;
            _userManager = userManager;
            _exportService = exportService;
        }

        /// <summary>
        /// Mengambil konfigurasi Global Base URL saat ini (GET /api/configuration/base-url)
        /// </summary>
        [HttpGet("base-url")]
        [ProducesResponseType(typeof(ApiResponse<SystemSettingResponseDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetBaseUrl()
        {
            var setting = await _db.SystemSettings.FirstOrDefaultAsync(s => s.Key == "GlobalBaseUrl");
            var baseUrl = setting?.Value ?? $"{Request.Scheme}://{Request.Host}";

            var dto = new SystemSettingResponseDto
            {
                GlobalBaseUrl = baseUrl,
                Description = setting?.Description ?? "Global Base URL aplikasi",
                UpdatedAt = setting?.UpdatedAt ?? DateTime.Now
            };

            return Ok(ApiResponse<SystemSettingResponseDto>.Ok(dto, "Global Base URL berhasil diambil."));
        }

        /// <summary>
        /// Memperbarui pengaturan Global Base URL (PUT /api/configuration/base-url)
        /// </summary>
        /// <param name="dto">Payload URL baru</param>
        [HttpPut("base-url")]
        [ProducesResponseType(typeof(ApiResponse<SystemSettingResponseDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> UpdateBaseUrl([FromBody] UpdateBaseUrlRequestDto dto)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                return BadRequest(ApiResponse<SystemSettingResponseDto>.Fail("Validasi URL gagal.", errors));
            }

            var cleanUrl = dto.BaseUrl.Trim().TrimEnd('/');
            var setting = await _db.SystemSettings.FirstOrDefaultAsync(s => s.Key == "GlobalBaseUrl");

            if (setting == null)
            {
                setting = new SystemSetting
                {
                    Key = "GlobalBaseUrl",
                    Value = cleanUrl,
                    Description = "Global Base URL untuk integrasi REST API, Swagger, dan Webhook",
                    UpdatedAt = DateTime.Now
                };
                _db.SystemSettings.Add(setting);
            }
            else
            {
                setting.Value = cleanUrl;
                setting.UpdatedAt = DateTime.Now;
            }

            await _db.SaveChangesAsync();

            var responseDto = new SystemSettingResponseDto
            {
                GlobalBaseUrl = setting.Value,
                Description = setting.Description,
                UpdatedAt = setting.UpdatedAt
            };

            return Ok(ApiResponse<SystemSettingResponseDto>.Ok(responseDto, "Global Base URL berhasil diperbarui."));
        }

        /// <summary>
        /// Mengambil informasi kapasitas dan statistik penyimpanan database (GET /api/configuration/database-capacity)
        /// </summary>
        [HttpGet("database-capacity")]
        [ProducesResponseType(typeof(ApiResponse<DatabaseCapacityInfoDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetDatabaseCapacity()
        {
            var dbPath = GetDbFilePath();
            var fileInfo = new FileInfo(dbPath);

            long fileSizeBytes = fileInfo.Exists ? fileInfo.Length : 0;
            var lastModified = fileInfo.Exists ? fileInfo.LastWriteTime : DateTime.Now;

            int pageSize = 4096;
            int pageCount = 0;
            int freelistCount = 0;
            string journalMode = "DELETE";

            try
            {
                var conn = _db.Database.GetDbConnection();
                var wasOpen = conn.State == System.Data.ConnectionState.Open;
                if (!wasOpen) await conn.OpenAsync();

                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "PRAGMA page_size;";
                    var res = await cmd.ExecuteScalarAsync();
                    if (res != null) pageSize = Convert.ToInt32(res);

                    cmd.CommandText = "PRAGMA page_count;";
                    var resCount = await cmd.ExecuteScalarAsync();
                    if (resCount != null) pageCount = Convert.ToInt32(resCount);

                    cmd.CommandText = "PRAGMA freelist_count;";
                    var resFree = await cmd.ExecuteScalarAsync();
                    if (resFree != null) freelistCount = Convert.ToInt32(resFree);

                    cmd.CommandText = "PRAGMA journal_mode;";
                    var resMode = await cmd.ExecuteScalarAsync();
                    if (resMode != null) journalMode = resMode.ToString() ?? "DELETE";
                }

                if (!wasOpen) await conn.CloseAsync();
            }
            catch { }

            var reclaimableBytes = (long)freelistCount * pageSize;

            // Get table record counts
            var tableStats = new Dictionary<string, int>
            {
                { "Tasks", await _db.Tasks.CountAsync() },
                { "Sessions", await _db.Sessions.CountAsync() },
                { "Projects", await _db.Projects.CountAsync() },
                { "Categories", await _db.Categories.CountAsync() },
                { "Notes", await _db.Notes.CountAsync() },
                { "NoteAttachments", await _db.NoteAttachments.CountAsync() },
                { "Users", await _db.Users.CountAsync() },
                { "AuditLogs", await _db.AuditLogs.CountAsync() },
                { "JsonHistories", await _db.JsonHistories.CountAsync() },
                { "ImportLogs", await _db.ImportLogs.CountAsync() },
                { "MasterPriorities", await _db.MasterPriorities.CountAsync() },
                { "MasterStatuses", await _db.MasterStatuses.CountAsync() }
            };

            // Attachments folder size
            long attachmentsSize = 0;
            var uploadsDir = Path.Combine(_env.WebRootPath ?? "wwwroot", "uploads");
            if (Directory.Exists(uploadsDir))
            {
                var files = Directory.GetFiles(uploadsDir, "*.*", SearchOption.AllDirectories);
                attachmentsSize = files.Sum(f => new FileInfo(f).Length);
            }

            var info = new DatabaseCapacityInfoDto
            {
                DatabaseFileName = Path.GetFileName(dbPath),
                DatabaseFilePath = dbPath,
                FileSizeBytes = fileSizeBytes,
                FileSizeFormatted = FormatBytes(fileSizeBytes),
                LastModified = lastModified,
                PageSize = pageSize,
                PageCount = pageCount,
                FreelistCount = freelistCount,
                ReclaimableBytes = reclaimableBytes,
                ReclaimableFormatted = FormatBytes(reclaimableBytes),
                JournalMode = journalMode,
                TableStats = tableStats,
                AttachmentsSizeBytes = attachmentsSize,
                AttachmentsSizeFormatted = FormatBytes(attachmentsSize)
            };

            return Ok(ApiResponse<DatabaseCapacityInfoDto>.Ok(info, "Informasi kapasitas database berhasil diambil."));
        }

        /// <summary>
        /// Melakukan kompresi (shrink / VACUUM) database untuk membersihkan ruang kosong (POST /api/configuration/shrink-database)
        /// </summary>
        [HttpPost("shrink-database")]
        [ProducesResponseType(typeof(ApiResponse<ShrinkDatabaseResponseDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> ShrinkDatabase()
        {
            var dbPath = GetDbFilePath();
            var fileInfo = new FileInfo(dbPath);
            long initialSize = fileInfo.Exists ? fileInfo.Length : 0;

            var sw = Stopwatch.StartNew();

            try
            {
                // Run VACUUM command in SQLite
                await _db.Database.ExecuteSqlRawAsync("VACUUM;");
            }
            catch (Exception ex)
            {
                return BadRequest(ApiResponse<ShrinkDatabaseResponseDto>.Fail($"Gagal melakukan shrink database: {ex.Message}"));
            }

            sw.Stop();

            fileInfo.Refresh();
            long finalSize = fileInfo.Exists ? fileInfo.Length : initialSize;
            long reclaimed = Math.Max(0, initialSize - finalSize);
            double reclaimedPercent = initialSize > 0 ? Math.Round((reclaimed / (double)initialSize) * 100, 2) : 0;

            var dto = new ShrinkDatabaseResponseDto
            {
                InitialSizeBytes = initialSize,
                InitialSizeFormatted = FormatBytes(initialSize),
                FinalSizeBytes = finalSize,
                FinalSizeFormatted = FormatBytes(finalSize),
                ReclaimedBytes = reclaimed,
                ReclaimedFormatted = FormatBytes(reclaimed),
                ReclaimedPercent = reclaimedPercent,
                ExecutionDurationMs = sw.ElapsedMilliseconds,
                Timestamp = DateTime.Now
            };

            return Ok(ApiResponse<ShrinkDatabaseResponseDto>.Ok(dto, $"Shrink database berhasil! Ruang kosong yang dikompresi: {dto.ReclaimedFormatted} ({reclaimedPercent}%)."));
        }

        /// <summary>
        /// Melakukan reset data database (POST /api/configuration/reset-database)
        /// </summary>
        /// <param name="dto">Payload reset (mode: 'transactional' / 'factory', confirmationCode: 'RESET-CONFIRM')</param>
        [HttpPost("reset-database")]
        [ProducesResponseType(typeof(ApiResponse<ResetDatabaseResponseDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> ResetDatabase([FromBody] ResetDatabaseRequestDto dto)
        {
            if (!string.Equals(dto.ConfirmationCode, "RESET-CONFIRM", StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest(ApiResponse<object>.Fail("Kode konfirmasi tidak valid. Harap masukkan 'RESET-CONFIRM' untuk menyetujui reset."));
            }

            var deletedCounts = new Dictionary<string, int>();
            var clearedTables = new List<string>();

            if (string.Equals(dto.Mode, "factory", StringComparison.OrdinalIgnoreCase))
            {
                // Mode Factory Reset: Bersihkan semua transaksi, notes, attachments, audit logs, json histories, import logs, dan tasks
                var sessionCount = await _db.Sessions.CountAsync();
                var noteAttachCount = await _db.NoteAttachments.CountAsync();
                var noteCount = await _db.Notes.CountAsync();
                var taskCount = await _db.Tasks.CountAsync();
                var auditCount = await _db.AuditLogs.CountAsync();
                var jsonCount = await _db.JsonHistories.CountAsync();
                var importCount = await _db.ImportLogs.CountAsync();

                _db.Sessions.RemoveRange(_db.Sessions);
                _db.NoteAttachments.RemoveRange(_db.NoteAttachments);
                _db.Notes.RemoveRange(_db.Notes);
                _db.Tasks.RemoveRange(_db.Tasks);
                _db.AuditLogs.RemoveRange(_db.AuditLogs);
                _db.JsonHistories.RemoveRange(_db.JsonHistories);
                _db.ImportLogs.RemoveRange(_db.ImportLogs);

                await _db.SaveChangesAsync();

                // Bersihkan file lampiran fisik jika ada
                var uploadsDir = Path.Combine(_env.WebRootPath ?? "wwwroot", "uploads");
                if (Directory.Exists(uploadsDir))
                {
                    try
                    {
                        var files = Directory.GetFiles(uploadsDir);
                        foreach (var f in files) System.IO.File.Delete(f);
                    }
                    catch { }
                }

                // Vacuum
                await _db.Database.ExecuteSqlRawAsync("VACUUM;");

                deletedCounts["Sessions"] = sessionCount;
                deletedCounts["NoteAttachments"] = noteAttachCount;
                deletedCounts["Notes"] = noteCount;
                deletedCounts["Tasks"] = taskCount;
                deletedCounts["AuditLogs"] = auditCount;
                deletedCounts["JsonHistories"] = jsonCount;
                deletedCounts["ImportLogs"] = importCount;

                clearedTables.AddRange(new[] { "Sessions", "NoteAttachments", "Notes", "Tasks", "AuditLogs", "JsonHistories", "ImportLogs" });

                var dbPath = GetDbFilePath();
                var finalSize = new FileInfo(dbPath).Length;

                var res = new ResetDatabaseResponseDto
                {
                    Mode = "factory",
                    ClearedTables = clearedTables,
                    DeletedCounts = deletedCounts,
                    FinalDatabaseSizeFormatted = FormatBytes(finalSize),
                    Message = "Factory reset berhasil. Seluruh data transaksi, catatan, dan tugas telah dibersihkan."
                };

                return Ok(ApiResponse<ResetDatabaseResponseDto>.Ok(res, res.Message));
            }
            else
            {
                // Mode Transactional: Bersihkan Sesi kerja, Audit Log, dan JSON Tools history
                var sessionCount = await _db.Sessions.CountAsync();
                var auditCount = await _db.AuditLogs.CountAsync();
                var jsonCount = await _db.JsonHistories.CountAsync();
                var importCount = await _db.ImportLogs.CountAsync();

                _db.Sessions.RemoveRange(_db.Sessions);
                _db.AuditLogs.RemoveRange(_db.AuditLogs);
                _db.JsonHistories.RemoveRange(_db.JsonHistories);
                _db.ImportLogs.RemoveRange(_db.ImportLogs);

                await _db.SaveChangesAsync();
                await _db.Database.ExecuteSqlRawAsync("VACUUM;");

                deletedCounts["Sessions"] = sessionCount;
                deletedCounts["AuditLogs"] = auditCount;
                deletedCounts["JsonHistories"] = jsonCount;
                deletedCounts["ImportLogs"] = importCount;

                clearedTables.AddRange(new[] { "Sessions", "AuditLogs", "JsonHistories", "ImportLogs" });

                var dbPath = GetDbFilePath();
                var finalSize = new FileInfo(dbPath).Length;

                var res = new ResetDatabaseResponseDto
                {
                    Mode = "transactional",
                    ClearedTables = clearedTables,
                    DeletedCounts = deletedCounts,
                    FinalDatabaseSizeFormatted = FormatBytes(finalSize),
                    Message = "Reset data transaksi (Sessions, Audit Logs, JSON History, Import Logs) berhasil."
                };

                return Ok(ApiResponse<ResetDatabaseResponseDto>.Ok(res, res.Message));
            }
        }

        /// <summary>
        /// Mengambil ringkasan OpenAPI Swagger dan daftar endpoint modul (GET /api/configuration/api-doc-summary)
        /// </summary>
        [HttpGet("api-doc-summary")]
        [ProducesResponseType(typeof(ApiResponse<ApiDocSummaryDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetApiDocSummary()
        {
            var setting = await _db.SystemSettings.FirstOrDefaultAsync(s => s.Key == "GlobalBaseUrl");
            var baseUrl = setting?.Value ?? $"{Request.Scheme}://{Request.Host}";

            var modules = new Dictionary<string, List<string>>
            {
                { "Auth", new List<string> { "POST /api/auth/login", "POST /api/auth/logout", "GET /api/auth/me", "POST /api/auth/change-password", "PUT /api/auth/profile" } },
                { "Tasks", new List<string> { "GET /api/tasks", "GET /api/tasks/{id}", "GET /api/tasks/summary", "GET /api/tasks/kanban", "POST /api/tasks", "PUT /api/tasks/{id}", "PUT /api/tasks/{id}/status", "DELETE /api/tasks/{id}", "DELETE /api/tasks/bulk", "DELETE /api/tasks/all", "POST /api/tasks/{id}/sessions", "DELETE /api/tasks/{id}/sessions/{sessionId}" } },
                { "Projects", new List<string> { "GET /api/projects", "GET /api/projects/{id}", "GET /api/projects/{id}/tasks", "GET /api/projects/summary", "POST /api/projects", "PUT /api/projects/{id}", "DELETE /api/projects/{id}" } },
                { "Notes", new List<string> { "GET /api/notes", "GET /api/notes/{id}", "GET /api/notes/categories", "POST /api/notes", "PUT /api/notes/{id}", "PUT /api/notes/{id}/pin", "POST /api/notes/{id}/pin", "POST /api/notes/{id}/attachments", "DELETE /api/notes/{id}/attachments/{attachmentId}", "DELETE /api/notes/{id}" } },
                { "Timesheets", new List<string> { "GET /api/timesheets", "GET /api/timesheets/summary", "GET /api/timesheets/{id}", "GET /api/timesheets/export-excel", "GET /api/timesheets/export-csv", "POST /api/timesheets", "POST /api/timesheets/start", "POST /api/timesheets/stop", "PUT /api/timesheets/{id}", "DELETE /api/timesheets/{id}", "DELETE /api/timesheets/all" } },
                { "Members", new List<string> { "GET /api/members", "GET /api/members/{id}", "GET /api/members/{id}/contributions", "POST /api/members", "PUT /api/members/{id}", "POST /api/members/{id}/toggle-lock", "DELETE /api/members/{id}" } },
                { "MasterData", new List<string> { "GET /api/master-data/all", "GET /api/master-data/categories", "POST /api/master-data/categories", "PUT /api/master-data/categories/{id}", "DELETE /api/master-data/categories/{id}", "GET /api/master-data/priorities", "POST /api/master-data/priorities", "PUT /api/master-data/priorities/{id}", "DELETE /api/master-data/priorities/{id}", "GET /api/master-data/statuses", "POST /api/master-data/statuses", "PUT /api/master-data/statuses/{id}", "DELETE /api/master-data/statuses/{id}", "GET /api/master-data/milestones", "POST /api/master-data/milestones", "PUT /api/master-data/milestones/{id}", "DELETE /api/master-data/milestones/{id}" } },
                { "Calendar", new List<string> { "GET /api/calendar/events" } },
                { "ImportExport", new List<string> { "GET /api/import/template", "POST /api/import/preview", "POST /api/import/execute", "GET /api/import/export-arms" } },
                { "JsonTools", new List<string> { "POST /api/json-tools/format", "POST /api/json-tools/minify", "POST /api/json-tools/validate", "POST /api/json-tools/save", "GET /api/json-tools/history", "GET /api/json-tools/history/{id}", "DELETE /api/json-tools/history/{id}" } },
                { "SqlTools", new List<string> { "POST /api/sql-tools/format", "POST /api/sql-tools/minify", "POST /api/sql-tools/validate", "POST /api/sql-tools/save", "GET /api/sql-tools/history", "GET /api/sql-tools/history/{id}", "DELETE /api/sql-tools/history/{id}" } },
                { "Notifications", new List<string> { "GET /api/notifications", "POST /api/notifications/{id}/read", "POST /api/notifications/read-all" } },
                { "Dashboard", new List<string> { "GET /api/dashboard/summary", "POST /api/dashboard/sync" } },
                { "Reports", new List<string> { "GET /api/reports/dashboard", "GET /api/reports/chart-data", "GET /api/reports/members-workload", "GET /api/reports/gantt" } },
                { "AuditTrail", new List<string> { "GET /api/audit-trail", "GET /api/audit-trail/{id}", "GET /api/audit-trail/stats", "GET /api/audit-trail/export-csv", "DELETE /api/audit-trail/clear" } },
                { "Configuration", new List<string> { "GET /api/configuration/base-url", "PUT /api/configuration/base-url", "GET /api/configuration/database-capacity", "POST /api/configuration/shrink-database", "POST /api/configuration/reset-database", "GET /api/configuration/api-doc-summary", "GET /api/configuration/export-database-file", "GET /api/configuration/export-sql-script" } }
            };

            var total = modules.Values.Sum(v => v.Count);

            var dto = new ApiDocSummaryDto
            {
                Title = "Work Tracker Pro REST API",
                Version = "v1",
                SwaggerUiUrl = $"{baseUrl}/swagger",
                OpenApiJsonUrl = $"{baseUrl}/swagger/v1/swagger.json",
                GlobalBaseUrl = baseUrl,
                TotalEndpoints = total,
                ModuleEndpoints = modules
            };

            return Ok(ApiResponse<ApiDocSummaryDto>.Ok(dto, "Informasi dokumentasi API berhasil diambil."));
        }

        /// <summary>
        /// Mengunduh cadangan snapshot file biner SQLite (.db) secara langsung (GET /api/configuration/export-database-file)
        /// </summary>
        [HttpGet("export-database-file")]
        [ProducesResponseType(typeof(FileResult), StatusCodes.Status200OK)]
        public async Task<IActionResult> ExportDatabaseFile()
        {
            try
            {
                var bytes = await _exportService.GetDatabaseBinarySnapshotAsync();
                var fileName = $"TrackerKerja_Backup_{DateTime.Now:yyyyMMdd_HHmmss}.db";
                return File(bytes, "application/x-sqlite3", fileName);
            }
            catch (Exception ex)
            {
                return BadRequest(ApiResponse<object>.Fail($"Gagal mengekspor file database: {ex.Message}"));
            }
        }

        /// <summary>
        /// Mengunduh skrip dump SQL lengkap (DDL Skema Tabel &amp; Data INSERT) (GET /api/configuration/export-sql-script)
        /// </summary>
        [HttpGet("export-sql-script")]
        [ProducesResponseType(typeof(FileResult), StatusCodes.Status200OK)]
        public async Task<IActionResult> ExportSqlScript()
        {
            try
            {
                var sql = await _exportService.GenerateFullSqlDumpAsync();
                var bytes = System.Text.Encoding.UTF8.GetBytes(sql);
                var fileName = $"TrackerKerja_Dump_{DateTime.Now:yyyyMMdd_HHmmss}.sql";
                return File(bytes, "application/sql", fileName);
            }
            catch (Exception ex)
            {
                return BadRequest(ApiResponse<object>.Fail($"Gagal mengekspor SQL script: {ex.Message}"));
            }
        }

        #region Helpers
        private string GetDbFilePath()
        {
            var connStr = _config.GetConnectionString("DefaultConnection") ?? "Data Source=trackerkerja.db";
            var parts = connStr.Split('=', StringSplitOptions.TrimEntries);
            var dbFileName = parts.Length > 1 ? parts[1] : "trackerkerja.db";

            if (Path.IsPathRooted(dbFileName))
            {
                return dbFileName;
            }

            return Path.Combine(Directory.GetCurrentDirectory(), dbFileName);
        }

        private static string FormatBytes(long bytes)
        {
            if (bytes < 1024) return $"{bytes} B";
            if (bytes < 1024 * 1024) return $"{bytes / 1024.0:F1} KB";
            if (bytes < 1024 * 1024 * 1024) return $"{bytes / (1024.0 * 1024.0):F2} MB";
            return $"{bytes / (1024.0 * 1024.0 * 1024.0):F2} GB";
        }
        #endregion
    }
}
