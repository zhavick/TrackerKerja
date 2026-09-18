using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TrackerKerja.Data;
using TrackerKerja.Models;
using TrackerKerja.ViewModels;

namespace TrackerKerja.Controllers
{
    [Authorize(Roles = "Admin")]
    public class ConfigurationController : Controller
    {
        private readonly AppDbContext _db;
        private readonly IConfiguration _config;
        private readonly IWebHostEnvironment _env;
        private readonly UserManager<AppUser> _userManager;
        private readonly Services.IDatabaseExportService _exportService;
        private readonly Services.IDatabaseSyncService _syncService;
        private readonly Services.IEmailService _emailService;

        public ConfigurationController(
            AppDbContext db,
            IConfiguration config,
            IWebHostEnvironment env,
            UserManager<AppUser> userManager,
            Services.IDatabaseExportService exportService,
            Services.IDatabaseSyncService syncService,
            Services.IEmailService emailService)
        {
            _db = db;
            _config = config;
            _env = env;
            _userManager = userManager;
            _exportService = exportService;
            _syncService = syncService;
            _emailService = emailService;
        }

        public async Task<IActionResult> Index()
        {
            ViewData["Title"] = "Konfigurasi Sistem & Database";

            // 1. Global Base URL
            var setting = await _db.SystemSettings.FirstOrDefaultAsync(s => s.Key == "GlobalBaseUrl");
            var baseUrl = setting?.Value ?? $"{Request.Scheme}://{Request.Host}";
            ViewBag.GlobalBaseUrl = baseUrl;
            ViewBag.BaseUrlUpdatedAt = setting?.UpdatedAt ?? DateTime.Now;

            // 1.5. Global App Typography Font
            var fontSetting = await _db.SystemSettings.FirstOrDefaultAsync(s => s.Key == "GlobalAppFont");
            ViewBag.GlobalAppFont = fontSetting?.Value ?? "inter";

            // 2. Database Capacity Info
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

            var tableStats = new Dictionary<string, int>
            {
                { "Tugas (Tasks)", await _db.Tasks.CountAsync() },
                { "Sesi Kerja (Sessions)", await _db.Sessions.CountAsync() },
                { "Proyek (Projects)", await _db.Projects.CountAsync() },
                { "Kategori (Categories)", await _db.Categories.CountAsync() },
                { "Catatan (WorkNotes)", await _db.Notes.CountAsync() },
                { "Lampiran Catatan", await _db.NoteAttachments.CountAsync() },
                { "Pengguna / Anggota Tim", await _db.Users.CountAsync() },
                { "Audit Log", await _db.AuditLogs.CountAsync() },
                { "Riwayat JSON Tools", await _db.JsonHistories.CountAsync() },
                { "Riwayat Import Excel", await _db.ImportLogs.CountAsync() }
            };

            long attachmentsSize = 0;
            var uploadsDir = Path.Combine(_env.WebRootPath ?? "wwwroot", "uploads");
            if (Directory.Exists(uploadsDir))
            {
                var files = Directory.GetFiles(uploadsDir, "*.*", SearchOption.AllDirectories);
                attachmentsSize = files.Sum(f => new FileInfo(f).Length);
            }

            ViewBag.DatabaseFileName = Path.GetFileName(dbPath);
            ViewBag.DatabaseFilePath = dbPath;
            ViewBag.FileSizeBytes = fileSizeBytes;
            ViewBag.FileSizeFormatted = FormatBytes(fileSizeBytes);
            ViewBag.LastModified = lastModified;
            ViewBag.PageSize = pageSize;
            ViewBag.PageCount = pageCount;
            ViewBag.FreelistCount = freelistCount;
            ViewBag.ReclaimableFormatted = FormatBytes(reclaimableBytes);
            ViewBag.JournalMode = journalMode;
            ViewBag.TableStats = tableStats;
            ViewBag.AttachmentsSizeFormatted = FormatBytes(attachmentsSize);

            // 3. API Doc info
            ViewBag.SwaggerUiUrl = $"{baseUrl}/swagger";
            ViewBag.OpenApiJsonUrl = $"{baseUrl}/swagger/v1/swagger.json";

            // 4. Host Synchronization Settings
            var syncSettings = await _syncService.GetSyncSettingsAsync();
            ViewBag.SyncSettings = syncSettings;
            ViewBag.HostSyncTargetUrl = syncSettings.TargetHostUrl;
            ViewBag.HostSyncApiKey = syncSettings.ApiKey;
            ViewBag.HostSyncRole = syncSettings.Role;
            ViewBag.LastSyncAt = syncSettings.LastSyncAt;
            ViewBag.LastSyncStatus = syncSettings.LastSyncStatus;

            // 5. Email SMTP Configuration & Event Templates
            var emailConfig = await _emailService.GetEmailConfigAsync();
            var emailTemplates = await _emailService.GetAllTemplatesAsync();
            ViewBag.EmailConfig = emailConfig;
            ViewBag.EmailTemplates = emailTemplates;

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateBaseUrl(string baseUrl)
        {
            if (string.IsNullOrWhiteSpace(baseUrl) || !Uri.TryCreate(baseUrl, UriKind.Absolute, out _))
            {
                TempData["Error"] = "Format URL tidak valid. Harap masukkan URL lengkap seperti http://localhost:5000 atau https://domain.com";
                return RedirectToAction(nameof(Index));
            }

            var cleanUrl = baseUrl.Trim().TrimEnd('/');
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
            TempData["Success"] = $"Global Base URL berhasil diperbarui menjadi '{cleanUrl}'!";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> UpdateGlobalFont([FromForm] string fontName)
        {
            if (string.IsNullOrWhiteSpace(fontName))
            {
                return BadRequest(new { success = false, message = "Nama font tidak boleh kosong" });
            }

            var allowedFonts = new[] { "inter", "jakarta", "outfit", "poppins", "roboto" };
            var selectedFont = fontName.Trim().ToLowerInvariant();
            if (!allowedFonts.Contains(selectedFont))
            {
                selectedFont = "inter";
            }

            var setting = await _db.SystemSettings.FirstOrDefaultAsync(s => s.Key == "GlobalAppFont");
            if (setting == null)
            {
                setting = new SystemSetting
                {
                    Key = "GlobalAppFont",
                    Value = selectedFont,
                    Description = "Global typography font family setting (Inter, Plus Jakarta Sans, Outfit, Poppins, Roboto)",
                    UpdatedAt = DateTime.Now
                };
                _db.SystemSettings.Add(setting);
            }
            else
            {
                setting.Value = selectedFont;
                setting.UpdatedAt = DateTime.Now;
            }

            await _db.SaveChangesAsync();

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest" || Request.Headers.Accept.ToString().Contains("application/json"))
            {
                return Json(new { success = true, font = selectedFont, message = "Font berhasil diperbarui" });
            }

            TempData["Success"] = $"Tipografi global aplikasi berhasil diatur ke '{selectedFont}'!";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ShrinkDatabase()
        {
            var dbPath = GetDbFilePath();
            var fileInfo = new FileInfo(dbPath);
            long initialSize = fileInfo.Exists ? fileInfo.Length : 0;

            var sw = Stopwatch.StartNew();
            try
            {
                await _db.Database.ExecuteSqlRawAsync("VACUUM;");
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Gagal melakukan shrink database: {ex.Message}";
                return RedirectToAction(nameof(Index));
            }
            sw.Stop();

            fileInfo.Refresh();
            long finalSize = fileInfo.Exists ? fileInfo.Length : initialSize;
            long reclaimed = Math.Max(0, initialSize - finalSize);
            double reclaimedPercent = initialSize > 0 ? Math.Round((reclaimed / (double)initialSize) * 100, 2) : 0;

            TempData["Success"] = $"Kompresi (Shrink) database berhasil dalam {sw.ElapsedMilliseconds}ms! Ukuran berkurang dari {FormatBytes(initialSize)} menjadi {FormatBytes(finalSize)} (hemat {FormatBytes(reclaimed)} / {reclaimedPercent}%).";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetDatabase(string mode, string confirmationCode)
        {
            if (!string.Equals(confirmationCode, "RESET-CONFIRM", StringComparison.OrdinalIgnoreCase))
            {
                TempData["Error"] = "Kode konfirmasi salah! Ketikkan 'RESET-CONFIRM' dengan tepat untuk menyetujui reset data.";
                return RedirectToAction(nameof(Index));
            }

            if (string.Equals(mode, "factory", StringComparison.OrdinalIgnoreCase))
            {
                _db.Sessions.RemoveRange(_db.Sessions);
                _db.NoteAttachments.RemoveRange(_db.NoteAttachments);
                _db.Notes.RemoveRange(_db.Notes);
                _db.Tasks.RemoveRange(_db.Tasks);
                _db.AuditLogs.RemoveRange(_db.AuditLogs);
                _db.JsonHistories.RemoveRange(_db.JsonHistories);
                _db.ImportLogs.RemoveRange(_db.ImportLogs);

                await _db.SaveChangesAsync();

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

                await _db.Database.ExecuteSqlRawAsync("VACUUM;");
                TempData["Success"] = "Factory reset berhasil! Seluruh data transaksi, catatan, dan tugas telah dibersihkan.";
            }
            else
            {
                _db.Sessions.RemoveRange(_db.Sessions);
                _db.AuditLogs.RemoveRange(_db.AuditLogs);
                _db.JsonHistories.RemoveRange(_db.JsonHistories);
                _db.ImportLogs.RemoveRange(_db.ImportLogs);

                await _db.SaveChangesAsync();
                await _db.Database.ExecuteSqlRawAsync("VACUUM;");
                TempData["Success"] = "Reset data transaksi (Sessions, Audit Logs, JSON History, Import Logs) berhasil!";
            }

            return RedirectToAction(nameof(Index));
        }

        // ── EXPORT DATABASE FILE (.DB) ──────────────────────────
        [HttpGet]
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
                TempData["Error"] = $"Gagal mengekspor file database: {ex.Message}";
                return RedirectToAction(nameof(Index));
            }
        }

        // ── EXPORT SQL SCRIPT (DDL & DATA .SQL) ─────────────────
        [HttpGet]
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
                TempData["Error"] = $"Gagal mengekspor SQL Script: {ex.Message}";
                return RedirectToAction(nameof(Index));
            }
        }

        // ── SQL PREVIEW API ────────────────────────────────────
        [HttpGet]
        public async Task<IActionResult> GetSqlPreview()
        {
            try
            {
                var sql = await _exportService.GenerateFullSqlDumpAsync();
                var preview = sql.Length > 12000 
                    ? sql.Substring(0, 12000) + "\n\n-- ... [Sisa data script dipotong untuk pratinjau cepat. Silakan klik 'Unduh SQL Script (.sql)' untuk mengunduh berkas lengkap] ..." 
                    : sql;

                return Json(new { success = true, preview, totalLength = sql.Length });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // ── SINKRONISASI HOST INDUK ACTIONS ───────────────────
        
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateSyncSettings(SyncSettingsDto dto)
        {
            if (dto == null)
            {
                TempData["Error"] = "Data pengaturan sinkronisasi tidak valid.";
                return RedirectToAction(nameof(Index));
            }

            await _syncService.SaveSyncSettingsAsync(dto);
            TempData["Success"] = "Pengaturan Sinkronisasi Host Induk berhasil disimpan!";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PushSyncToHost(string targetUrl, string apiKey, bool cleanBeforeSync = true, bool backupBeforeSync = true, bool syncFiles = true)
        {
            if (string.IsNullOrWhiteSpace(targetUrl))
            {
                TempData["Error"] = "Target URL Host Induk tidak boleh kosong.";
                return RedirectToAction(nameof(Index));
            }

            var setting = await _db.SystemSettings.FirstOrDefaultAsync(s => s.Key == "GlobalBaseUrl");
            var sourceLabel = setting?.Value ?? $"{Request.Scheme}://{Request.Host}";

            var result = await _syncService.PushSyncToHostAsync(targetUrl, apiKey, cleanBeforeSync, backupBeforeSync, sourceLabel, syncFiles);
            if (result.Success)
            {
                TempData["Success"] = $"Sinkronisasi online berhasil dikirim ke Host Induk ({targetUrl.Trim().TrimEnd('/')})! {result.Message}";
            }
            else
            {
                TempData["Error"] = $"Gagal mengirim sinkronisasi ke Host Induk: {result.Message}";
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PullSyncFromHost(string targetUrl, string apiKey, bool cleanBeforeSync = true, bool backupBeforeSync = true)
        {
            if (string.IsNullOrWhiteSpace(targetUrl))
            {
                TempData["Error"] = "Target URL Host Induk tidak boleh kosong.";
                return RedirectToAction(nameof(Index));
            }

            var result = await _syncService.PullSyncFromHostAsync(targetUrl, apiKey, cleanBeforeSync, backupBeforeSync);
            if (result.Success)
            {
                TempData["Success"] = $"Tarik data & berkas berhasil dari Host Induk ({targetUrl.Trim().TrimEnd('/')})! {result.Message}";
            }
            else
            {
                TempData["Error"] = $"Gagal menarik data dari Host Induk: {result.Message}";
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> TestHostConnection([FromBody] SyncPushRequestDto req)
        {
            if (string.IsNullOrWhiteSpace(req?.TargetHostUrl))
            {
                return Json(new { success = false, message = "URL Host Induk tidak boleh kosong." });
            }

            var res = await _syncService.PingHostAsync(req.TargetHostUrl, req.ApiKey ?? string.Empty);
            return Json(new
            {
                success = res.IsOnline,
                message = res.Message,
                data = res
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadAndSyncPackage(IFormFile packageFile, bool cleanBeforeSync = true, bool backupBeforeSync = true)
        {
            if (packageFile == null || packageFile.Length == 0)
            {
                TempData["Error"] = "Silakan pilih berkas paket (.zip) atau naskah SQL (.sql) yang valid untuk disinkronkan.";
                return RedirectToAction(nameof(Index));
            }

            var fileName = packageFile.FileName;
            var isZip = fileName.EndsWith(".zip", StringComparison.OrdinalIgnoreCase);
            var isSql = fileName.EndsWith(".sql", StringComparison.OrdinalIgnoreCase);

            if (!isZip && !isSql)
            {
                TempData["Error"] = "Format berkas tidak didukung. Harap unggah berkas berformat .zip atau .sql.";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                using var stream = packageFile.OpenReadStream();
                SyncResultDto result;

                if (isZip)
                {
                    result = await _syncService.ExecutePackageSyncAsync(
                        stream,
                        cleanBeforeSync,
                        backupBeforeSync,
                        $"Upload Manual Paket '{fileName}'");
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
                        cleanBeforeSync,
                        backupBeforeSync,
                        $"Upload Manual SQL '{fileName}'");
                }

                if (result.Success)
                {
                    var backupInfo = !string.IsNullOrEmpty(result.BackupFileName) ? $" (Backup otomatis dibuat di folder backups/{result.BackupFileName})" : "";
                    TempData["Success"] = $"Sinkronisasi paket berhasil! {result.Message}{backupInfo}";
                }
                else
                {
                    TempData["Error"] = $"Gagal mengeksekusi sinkronisasi paket: {result.Message}";
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Terjadi kesalahan saat memproses paket sinkronisasi: {ex.Message}";
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadAndSyncSql(IFormFile sqlFile, bool cleanBeforeSync = true, bool backupBeforeSync = true)
        {
            if (sqlFile == null || sqlFile.Length == 0)
            {
                TempData["Error"] = "Silakan pilih berkas SQL (.sql) yang valid untuk disinkronkan.";
                return RedirectToAction(nameof(Index));
            }

            if (!sqlFile.FileName.EndsWith(".sql", StringComparison.OrdinalIgnoreCase))
            {
                TempData["Error"] = "Format berkas tidak didukung. Harap unggah berkas dengan ekstensi .sql.";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                string sqlContent;
                using (var reader = new StreamReader(sqlFile.OpenReadStream()))
                {
                    sqlContent = await reader.ReadToEndAsync();
                }

                var result = await _syncService.ExecuteSqlSyncAsync(sqlContent, cleanBeforeSync, backupBeforeSync, $"Upload Manual '{sqlFile.FileName}'");
                if (result.Success)
                {
                    var backupInfo = !string.IsNullOrEmpty(result.BackupFileName) ? $" (Backup otomatis dibuat di folder backups/{result.BackupFileName})" : "";
                    TempData["Success"] = $"Sinkronisasi file SQL berhasil! {result.AffectedTables.Count} tabel diperbarui dalam {result.ExecutionDurationMs}ms.{backupInfo}";
                }
                else
                {
                    TempData["Error"] = $"Gagal mengeksekusi sinkronisasi file SQL: {result.Message}";
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Terjadi kesalahan saat membaca file SQL: {ex.Message}";
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> ExportSyncPackage([FromQuery] bool cleanBeforeSync = true)
        {
            try
            {
                var zipBytes = await _syncService.GenerateFullSyncPackageZipAsync(cleanBeforeSync);
                var fileName = $"TrackerKerja_FullSyncPackage_{DateTime.Now:yyyyMMdd_HHmmss}.zip";
                return File(zipBytes, "application/zip", fileName);
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Gagal mengekspor Paket Lengkap Sinkronisasi: {ex.Message}";
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpGet]
        public async Task<IActionResult> ExportSyncSql([FromQuery] bool cleanBeforeSync = true)
        {
            try
            {
                var sql = await _syncService.GenerateSyncSqlDumpAsync(cleanBeforeSync);
                var bytes = System.Text.Encoding.UTF8.GetBytes(sql);
                var fileName = $"TrackerKerja_Sync_{DateTime.Now:yyyyMMdd_HHmmss}.sql";
                return File(bytes, "application/sql", fileName);
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Gagal mengekspor SQL Sinkronisasi: {ex.Message}";
                return RedirectToAction(nameof(Index));
            }
        }

        // ── EMAIL SMTP INTEGRATION ACTIONS ─────────────────────

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateEmailSettings(EmailConfigDto dto)
        {
            if (dto == null)
            {
                TempData["Error"] = "Data konfigurasi email tidak valid.";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                await _emailService.SaveEmailConfigAsync(dto);
                TempData["Success"] = "Pengaturan Server Email SMTP berhasil disimpan!";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Gagal menyimpan pengaturan email: {ex.Message}";
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> TestEmailConnection([FromForm] string? recipientEmail, [FromBody] TestEmailRequestDto? jsonDto = null)
        {
            var targetEmail = recipientEmail ?? jsonDto?.RecipientEmail;

            if (string.IsNullOrWhiteSpace(targetEmail))
            {
                var currentUser = await _userManager.GetUserAsync(User);
                targetEmail = currentUser?.Email;
            }

            if (string.IsNullOrWhiteSpace(targetEmail))
            {
                return Json(new { success = false, message = "Email tujuan uji coba (recipient) tidak boleh kosong." });
            }

            var result = await _emailService.TestConnectionAsync(targetEmail);
            return Json(new
            {
                success = result.IsSuccess,
                message = result.Message,
                latencyMs = result.LatencyMs,
                diagnostics = result.Diagnostics,
                recipient = targetEmail
            });
        }

        // ── EMAIL TEMPLATE MANAGEMENT ACTIONS ──────────────────

        [HttpGet]
        public async Task<IActionResult> GetEmailTemplate(int id)
        {
            var template = await _emailService.GetTemplateByIdAsync(id);
            if (template == null)
            {
                return Json(new { success = false, message = "Template email tidak ditemukan." });
            }

            return Json(new
            {
                success = true,
                data = new
                {
                    template.Id,
                    template.EventCode,
                    template.EventName,
                    template.Category,
                    template.Subject,
                    template.BodyHtml,
                    template.AvailableVariables,
                    template.IsActive
                }
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveEmailTemplate(CreateOrUpdateEmailTemplateDto dto)
        {
            if (!ModelState.IsValid)
            {
                var errorList = string.Join(", ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
                TempData["Error"] = $"Validasi template gagal: {errorList}";
                return RedirectToAction(nameof(Index));
            }

            var currentUser = await _userManager.GetUserAsync(User);
            var saved = await _emailService.SaveTemplateAsync(dto, currentUser?.Id);

            if (saved != null)
            {
                TempData["Success"] = $"Template email '{saved.EventName}' ({saved.EventCode}) berhasil disimpan!";
            }
            else
            {
                TempData["Error"] = "Gagal menyimpan template email.";
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteEmailTemplate(int id)
        {
            var deleted = await _emailService.DeleteTemplateAsync(id);
            if (deleted)
            {
                TempData["Success"] = "Template email berhasil dihapus.";
            }
            else
            {
                TempData["Error"] = "Template email tidak ditemukan atau gagal dihapus.";
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> PreviewEmailTemplate([FromForm] int id, [FromForm] string? sampleJson)
        {
            var template = await _emailService.GetTemplateByIdAsync(id);
            if (template == null)
            {
                return Json(new { success = false, message = "Template tidak ditemukan." });
            }

            Dictionary<string, string> sampleVars = new();
            if (!string.IsNullOrWhiteSpace(sampleJson))
            {
                try
                {
                    sampleVars = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(sampleJson) ?? new();
                }
                catch { }
            }

            var preview = _emailService.RenderTemplate(template, sampleVars);
            return Json(new
            {
                success = true,
                renderedSubject = preview.RenderedSubject,
                renderedHtml = preview.RenderedHtml,
                eventCode = template.EventCode,
                eventName = template.EventName
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetDefaultTemplates()
        {
            try
            {
                // Remove existing templates and seed defaults
                var existing = await _db.EmailTemplates.ToListAsync();
                _db.EmailTemplates.RemoveRange(existing);
                await _db.SaveChangesAsync();

                // Re-seed default 7 templates
                var defaults = new List<EmailTemplate>
                {
                    new EmailTemplate
                    {
                        EventCode = "USER_REGISTERED",
                        EventName = "Pendaftaran Akun Baru (User)",
                        Category = "Authentication",
                        Subject = "[{AppName}] Pendaftaran Berhasil - Menunggu Persetujuan Admin",
                        BodyHtml = @"<div style=""font-family:'Segoe UI',sans-serif;max-width:600px;margin:0 auto;background:#fff;border:1px solid #e2e8f0;border-radius:16px;overflow:hidden;box-shadow:0 4px 6px -1px rgba(0,0,0,0.1);""><div style=""background:linear-gradient(135deg,#4f46e5,#7c3aed);padding:32px 24px;text-align:center;color:#fff;""><h1 style=""margin:0;font-size:24px;font-weight:800;"">TrackerKerja</h1><p style=""margin:6px 0 0;font-size:13px;opacity:0.9;"">Sistem Manajemen Tugas & Kolaborasi Tim</p></div><div style=""padding:32px 24px;""><h2 style=""color:#1e293b;font-size:18px;margin-top:0;"">Halo, {FullName}! 👋</h2><p style=""color:#475569;font-size:14px;line-height:1.6;"">Terima kasih telah mendaftar di <strong>{AppName}</strong>. Akun Anda dengan email <strong style=""color:#4f46e5;"">{Email}</strong> berhasil dibuat.</p><div style=""background:#f8fafc;border-left:4px solid #f59e0b;padding:16px;border-radius:8px;margin:24px 0;""><p style=""margin:0;color:#92400e;font-size:13px;font-weight:600;"">⏳ Status Akun: Menunggu Persetujuan Administrator</p><p style=""margin:6px 0 0;color:#78350f;font-size:12px;"">Sistem kami menerapkan keamanan pendaftaran berbasis Approval. Anda akan menerima email notifikasi saat akun telah disetujui.</p></div></div><div style=""background:#f8fafc;padding:20px 24px;text-align:center;border-top:1px solid #e2e8f0;color:#94a3b8;font-size:12px;""><p style=""margin:0;"">&copy; {CurrentYear} {AppName}. All rights reserved.</p></div></div>",
                        AvailableVariables = "{FullName}, {Email}, {CompanyName}, {JobTitle}, {CurrentDate}",
                        IsActive = true,
                        CreatedAt = DateTime.Now,
                        UpdatedAt = DateTime.Now
                    },
                    new EmailTemplate
                    {
                        EventCode = "USER_APPROVED",
                        EventName = "Persetujuan Akun Pengguna (User Approved)",
                        Category = "Authentication",
                        Subject = "[{AppName}] Selamat! Akun Anda Telah Disetujui",
                        BodyHtml = @"<div style=""font-family:'Segoe UI',sans-serif;max-width:600px;margin:0 auto;background:#fff;border:1px solid #e2e8f0;border-radius:16px;overflow:hidden;box-shadow:0 4px 6px -1px rgba(0,0,0,0.1);""><div style=""background:linear-gradient(135deg,#059669,#10b981);padding:32px 24px;text-align:center;color:#fff;""><h1 style=""margin:0;font-size:24px;font-weight:800;"">Akun Telah Disetujui! 🎉</h1><p style=""margin:6px 0 0;font-size:13px;opacity:0.9;"">Selamat bergabung di {AppName}</p></div><div style=""padding:32px 24px;""><h2 style=""color:#1e293b;font-size:18px;margin-top:0;"">Halo, {FullName}!</h2><p style=""color:#475569;font-size:14px;line-height:1.6;"">Kabar baik! Administrator telah menyetujui akun Anda. Sekarang Anda dapat langsung masuk dan mulai mengelola tugas kerja Anda.</p><div style=""text-align:center;margin:32px 0;""><a href=""{LoginUrl}"" style=""background:linear-gradient(135deg,#059669,#10b981);color:#fff;text-decoration:none;padding:14px 32px;border-radius:12px;font-weight:700;font-size:14px;display:inline-block;"">Masuk ke TrackerKerja &rarr;</a></div></div><div style=""background:#f8fafc;padding:20px 24px;text-align:center;border-top:1px solid #e2e8f0;color:#94a3b8;font-size:12px;""><p style=""margin:0;"">&copy; {CurrentYear} {AppName}. All rights reserved.</p></div></div>",
                        AvailableVariables = "{FullName}, {Email}, {LoginUrl}, {CurrentDate}",
                        IsActive = true,
                        CreatedAt = DateTime.Now,
                        UpdatedAt = DateTime.Now
                    },
                    new EmailTemplate
                    {
                        EventCode = "USER_REJECTED",
                        EventName = "Penolakan Pendaftaran Akun",
                        Category = "Authentication",
                        Subject = "[{AppName}] Pemberitahuan Status Pendaftaran Akun",
                        BodyHtml = @"<div style=""font-family:'Segoe UI',sans-serif;max-width:600px;margin:0 auto;background:#fff;border:1px solid #e2e8f0;border-radius:16px;overflow:hidden;box-shadow:0 4px 6px -1px rgba(0,0,0,0.1);""><div style=""background:linear-gradient(135deg,#e11d48,#f43f5e);padding:32px 24px;text-align:center;color:#fff;""><h1 style=""margin:0;font-size:24px;font-weight:800;"">Status Pendaftaran Akun</h1></div><div style=""padding:32px 24px;""><h2 style=""color:#1e293b;font-size:18px;margin-top:0;"">Halo, {FullName}</h2><p style=""color:#475569;font-size:14px;line-height:1.6;"">Mohon maaf, permohonan pendaftaran akun Anda untuk email <strong>{Email}</strong> belum dapat disetujui saat ini.</p><div style=""background:#fff1f2;border-left:4px solid #e11d48;padding:16px;border-radius:8px;margin:24px 0;""><p style=""margin:0;color:#9f1239;font-size:13px;font-weight:600;"">Alasan: {RejectionReason}</p></div></div><div style=""background:#f8fafc;padding:20px 24px;text-align:center;border-top:1px solid #e2e8f0;color:#94a3b8;font-size:12px;""><p style=""margin:0;"">&copy; {CurrentYear} {AppName}. All rights reserved.</p></div></div>",
                        AvailableVariables = "{FullName}, {Email}, {RejectionReason}, {CurrentDate}",
                        IsActive = true,
                        CreatedAt = DateTime.Now,
                        UpdatedAt = DateTime.Now
                    },
                    new EmailTemplate
                    {
                        EventCode = "ADMIN_NEW_USER_ALERT",
                        EventName = "Peringatan Admin: User Baru Mendaftar",
                        Category = "Admin Alert",
                        Subject = "[{AppName} Admin] Pendaftaran Akun Baru: {FullName} ({CompanyName})",
                        BodyHtml = @"<div style=""font-family:'Segoe UI',sans-serif;max-width:600px;margin:0 auto;background:#fff;border:1px solid #e2e8f0;border-radius:16px;overflow:hidden;box-shadow:0 4px 6px -1px rgba(0,0,0,0.1);""><div style=""background:linear-gradient(135deg,#3b82f6,#1d4ed8);padding:32px 24px;text-align:center;color:#fff;""><h1 style=""margin:0;font-size:22px;font-weight:800;"">Pendaftaran Akun Baru 👤</h1><p style=""margin:6px 0 0;font-size:13px;opacity:0.9;"">Perlu tindakan persetujuan (approval) Administrator</p></div><div style=""padding:32px 24px;""><div style=""background:#f8fafc;border:1px solid #e2e8f0;border-radius:12px;padding:20px;margin-bottom:24px;""><table style=""width:100%;font-size:13px;color:#334155;""><tr><td style=""padding:6px 0;color:#64748b;width:120px;"">Nama Lengkap:</td><td style=""font-weight:700;"">{FullName}</td></tr><tr><td style=""padding:6px 0;color:#64748b;"">Email:</td><td style=""font-weight:700;"">{Email}</td></tr><tr><td style=""padding:6px 0;color:#64748b;"">Perusahaan/Tim:</td><td>{CompanyName}</td></tr><tr><td style=""padding:6px 0;color:#64748b;"">Jabatan:</td><td>{JobTitle}</td></tr><tr><td style=""padding:6px 0;color:#64748b;"">Waktu Daftar:</td><td>{CurrentDate}</td></tr></table></div><div style=""text-align:center;""><a href=""{ApprovalUrl}"" style=""background:#2563eb;color:#fff;text-decoration:none;padding:12px 28px;border-radius:10px;font-weight:700;font-size:13px;display:inline-block;"">Buka Panel Approval Member &rarr;</a></div></div><div style=""background:#f8fafc;padding:20px 24px;text-align:center;border-top:1px solid #e2e8f0;color:#94a3b8;font-size:12px;""><p style=""margin:0;"">&copy; {CurrentYear} {AppName}. All rights reserved.</p></div></div>",
                        AvailableVariables = "{FullName}, {Email}, {CompanyName}, {JobTitle}, {ApprovalUrl}, {CurrentDate}",
                        IsActive = true,
                        CreatedAt = DateTime.Now,
                        UpdatedAt = DateTime.Now
                    },
                    new EmailTemplate
                    {
                        EventCode = "TASK_ASSIGNED",
                        EventName = "Penugasan Tugas Baru (Task Assigned)",
                        Category = "Task Management",
                        Subject = "[{AppName}] Tugas Baru Diberikan: {TaskTitle}",
                        BodyHtml = @"<div style=""font-family:'Segoe UI',sans-serif;max-width:600px;margin:0 auto;background:#fff;border:1px solid #e2e8f0;border-radius:16px;overflow:hidden;box-shadow:0 4px 6px -1px rgba(0,0,0,0.1);""><div style=""background:linear-gradient(135deg,#4f46e5,#6366f1);padding:32px 24px;text-align:center;color:#fff;""><h1 style=""margin:0;font-size:22px;font-weight:800;"">Tugas Baru Ditugaskan 📋</h1></div><div style=""padding:32px 24px;""><h2 style=""color:#1e293b;font-size:16px;margin-top:0;"">Halo, {FullName}!</h2><p style=""color:#475569;font-size:14px;"">Anda telah ditugaskan untuk mengerjakan tugas berikut:</p><div style=""background:#f8fafc;border:1px solid #e2e8f0;border-radius:12px;padding:20px;margin:20px 0;""><h3 style=""margin:0 0 12px;color:#1e293b;font-size:16px;"">{TaskTitle}</h3><p style=""margin:0 0 12px;color:#64748b;font-size:13px;line-height:1.5;"">{TaskDescription}</p><table style=""width:100%;font-size:12px;color:#475569;""><tr><td style=""padding:4px 0;width:110px;color:#94a3b8;"">Proyek:</td><td style=""font-weight:600;"">{ProjectName}</td></tr><tr><td style=""padding:4px 0;color:#94a3b8;"">Prioritas:</td><td style=""font-weight:600;"">{Priority}</td></tr><tr><td style=""padding:4px 0;color:#94a3b8;"">Batas Waktu:</td><td style=""font-weight:600;"">{DueDate}</td></tr></table></div><div style=""text-align:center;""><a href=""{TaskUrl}"" style=""background:#4f46e5;color:#fff;text-decoration:none;padding:12px 28px;border-radius:10px;font-weight:700;font-size:13px;display:inline-block;"">Buka Detail Tugas &rarr;</a></div></div><div style=""background:#f8fafc;padding:20px 24px;text-align:center;border-top:1px solid #e2e8f0;color:#94a3b8;font-size:12px;""><p style=""margin:0;"">&copy; {CurrentYear} {AppName}. All rights reserved.</p></div></div>",
                        AvailableVariables = "{FullName}, {TaskTitle}, {TaskDescription}, {ProjectName}, {Priority}, {DueDate}, {TaskUrl}",
                        IsActive = true,
                        CreatedAt = DateTime.Now,
                        UpdatedAt = DateTime.Now
                    },
                    new EmailTemplate
                    {
                        EventCode = "TASK_STATUS_CHANGED",
                        EventName = "Perubahan Status Tugas (Status Updated)",
                        Category = "Task Management",
                        Subject = "[{AppName}] Status Tugas Diperbarui: {TaskTitle} -> {NewStatus}",
                        BodyHtml = @"<div style=""font-family:'Segoe UI',sans-serif;max-width:600px;margin:0 auto;background:#fff;border:1px solid #e2e8f0;border-radius:16px;overflow:hidden;box-shadow:0 4px 6px -1px rgba(0,0,0,0.1);""><div style=""background:linear-gradient(135deg,#0284c7,#0ea5e9);padding:32px 24px;text-align:center;color:#fff;""><h1 style=""margin:0;font-size:22px;font-weight:800;"">Status Tugas Berubah 🔄</h1></div><div style=""padding:32px 24px;""><h2 style=""color:#1e293b;font-size:16px;margin-top:0;"">Halo, {FullName}</h2><p style=""color:#475569;font-size:14px;"">Status tugas <strong>{TaskTitle}</strong> telah diperbarui.</p><div style=""background:#f8fafc;border:1px solid #e2e8f0;border-radius:12px;padding:20px;margin:20px 0;text-align:center;""><span style=""background:#e2e8f0;color:#475569;padding:6px 14px;border-radius:8px;font-size:12px;font-weight:700;"">{OldStatus}</span><span style=""margin:0 12px;color:#94a3b8;font-size:16px;"">&rarr;</span><span style=""background:#dbeafe;color:#1d4ed8;padding:6px 14px;border-radius:8px;font-size:12px;font-weight:700;"">{NewStatus}</span></div><div style=""text-align:center;""><a href=""{TaskUrl}"" style=""background:#0284c7;color:#fff;text-decoration:none;padding:12px 28px;border-radius:10px;font-weight:700;font-size:13px;display:inline-block;"">Lihat Tugas &rarr;</a></div></div><div style=""background:#f8fafc;padding:20px 24px;text-align:center;border-top:1px solid #e2e8f0;color:#94a3b8;font-size:12px;""><p style=""margin:0;"">&copy; {CurrentYear} {AppName}. All rights reserved.</p></div></div>",
                        AvailableVariables = "{FullName}, {TaskTitle}, {OldStatus}, {NewStatus}, {ProjectName}, {TaskUrl}",
                        IsActive = true,
                        CreatedAt = DateTime.Now,
                        UpdatedAt = DateTime.Now
                    },
                    new EmailTemplate
                    {
                        EventCode = "PASSWORD_RESET_NOTIFICATION",
                        EventName = "Reset Password Pengguna",
                        Category = "Authentication",
                        Subject = "[{AppName}] Pemberitahuan Reset Password Akun",
                        BodyHtml = @"<div style=""font-family:'Segoe UI',sans-serif;max-width:600px;margin:0 auto;background:#fff;border:1px solid #e2e8f0;border-radius:16px;overflow:hidden;box-shadow:0 4px 6px -1px rgba(0,0,0,0.1);""><div style=""background:linear-gradient(135deg,#d97706,#f59e0b);padding:32px 24px;text-align:center;color:#fff;""><h1 style=""margin:0;font-size:22px;font-weight:800;"">Reset Password Akun 🔑</h1></div><div style=""padding:32px 24px;""><h2 style=""color:#1e293b;font-size:16px;margin-top:0;"">Halo, {FullName}</h2><p style=""color:#475569;font-size:14px;line-height:1.6;"">Password akun <strong>{AppName}</strong> Anda telah direset oleh Administrator. Berikut adalah kredensial baru Anda:</p><div style=""background:#fef3c7;border:1px solid #fde68a;border-radius:12px;padding:20px;margin:24px 0;""><p style=""margin:0 0 6px;color:#92400e;font-size:12px;font-weight:600;"">Password Baru Sementara:</p><p style=""margin:0;font-family:monospace;font-size:18px;font-weight:700;color:#b45309;"">{NewPassword}</p></div><p style=""color:#64748b;font-size:12px;"">Demi keamanan, segera ubah password Anda setelah berhasil masuk.</p><div style=""text-align:center;margin-top:24px;""><a href=""{LoginUrl}"" style=""background:#d97706;color:#fff;text-decoration:none;padding:12px 28px;border-radius:10px;font-weight:700;font-size:13px;display:inline-block;"">Login Sekarang &rarr;</a></div></div><div style=""background:#f8fafc;padding:20px 24px;text-align:center;border-top:1px solid #e2e8f0;color:#94a3b8;font-size:12px;""><p style=""margin:0;"">&copy; {CurrentYear} {AppName}. All rights reserved.</p></div></div>",
                        AvailableVariables = "{FullName}, {Email}, {NewPassword}, {LoginUrl}, {CurrentDate}",
                        IsActive = true,
                        CreatedAt = DateTime.Now,
                        UpdatedAt = DateTime.Now
                    }
                };

                _db.EmailTemplates.AddRange(defaults);
                await _db.SaveChangesAsync();
                TempData["Success"] = "7 Template email default berhasil dipulihkan!";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Gagal memulihkan template default: {ex.Message}";
            }

            return RedirectToAction(nameof(Index));
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
