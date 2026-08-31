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

        public ConfigurationController(
            AppDbContext db,
            IConfiguration config,
            IWebHostEnvironment env,
            UserManager<AppUser> userManager)
        {
            _db = db;
            _config = config;
            _env = env;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            ViewData["Title"] = "Konfigurasi Sistem & Database";

            // 1. Global Base URL
            var setting = await _db.SystemSettings.FirstOrDefaultAsync(s => s.Key == "GlobalBaseUrl");
            var baseUrl = setting?.Value ?? $"{Request.Scheme}://{Request.Host}";
            ViewBag.GlobalBaseUrl = baseUrl;
            ViewBag.BaseUrlUpdatedAt = setting?.UpdatedAt ?? DateTime.Now;

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
