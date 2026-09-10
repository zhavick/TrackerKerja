using ClosedXML.Excel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using TrackerKerja.Data;
using TrackerKerja.Models;
using TrackerKerja.Services;
using TrackerKerja.ViewModels;

namespace TrackerKerja.Controllers
{
    [Authorize]
    public class ImportController : Controller
    {
        private readonly AppDbContext _db;
        private readonly UserManager<AppUser> _userManager;
        private readonly IExcelSyncService _excelSyncService;
        private readonly ILogger<ImportController> _logger;

        public ImportController(
            AppDbContext db, 
            UserManager<AppUser> userManager,
            IExcelSyncService excelSyncService,
            ILogger<ImportController> logger)
        {
            _db = db;
            _userManager = userManager;
            _excelSyncService = excelSyncService;
            _logger = logger;
        }

        // ── INDEX: DASHBOARD IMPORT & SYNC ───────────────────────
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            ViewData["Title"] = "Import & Sinkronisasi Task dari Excel";
            
            var defaultLocalPath = _excelSyncService.GetDefaultLocalSyncPath();
            ViewBag.DefaultLocalPath = defaultLocalPath;
            ViewBag.LocalFileExists = System.IO.File.Exists(defaultLocalPath);

            var logs = await _db.ImportLogs
                .OrderByDescending(l => l.ImportedAt)
                .Take(15)
                .ToListAsync();

            return View(logs);
        }

        // ── METODE 1: IMPORT DARI LINK / URL ─────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ImportFromUrl(string? url)
        {
            if (string.IsNullOrWhiteSpace(url))
            {
                TempData["Error"] = "Masukkan tautan / link file Excel terlebih dahulu.";
                return RedirectToAction("Index");
            }

            try
            {
                var previewResult = await _excelSyncService.ParseFromUrlAsync(url);
                if (previewResult.TotalRows == 0)
                {
                    TempData["Error"] = "Tidak ditemukan data tugas yang dapat diimpor dari link tersebut. Silakan periksa isi file atau gunakan Metode 2 (Upload File Excel).";
                    return RedirectToAction("Index");
                }

                // Store in Session for confirmation
                HttpContext.Session.SetString("ImportPreview", JsonSerializer.Serialize(previewResult));
                TempData["ImportFileName"] = previewResult.FileName;

                var users = await _db.Users.OrderBy(u => u.FullName).ToListAsync();
                ViewBag.Users = users;

                return View("Preview", previewResult);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to import from URL {Url}", url);
                TempData["Error"] = $"Gagal mengimpor dari link: {ex.Message} " +
                                    "Jika link tidak dapat diakses secara publik, silakan gunakan METODE 2 (Upload File Excel Utuh langsung dari komputer Anda).";
                return RedirectToAction("Index");
            }
        }

        // ── METODE 2: UPLOAD SEMUA SHEET DARI FILE EXCEL UTUH ────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Upload(IFormFile? file, string? sheets)
        {
            if (file == null || file.Length == 0)
            {
                TempData["Error"] = "Pilih file Excel (.xlsx / .xls) terlebih dahulu.";
                return RedirectToAction("Index");
            }

            var ext = Path.GetExtension(file.FileName).ToLower();
            if (ext != ".xlsx" && ext != ".xls")
            {
                TempData["Error"] = "Format file tidak didukung. Gunakan file spreadsheet .xlsx atau .xls";
                return RedirectToAction("Index");
            }

            try
            {
                List<string>? specificSheets = null;
                if (!string.IsNullOrWhiteSpace(sheets))
                {
                    specificSheets = sheets.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
                                           .Select(s => s.Trim()).ToList();
                }

                using var stream = new MemoryStream();
                await file.CopyToAsync(stream);
                stream.Position = 0;

                var previewResult = await _excelSyncService.ParseFromStreamAsync(stream, file.FileName, "Upload", null, specificSheets);
                if (previewResult.TotalRows == 0)
                {
                    TempData["Error"] = "File Excel tidak memiliki baris data tugas valid. Pastikan header dan nama tugas sudah terisi.";
                    return RedirectToAction("Index");
                }

                // Store in Session for confirmation
                HttpContext.Session.SetString("ImportPreview", JsonSerializer.Serialize(previewResult));
                TempData["ImportFileName"] = previewResult.FileName;

                var users = await _db.Users.OrderBy(u => u.FullName).ToListAsync();
                ViewBag.Users = users;

                return View("Preview", previewResult);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading Excel file");
                TempData["Error"] = $"Gagal memproses file Excel: {ex.Message}";
                return RedirectToAction("Index");
            }
        }

        // ── METODE 3: SINKRONISASI FILE LOKAL SERVER ─────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SyncLocalFile(string? filePath)
        {
            try
            {
                var targetPath = !string.IsNullOrWhiteSpace(filePath) ? filePath.Trim() : _excelSyncService.GetDefaultLocalSyncPath();
                var previewResult = await _excelSyncService.ParseFromLocalPathAsync(targetPath);

                if (previewResult.TotalRows == 0)
                {
                    TempData["Error"] = $"File pada '{targetPath}' tidak memiliki baris tugas valid.";
                    return RedirectToAction("Index");
                }

                // Store in Session for confirmation
                HttpContext.Session.SetString("ImportPreview", JsonSerializer.Serialize(previewResult));
                TempData["ImportFileName"] = previewResult.FileName;

                var users = await _db.Users.OrderBy(u => u.FullName).ToListAsync();
                ViewBag.Users = users;

                return View("Preview", previewResult);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to sync from local path {Path}", filePath);
                TempData["Error"] = $"Gagal membaca file lokal: {ex.Message}";
                return RedirectToAction("Index");
            }
        }

        // ── QUICK ONE-CLICK SYNC FOR SERVER FILE ─────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> QuickSyncServerFile()
        {
            try
            {
                var targetPath = _excelSyncService.GetDefaultLocalSyncPath();
                var previewResult = await _excelSyncService.ParseFromLocalPathAsync(targetPath);
                var currentUser = await _userManager.GetUserAsync(User);
                var currentUserName = currentUser?.FullName ?? currentUser?.Email ?? "Quick Sync";

                var syncResult = await _excelSyncService.ExecuteSyncAsync(previewResult, null, currentUserName);

                if (syncResult.Success)
                {
                    TempData["Success"] = syncResult.Message;
                }
                else
                {
                    TempData["Error"] = syncResult.Message;
                }

                return RedirectToAction("Index", "Task");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing quick server sync");
                TempData["Error"] = $"Gagal melakukan sinkronisasi cepat: {ex.Message}";
                return RedirectToAction("Index");
            }
        }

        // ── CONFIRM & SAVE SYNC (CASE 1 & CASE 2) ────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Confirm()
        {
            var rowAssignees = new Dictionary<int, string>();
            if (Request.HasFormContentType)
            {
                foreach (var key in Request.Form.Keys)
                {
                    if (key.StartsWith("rowAssignees[", StringComparison.OrdinalIgnoreCase) && key.EndsWith("]"))
                    {
                        var inner = key.Substring(13, key.Length - 14);
                        if (int.TryParse(inner, out var rowNum))
                        {
                            rowAssignees[rowNum] = Request.Form[key].ToString();
                        }
                    }
                }
            }

            var json = HttpContext.Session.GetString("ImportPreview");
            if (string.IsNullOrEmpty(json))
            {
                TempData["Error"] = "Sesi preview import telah berakhir. Silakan lakukan upload atau sinkronisasi ulang.";
                return RedirectToAction("Index");
            }

            var importData = JsonSerializer.Deserialize<ImportResultViewModel>(json);
            if (importData == null) return RedirectToAction("Index");

            var currentUser = await _userManager.GetUserAsync(User);
            var currentUserName = currentUser?.FullName ?? currentUser?.Email ?? User.Identity?.Name ?? "User";

            var syncResult = await _excelSyncService.ExecuteSyncAsync(importData, rowAssignees, currentUserName);

            HttpContext.Session.Remove("ImportPreview");

            if (syncResult.Success)
            {
                TempData["Success"] = syncResult.Message;
            }
            else
            {
                TempData["Error"] = syncResult.Message;
            }

            return RedirectToAction("Index", "Task");
        }

        // ── DOWNLOAD TEMPLATE (22-COLUMN STANDARD FORMAT) ────────
        [HttpGet]
        public IActionResult Template()
        {
            using var wb = new XLWorkbook();
            var ws = wb.Worksheets.Add("Sheet1");

            var headers = new[]
            {
                "project_name", "requirement_code", "title", "status", "priority",
                "jenis_task", "module_name", "bug_type", "progress", "start_date",
                "due_date", "completed_date", "developer_emails", "ba_emails", "infra_emails",
                "master_data_emails", "tester_emails", "tw_emails", "kendala", "solusi",
                "Notes Tracker", "PIC"
            };

            for (int i = 0; i < headers.Length; i++)
            {
                var cell = ws.Cell(1, i + 1);
                cell.Value = headers[i];
                cell.Style.Font.Bold = true;
                cell.Style.Font.FontColor = XLColor.White;
                cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#4F46E5"); // Indigo
                cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
                cell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                cell.Style.Border.OutsideBorderColor = XLColor.FromHtml("#3730A3");
            }

            var samples = new object[,]
            {
                { "Integrasi TCES TICS", "TSD-001", "Checking & Testing Product, Scheming & premium class TICS vs Mass Product", "IN_PROGRESS", "HIGH", "ENHANCEMENT", "TCES", "Feature", 40, "2026-08-10", "2026-08-25", "", "haviz.indra@elistec.com;athallah.bariq@elistec.com", "syafix.said@elistec.com", "", "", "heni.rahayu@elistec.com", "nanda.putri@elistec.com", "Menunggu sinkronisasi skema data", "Koordinasi dengan lead backend", "Sprint 4 Target", "syafix.said@elistec.com" },
                { "Integrasi TCES TICS", "TSD-002", "Melakukan Deployment ke Server Staging & Smoke Testing", "DONE", "HIGH", "NEW_APP", "TCES", "Task", 100, "2026-08-10", "2026-08-18", "2026-08-18", "haviz.indra@elistec.com", "syafix.said@elistec.com", "mohammad.danang@elistec.com", "", "heni.rahayu@elistec.com", "", "", "", "Deployed successfully", "haviz.indra@elistec.com" },
                { "Pengembangan Mass Product Retail", "BRD-004", "Pembuatan Test Case untuk Pengujian End-to-End TFire dan Travela", "TODO", "MEDIUM", "NEW_APP", "Mass Product", "Feature", 0, "2026-08-20", "2026-08-30", "", "glenn.hakim@elistec.com", "syafix.said@elistec.com", "", "", "heni.rahayu@elistec.com", "", "Dokumen requirement belum final", "Follow up ke tim bisnis", "Priority QA item", "glenn.hakim@elistec.com" }
            };

            for (int r = 0; r < samples.GetLength(0); r++)
            {
                for (int c = 0; c < samples.GetLength(1); c++)
                {
                    var cell = ws.Cell(r + 2, c + 1);
                    cell.Value = samples[r, c]?.ToString() ?? "";
                    cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                    cell.Style.Border.OutsideBorderColor = XLColor.FromHtml("#E2E8F0");
                }
            }

            ws.Columns().AdjustToContents(10, 50);

            // Petunjuk Sheet
            var info = wb.Worksheets.Add("Petunjuk Pengisian");
            info.Cell(1, 1).Value = "PETUNJUK PENGISIAN TEMPLATE IMPORT TASK (22 KOLOM STANDAR & MULTI-SHEET)";
            info.Cell(1, 1).Style.Font.Bold = true;
            info.Cell(1, 1).Style.Font.FontSize = 14;

            var notes = new[]
            {
                "1. project_name: Nama proyek terkait (otomatis dibuat jika belum ada di sistem).",
                "2. requirement_code: Kode requirement / BRD / TSD (opsional).",
                "3. title: Judul nama task (WAJIB diisi).",
                "4. status: Status pengerjaan (TODO / IN_PROGRESS / DONE / TESTING).",
                "5. priority: Tingkat prioritas (LOW / MEDIUM / HIGH / CRITICAL). Default: MEDIUM.",
                "6. jenis_task: Kategori / tipe task (NEW_APP, ENHANCEMENT, BUGFIX, MAINTENANCE, dll).",
                "7. module_name: Nama modul / milestone (misal: TCES, TICS, Policy, Auth, dll).",
                "8. bug_type: Tipe isu / bug (Feature, Bug, Task, Enhancement).",
                "9. progress: Nilai persentase kemajuan (0 s/d 100). Status DONE otomatis 100%.",
                "10. start_date: Tanggal mulai pengerjaan (format YYYY-MM-DD atau format tanggal Excel).",
                "11. due_date: Batas tenggat waktu penyelesaian (deadline).",
                "12. completed_date: Tanggal selesai aktual (opsional).",
                "13. developer_emails: Email PIC Developer (dapat dipisah tanda ';' jika lebih dari 1 email).",
                "14. ba_emails: Email Business Analyst terkait.",
                "15. infra_emails: Email Tim DevOps / IT Infra terkait.",
                "16. master_data_emails: Email Tim Master Data terkait.",
                "17. tester_emails: Email Tim QA / Tester.",
                "18. tw_emails: Email Tim Technical Writer.",
                "19. kendala: Catatan hambatan / blocker selama pengerjaan.",
                "20. solusi: Solusi teknis atau tindak lanjut atas kendala.",
                "21. Notes Tracker: Catatan tambahan atau deskripsi aktivitas pengerjaan.",
                "22. PIC: Nama lengkap, username, atau email penanggung jawab (PIC) utama task. Jika kosong, sistem otomatis mencari dari sheet Master_Data atau nama sheet!"
            };

            for (int i = 0; i < notes.Length; i++)
            {
                info.Cell(i + 3, 1).Value = notes[i];
            }
            info.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            wb.SaveAs(stream);
            stream.Position = 0;
            return File(stream.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                "Template_Import_Task_22Kolom.xlsx");
        }

        // ── DOWNLOAD ARMS TEMPLATE ──────────────────────────────
        [HttpGet]
        public IActionResult ArmsTemplate()
        {
            return Template();
        }
    }
}
