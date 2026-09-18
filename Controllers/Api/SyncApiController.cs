using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TrackerKerja.Data;
using TrackerKerja.Services;
using TrackerKerja.ViewModels;

namespace TrackerKerja.Controllers.Api
{
    [ApiController]
    [Route("api/sync")]
    [Produces("application/json")]
    [Authorize]
    public class SyncApiController : ControllerBase
    {
        private readonly IDatabaseSyncService _syncService;
        private readonly AppDbContext _db;

        public SyncApiController(IDatabaseSyncService syncService, AppDbContext db)
        {
            _syncService = syncService;
            _db = db;
        }

        /// <summary>
        /// Pengujian koneksi (Ping) &amp; validasi API Key Host Induk (GET /api/sync/ping)
        /// </summary>
        [HttpGet("ping")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(ApiResponse<SyncPingResponseDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> Ping()
        {
            // Optional: verify header if provided
            if (Request.Headers.TryGetValue("X-Sync-ApiKey", out var apiKeyHeader))
            {
                var isValid = await _syncService.VerifyApiKeyAsync(apiKeyHeader.ToString());
                if (!isValid)
                {
                    return Unauthorized(ApiResponse<object>.Fail("Autentikasi gagal: API Key sinkronisasi tidak valid."));
                }
            }

            var totalTasks = await _db.Tasks.CountAsync();
            var totalSessions = await _db.Sessions.CountAsync();
            var totalProjects = await _db.Projects.CountAsync();
            var totalUsers = await _db.Users.CountAsync();
            var (fileCount, totalBytes, formattedSize) = await _syncService.GetUploadsStatsAsync();

            var setting = await _db.SystemSettings.FirstOrDefaultAsync(s => s.Key == "GlobalBaseUrl");
            var hostUrl = setting?.Value ?? $"{Request.Scheme}://{Request.Host}";

            var dto = new SyncPingResponseDto
            {
                IsOnline = true,
                HostName = $"TrackerKerja Host ({hostUrl})",
                AppVersion = "v3.1",
                DatabaseType = "SQLite (WAL Mode)",
                ServerTime = DateTime.Now,
                TotalTasks = totalTasks,
                TotalSessions = totalSessions,
                TotalProjects = totalProjects,
                TotalUsers = totalUsers,
                TotalUploadFiles = fileCount,
                TotalUploadsSizeBytes = totalBytes,
                TotalUploadsFormatted = formattedSize,
                Message = "Host Induk siap menerima sinkronisasi data dan berkas dari child application."
            };

            return Ok(ApiResponse<SyncPingResponseDto>.Ok(dto, "Koneksi Host Induk aktif dan terverifikasi."));
        }

        /// <summary>
        /// Menerima payload sinkronisasi dari Child Application di Host Induk (POST /api/sync/receive)
        /// </summary>
        /// <param name="request">Payload sinkronisasi berisi SQL script, arsip zip berkas uploads, dan opsi pembersihan</param>
        [HttpPost("receive")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(ApiResponse<SyncResultDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> ReceiveSync([FromBody] SyncReceiveRequestDto request)
        {
            // Verifikasi API Key
            string? apiKey = null;
            if (Request.Headers.TryGetValue("X-Sync-ApiKey", out var keyVal))
            {
                apiKey = keyVal.ToString();
            }

            var isValid = await _syncService.VerifyApiKeyAsync(apiKey ?? string.Empty);
            if (!isValid)
            {
                return Unauthorized(ApiResponse<object>.Fail("Akses Ditolak: Header 'X-Sync-ApiKey' hilang atau tidak cocok dengan pengaturan Host Induk."));
            }

            if (string.IsNullOrWhiteSpace(request?.SqlScript))
            {
                return BadRequest(ApiResponse<object>.Fail("Payload 'SqlScript' tidak boleh kosong."));
            }

            var result = await _syncService.ExecuteSqlSyncAsync(
                request.SqlScript,
                request.CleanBeforeSync,
                request.BackupBeforeSync,
                request.SourceLabel ?? request.SourceInstanceUrl,
                request.FilesZipBase64);

            if (!result.Success)
            {
                return BadRequest(ApiResponse<SyncResultDto>.Fail(result.Message, result.ErrorDetails != null ? new List<string> { result.ErrorDetails } : null));
            }

            return Ok(ApiResponse<SyncResultDto>.Ok(result, result.Message));
        }

        /// <summary>
        /// Memicu proses pengiriman sinkronisasi data &amp; berkas dari instance ini ke Host Induk target (POST /api/sync/push)
        /// </summary>
        [HttpPost("push")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(ApiResponse<SyncResultDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> PushToHost([FromBody] SyncPushRequestDto request)
        {
            var isApiKeyValid = (Request.Headers.TryGetValue("X-Sync-ApiKey", out var hKey) && await _syncService.VerifyApiKeyAsync(hKey.ToString()))
                || (!string.IsNullOrWhiteSpace(request?.ApiKey) && await _syncService.VerifyApiKeyAsync(request.ApiKey));
            var isAuthorizedUser = User?.IsInRole("Admin") == true || (User?.Identity?.IsAuthenticated == true);

            if (!isApiKeyValid && !isAuthorizedUser)
            {
                return Unauthorized(ApiResponse<object>.Fail("Akses Ditolak: Diperlukan hak akses Administrator atau API Key sinkronisasi yang valid."));
            }

            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                return BadRequest(ApiResponse<object>.Fail("Validasi request gagal.", errors));
            }

            var settings = await _syncService.GetSyncSettingsAsync();
            var targetUrl = !string.IsNullOrWhiteSpace(request?.TargetHostUrl) ? request.TargetHostUrl : settings.TargetHostUrl;
            var apiKey = !string.IsNullOrWhiteSpace(request?.ApiKey) ? request.ApiKey : settings.ApiKey;

            var result = await _syncService.PushSyncToHostAsync(
                targetUrl,
                apiKey,
                request.CleanBeforeSync,
                request.BackupBeforeSync,
                request.SourceLabel,
                request.SyncFiles);

            if (!result.Success)
            {
                return BadRequest(ApiResponse<SyncResultDto>.Fail(result.Message, result.ErrorDetails != null ? new List<string> { result.ErrorDetails } : null));
            }

            return Ok(ApiResponse<SyncResultDto>.Ok(result, result.Message));
        }

        /// <summary>
        /// Menarik (Pull) seluruh data &amp; berkas dari Host Induk ke instance ini (POST /api/sync/pull)
        /// </summary>
        [HttpPost("pull")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(ApiResponse<SyncResultDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> PullFromHost([FromBody] SyncPullRequestDto request)
        {
            var isApiKeyValid = (Request.Headers.TryGetValue("X-Sync-ApiKey", out var hKey) && await _syncService.VerifyApiKeyAsync(hKey.ToString()))
                || (!string.IsNullOrWhiteSpace(request?.ApiKey) && await _syncService.VerifyApiKeyAsync(request.ApiKey));
            var isAuthorizedUser = User?.IsInRole("Admin") == true || (User?.Identity?.IsAuthenticated == true);

            if (!isApiKeyValid && !isAuthorizedUser)
            {
                return Unauthorized(ApiResponse<object>.Fail("Akses Ditolak: Diperlukan hak akses Administrator atau API Key sinkronisasi yang valid."));
            }

            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                return BadRequest(ApiResponse<object>.Fail("Validasi request gagal.", errors));
            }

            var settings = await _syncService.GetSyncSettingsAsync();
            var targetUrl = !string.IsNullOrWhiteSpace(request?.TargetHostUrl) ? request.TargetHostUrl : settings.TargetHostUrl;
            var apiKey = !string.IsNullOrWhiteSpace(request?.ApiKey) ? request.ApiKey : settings.ApiKey;

            var result = await _syncService.PullSyncFromHostAsync(
                targetUrl,
                apiKey,
                request.CleanBeforeSync,
                request.BackupBeforeSync);

            if (!result.Success)
            {
                return BadRequest(ApiResponse<SyncResultDto>.Fail(result.Message, result.ErrorDetails != null ? new List<string> { result.ErrorDetails } : null));
            }

            return Ok(ApiResponse<SyncResultDto>.Ok(result, result.Message));
        }

        /// <summary>
        /// Mengunduh Paket Lengkap Sinkronisasi (.zip) berisi SQL dump dan folder uploads (GET /api/sync/export-package)
        /// </summary>
        [HttpGet("export-package")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(FileResult), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> ExportSyncPackage([FromQuery] bool cleanBeforeSync = true)
        {
            var isApiKeyValid = Request.Headers.TryGetValue("X-Sync-ApiKey", out var apiKeyHeader)
                && await _syncService.VerifyApiKeyAsync(apiKeyHeader.ToString());
            var isAuthorizedUser = User?.IsInRole("Admin") == true || (User?.Identity?.IsAuthenticated == true);

            if (!isApiKeyValid && !isAuthorizedUser)
            {
                return Unauthorized(ApiResponse<object>.Fail("Akses Ditolak: Diperlukan API Key sinkronisasi (X-Sync-ApiKey) yang valid atau otorisasi Administrator."));
            }

            try
            {
                var zipBytes = await _syncService.GenerateFullSyncPackageZipAsync(cleanBeforeSync);
                var fileName = $"TrackerKerja_SyncPackage_{DateTime.Now:yyyyMMdd_HHmmss}.zip";
                return File(zipBytes, "application/zip", fileName);
            }
            catch (Exception ex)
            {
                return BadRequest(ApiResponse<object>.Fail($"Gagal membuat paket sinkronisasi: {ex.Message}"));
            }
        }

        /// <summary>
        /// Mengunggah paket sinkronisasi (.zip atau .sql) untuk diterapkan di server ini (POST /api/sync/import-package)
        /// </summary>
        [HttpPost("import-package")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(ApiResponse<SyncResultDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> ImportPackageFile([FromForm] SyncPackageUploadRequest request)
        {
            var isApiKeyValid = Request.Headers.TryGetValue("X-Sync-ApiKey", out var apiKeyHeader)
                && await _syncService.VerifyApiKeyAsync(apiKeyHeader.ToString());
            var isAuthorizedUser = User?.IsInRole("Admin") == true || (User?.Identity?.IsAuthenticated == true);

            if (!isApiKeyValid && !isAuthorizedUser)
            {
                return Unauthorized(ApiResponse<object>.Fail("Akses Ditolak: Diperlukan hak akses Administrator atau API Key sinkronisasi yang valid."));
            }

            if (request.PackageFile == null || request.PackageFile.Length == 0)
            {
                return BadRequest(ApiResponse<object>.Fail("Berkas paket sinkronisasi tidak boleh kosong. Silakan pilih berkas .zip atau .sql yang valid."));
            }

            var fileName = request.PackageFile.FileName;
            var isZip = fileName.EndsWith(".zip", StringComparison.OrdinalIgnoreCase);
            var isSql = fileName.EndsWith(".sql", StringComparison.OrdinalIgnoreCase);

            if (!isZip && !isSql)
            {
                return BadRequest(ApiResponse<object>.Fail("Format berkas tidak didukung. Harap unggah paket berformat .zip atau .sql."));
            }

            try
            {
                using var stream = request.PackageFile.OpenReadStream();
                SyncResultDto result;

                if (isZip)
                {
                    result = await _syncService.ExecutePackageSyncAsync(
                        stream,
                        request.CleanBeforeSync,
                        request.BackupBeforeSync,
                        $"Upload Paket .zip ({fileName})");
                }
                else
                {
                    string sqlContent;
                    using (var reader = new StreamReader(stream))
                    {
                        sqlContent = await reader.ReadToEndAsync();
                    }

                    result = await _syncService.ExecuteSqlSyncAsync(
                        sqlContent,
                        request.CleanBeforeSync,
                        request.BackupBeforeSync,
                        $"Upload Berkas .sql ({fileName})");
                }

                if (!result.Success)
                {
                    return BadRequest(ApiResponse<SyncResultDto>.Fail(result.Message, result.ErrorDetails != null ? new List<string> { result.ErrorDetails } : null));
                }

                return Ok(ApiResponse<SyncResultDto>.Ok(result, result.Message));
            }
            catch (Exception ex)
            {
                return BadRequest(ApiResponse<object>.Fail($"Terjadi kesalahan saat mengeksekusi paket sinkronisasi: {ex.Message}"));
            }
        }

        /// <summary>
        /// Mengunggah berkas .sql secara manual untuk dieksekusi dan disinkronkan di Host Induk (POST /api/sync/import-sql)
        /// </summary>
        [HttpPost("import-sql")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(ApiResponse<SyncResultDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> ImportSqlFile([FromForm] SyncSqlUploadRequest request)
        {
            var isApiKeyValid = Request.Headers.TryGetValue("X-Sync-ApiKey", out var apiKeyHeader)
                && await _syncService.VerifyApiKeyAsync(apiKeyHeader.ToString());
            var isAuthorizedUser = User?.IsInRole("Admin") == true || (User?.Identity?.IsAuthenticated == true);

            if (!isApiKeyValid && !isAuthorizedUser)
            {
                return Unauthorized(ApiResponse<object>.Fail("Akses Ditolak: Diperlukan hak akses Administrator atau API Key sinkronisasi yang valid."));
            }

            if (request.SqlFile == null || request.SqlFile.Length == 0)
            {
                return BadRequest(ApiResponse<object>.Fail("File SQL tidak boleh kosong. Silakan pilih file .sql yang valid."));
            }

            string sqlContent;
            using (var reader = new StreamReader(request.SqlFile.OpenReadStream()))
            {
                sqlContent = await reader.ReadToEndAsync();
            }

            var result = await _syncService.ExecuteSqlSyncAsync(
                sqlContent,
                request.CleanBeforeSync,
                request.BackupBeforeSync,
                $"Upload Berkas SQL ({request.SqlFile.FileName})");

            if (!result.Success)
            {
                return BadRequest(ApiResponse<SyncResultDto>.Fail(result.Message, result.ErrorDetails != null ? new List<string> { result.ErrorDetails } : null));
            }

            return Ok(ApiResponse<SyncResultDto>.Ok(result, result.Message));
        }

        /// <summary>
        /// Mengunduh seluruh data dalam naskah paket SQL sinkronisasi siap impor (GET /api/sync/export-sql)
        /// </summary>
        [HttpGet("export-sql")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(FileResult), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> ExportSqlScript([FromQuery] bool cleanBeforeSync = true)
        {
            var isApiKeyValid = Request.Headers.TryGetValue("X-Sync-ApiKey", out var apiKeyHeader)
                && await _syncService.VerifyApiKeyAsync(apiKeyHeader.ToString());
            var isAuthorizedUser = User?.IsInRole("Admin") == true || (User?.Identity?.IsAuthenticated == true);

            if (!isApiKeyValid && !isAuthorizedUser)
            {
                return Unauthorized(ApiResponse<object>.Fail("Akses Ditolak: Diperlukan API Key sinkronisasi (X-Sync-ApiKey) yang valid atau otorisasi Administrator."));
            }

            try
            {
                var sql = await _syncService.GenerateSyncSqlDumpAsync(cleanBeforeSync);
                var bytes = System.Text.Encoding.UTF8.GetBytes(sql);
                var fileName = $"TrackerKerja_SyncPayload_{DateTime.Now:yyyyMMdd_HHmmss}.sql";
                return File(bytes, "application/sql", fileName);
            }
            catch (Exception ex)
            {
                return BadRequest(ApiResponse<object>.Fail($"Gagal membuat naskah SQL sinkronisasi: {ex.Message}"));
            }
        }

        /// <summary>
        /// Mengambil konfigurasi sinkronisasi saat ini (GET /api/sync/settings)
        /// </summary>
        [HttpGet("settings")]
        [ProducesResponseType(typeof(ApiResponse<SyncSettingsDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetSettings()
        {
            var settings = await _syncService.GetSyncSettingsAsync();
            return Ok(ApiResponse<SyncSettingsDto>.Ok(settings, "Pengaturan sinkronisasi berhasil diambil."));
        }

        /// <summary>
        /// Menyimpan konfigurasi sinkronisasi (PUT /api/sync/settings)
        /// </summary>
        [HttpPut("settings")]
        [ProducesResponseType(typeof(ApiResponse<SyncSettingsDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> UpdateSettings([FromBody] SyncSettingsDto settings)
        {
            await _syncService.SaveSyncSettingsAsync(settings);
            return Ok(ApiResponse<SyncSettingsDto>.Ok(settings, "Pengaturan sinkronisasi berhasil diperbarui."));
        }
    }
}
