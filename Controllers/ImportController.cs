using ClosedXML.Excel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using TrackerKerja.Data;
using TrackerKerja.Models;
using TrackerKerja.ViewModels;

namespace TrackerKerja.Controllers
{
    [Authorize]
    public class ImportController : Controller
    {
        private readonly AppDbContext _db;
        private readonly UserManager<AppUser> _userManager;

        public ImportController(AppDbContext db, UserManager<AppUser> userManager)
        {
            _db = db;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            ViewData["Title"] = "Import Task dari Excel";
            var logs = await _db.ImportLogs
                .OrderByDescending(l => l.ImportedAt)
                .Take(10)
                .ToListAsync();
            return View(logs);
        }

        // ── DOWNLOAD TEMPLATE (PROPOSED 21-COLUMN STANDARD FORMAT) ──
        [HttpGet]
        public IActionResult Template()
        {
            using var wb = new XLWorkbook();
            var ws = wb.Worksheets.Add("Sheet1");

            // Row 1: Exact 21 headers matching Proposed Import Format Tracker.xlsx
            var headers = new[]
            {
                "project_name", "requirement_code", "title", "status", "priority",
                "jenis_task", "module_name", "bug_type", "progress", "start_date",
                "due_date", "completed_date", "developer_emails", "ba_emails", "infra_emails",
                "master_data_emails", "tester_emails", "tw_emails", "kendala", "solusi",
                "Notes Tracker"
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

            // Sample rows matching user proposed standard
            var samples = new object[,]
            {
                { "Integrasi TCES TICS", "TSD-001", "Checking & Testing Product, Scheming & premium class TICS vs Mass Product", "IN_PROGRESS", "HIGH", "ENHANCEMENT", "TCES", "Feature", 40, "2026-08-10", "2026-08-25", "", "haviz.indra@elistec.com;athallah.bariq@elistec.com", "syafix.said@elistec.com", "", "", "heni.rahayu@elistec.com", "nanda.putri@elistec.com", "Menunggu sinkronisasi skema data", "Koordinasi dengan lead backend", "Sprint 4 Target" },
                { "Integrasi TCES TICS", "TSD-002", "Melakukan Deployment ke Server Staging & Smoke Testing", "DONE", "HIGH", "NEW_APP", "TCES", "Task", 100, "2026-08-10", "2026-08-18", "2026-08-18", "haviz.indra@elistec.com", "syafix.said@elistec.com", "mohammad.danang@elistec.com", "", "heni.rahayu@elistec.com", "", "", "", "Deployed successfully" },
                { "Pengembangan Mass Product Retail", "BRD-004", "Pembuatan Test Case untuk Pengujian End-to-End TFire dan Travela", "TODO", "MEDIUM", "NEW_APP", "Mass Product", "Feature", 0, "2026-08-20", "2026-08-30", "", "glenn.hakim@elistec.com", "syafix.said@elistec.com", "", "", "heni.rahayu@elistec.com", "", "Dokumen requirement belum final", "Follow up ke tim bisnis", "Priority QA item" }
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
            info.Cell(1, 1).Value = "PETUNJUK PENGISIAN TEMPLATE IMPORT TASK (21 KOLOM)";
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
                "21. Notes Tracker: Catatan tambahan atau deskripsi aktivitas pengerjaan."
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
                "Template_Import_Task_21Kolom.xlsx");
        }

        // ── DOWNLOAD ARMS TEMPLATE ──────────────────────────────
        [HttpGet]
        public IActionResult ArmsTemplate()
        {
            return Template();
        }

        // ── HELPER: EXTRACT DATE STRING FROM CLOSEDXML CELL ──────
        private string ExtractDateString(IXLCell cell)
        {
            if (cell.IsEmpty()) return string.Empty;

            try
            {
                // 1. Direct DateTime type in ClosedXML
                if (cell.DataType == XLDataType.DateTime || cell.Value.IsDateTime)
                {
                    if (cell.TryGetValue<DateTime>(out var dt))
                        return dt.ToString("yyyy-MM-dd");
                }

                // 2. Numeric OA Date (Excel stores dates as numbers e.g. 46253)
                if (cell.DataType == XLDataType.Number && cell.TryGetValue<double>(out var num))
                {
                    if (num >= 30000 && num <= 75000)
                    {
                        return DateTime.FromOADate(num).ToString("yyyy-MM-dd");
                    }
                }

                // 3. Formatted string or raw text
                var formatted = cell.GetFormattedString()?.Trim();
                if (!string.IsNullOrEmpty(formatted)) return formatted;

                var text = cell.GetString()?.Trim();
                if (!string.IsNullOrEmpty(text)) return text;

                return cell.Value.ToString()?.Trim() ?? string.Empty;
            }
            catch
            {
                return cell.GetString()?.Trim() ?? string.Empty;
            }
        }

        // ── HELPER: ROBUST MULTI-FORMAT DATE PARSER ───────────────
        public static DateTime? ParseDateRobust(string? val)
        {
            if (string.IsNullOrWhiteSpace(val)) return null;
            val = val.Trim();

            // 1. Check numeric OA Date
            if (double.TryParse(val, NumberStyles.Any, CultureInfo.InvariantCulture, out var oa) && oa >= 30000 && oa <= 75000)
            {
                try { return DateTime.FromOADate(oa); } catch { }
            }

            // 2. Standard explicit formats
            var formats = new[]
            {
                "yyyy-MM-dd", "yyyy/MM/dd", "yyyy.MM.dd",
                "dd/MM/yyyy", "dd-MM-yyyy", "dd.MM.yyyy",
                "d/M/yyyy", "d-M-yyyy", "d.M.yyyy",
                "MM/dd/yyyy", "M/d/yyyy",
                "dd/MM/yyyy HH:mm:ss", "yyyy-MM-dd HH:mm:ss",
                "dd/MM/yyyy HH:mm", "yyyy-MM-dd HH:mm",
                "dd-MM-yyyy HH:mm:ss", "dd-MM-yyyy HH:mm",
                "yyyy-MM-ddTHH:mm:ss", "yyyy-MM-ddTHH:mm:ssZ"
            };

            if (DateTime.TryParseExact(val, formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dtExact))
                return dtExact;

            // 3. Indonesian culture (e.g. 19 Agustus 2026, 19-Agu-2026)
            var idCulture = new CultureInfo("id-ID");
            if (DateTime.TryParse(val, idCulture, DateTimeStyles.None, out var dtId))
                return dtId;

            // 4. Invariant culture
            if (DateTime.TryParse(val, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dtInv))
                return dtInv;

            // 5. General Fallback
            if (DateTime.TryParse(val, out var dtGeneral))
                return dtGeneral;

            return null;
        }

        // ── UPLOAD & PREVIEW ────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Upload(IFormFile? file)
        {
            ViewData["Title"] = "Import Task dari Excel";

            if (file == null || file.Length == 0)
            {
                TempData["Error"] = "Pilih file Excel terlebih dahulu.";
                return RedirectToAction("Index");
            }

            var ext = Path.GetExtension(file.FileName).ToLower();
            if (ext != ".xlsx" && ext != ".xls")
            {
                TempData["Error"] = "Format file tidak didukung. Gunakan .xlsx atau .xls";
                return RedirectToAction("Index");
            }

            var result = new ImportResultViewModel
            {
                FileName = file.FileName
            };

            try
            {
                using var stream = new MemoryStream();
                await file.CopyToAsync(stream);
                stream.Position = 0;

                using var wb = new XLWorkbook(stream);
                var ws = wb.Worksheets.First();
                var rows = ws.RowsUsed().Skip(1).ToList(); // skip header

                var users = await _db.Users.ToListAsync();
                
                // Detect header structure
                var h1 = ws.Cell(1, 1).GetString().Trim().ToLower();
                var h2 = ws.Cell(1, 2).GetString().Trim().ToLower();
                var h3 = ws.Cell(1, 3).GetString().Trim().ToLower();
                var h4 = ws.Cell(1, 4).GetString().Trim().ToLower();
                var h13 = ws.Cell(1, 13).GetString().Trim().ToLower();

                // Format 1: Proposed 21-Column Format (project_name, requirement_code, title, status, priority, ...)
                bool isProposed21 = h1.Contains("project_name") || h2.Contains("requirement_code") || h13.Contains("developer_emails") || (h1.Contains("project") && h3.Contains("title"));
                // Format 2: ARMS 21-Column with Task Code at col 1
                bool isArms21WithTaskCode = !isProposed21 && (h1.Contains("task code") || h1.Contains("task_code") || (h2.Contains("project") && h4.Contains("title")));
                // Format 3: Legacy 9-Column Format
                bool hasPicColumn = !isProposed21 && !isArms21WithTaskCode && (h4.Contains("pic") || h4.Contains("penugasan") || h4.Contains("pengguna") || h4.Contains("assign") || ws.ColumnsUsed().Count() >= 9);

                if (rows.Count == 0)
                {
                    TempData["Error"] = "File Excel tidak memiliki baris data (hanya berisi header). Silakan isi data tugas mulai dari baris ke-2.";
                    return RedirectToAction("Index");
                }

                result.TotalRows = rows.Count;

                foreach (var row in rows)
                {
                    var rowNum = row.RowNumber();
                    var preview = new ImportPreviewRow { RowNumber = rowNum };

                    if (isProposed21)
                    {
                        // ── PROPOSED 21-COLUMN STANDARD FORMAT ──────────────────
                        preview.Project = row.Cell(1).GetString().Trim();
                        preview.Requirement = row.Cell(2).GetString().Trim();
                        preview.Title = row.Cell(3).GetString().Trim();

                        var rawStatus = row.Cell(4).GetString().Trim().ToUpper();
                        preview.Status = rawStatus switch
                        {
                            "DONE" or "SELESAI" => "Done",
                            "IN_PROGRESS" or "INPROGRESS" or "PROGRESS" => "InProgress",
                            "OVERDUE" => "Overdue",
                            _ => "Todo"
                        };

                        var rawPriority = row.Cell(5).GetString().Trim().ToUpper();
                        preview.Priority = rawPriority switch
                        {
                            "CRITICAL" => "Critical",
                            "HIGH" => "High",
                            "LOW" => "Low",
                            _ => "Medium"
                        };

                        var jenisTask = row.Cell(6).GetString().Trim();
                        var moduleName = row.Cell(7).GetString().Trim();
                        preview.Category = !string.IsNullOrEmpty(jenisTask) ? jenisTask : (!string.IsNullOrEmpty(moduleName) ? moduleName : null);
                        preview.ModuleName = moduleName;
                        preview.BugType = row.Cell(8).GetString().Trim();

                        var rawProgress = row.Cell(9).GetString().Trim().Replace("%", "");
                        if (int.TryParse(rawProgress, out var pVal)) preview.Progress = Math.Clamp(pVal, 0, 100);

                        preview.StartDate = ExtractDateString(row.Cell(10));
                        preview.Deadline = ExtractDateString(row.Cell(11));
                        preview.EndDate = ExtractDateString(row.Cell(12));

                        preview.DeveloperEmails = row.Cell(13).GetString().Trim();
                        preview.BaEmails = row.Cell(14).GetString().Trim();
                        preview.InfraEmails = row.Cell(15).GetString().Trim();
                        preview.MasterDataEmails = row.Cell(16).GetString().Trim();
                        preview.TesterEmails = row.Cell(17).GetString().Trim();
                        preview.TwEmails = row.Cell(18).GetString().Trim();

                        preview.Obstacle = row.Cell(19).GetString().Trim(); // kendala
                        preview.Solution = row.Cell(20).GetString().Trim(); // solusi
                        preview.NotesTracker = row.Cell(21).GetString().Trim(); // Notes Tracker

                        // Extract primary PIC from developer_emails (supports semicolon or comma separation)
                        if (!string.IsNullOrEmpty(preview.DeveloperEmails))
                        {
                            var firstDev = preview.DeveloperEmails.Split(new[] { ';', ',' }, StringSplitOptions.RemoveEmptyEntries).Select(e => e.Trim()).FirstOrDefault();
                            preview.Assignee = firstDev;
                        }
                    }
                    else if (isArms21WithTaskCode)
                    {
                        // ── ARMS 21-COLUMN FORMAT WITH TASK CODE ────────────────
                        preview.Project = row.Cell(2).GetString().Trim();
                        preview.Requirement = row.Cell(3).GetString().Trim();
                        preview.Title = row.Cell(4).GetString().Trim();

                        var rawStatus = row.Cell(5).GetString().Trim().ToUpper();
                        preview.Status = rawStatus switch
                        {
                            "DONE" or "SELESAI" => "Done",
                            "IN_PROGRESS" or "INPROGRESS" => "InProgress",
                            "OVERDUE" => "Overdue",
                            _ => "Todo"
                        };

                        var rawPriority = row.Cell(6).GetString().Trim().ToUpper();
                        preview.Priority = rawPriority switch
                        {
                            "CRITICAL" => "Critical",
                            "HIGH" => "High",
                            "LOW" => "Low",
                            _ => "Medium"
                        };

                        var jenisTask = row.Cell(7).GetString().Trim();
                        var moduleName = row.Cell(8).GetString().Trim();
                        preview.Category = !string.IsNullOrEmpty(jenisTask) ? jenisTask : (!string.IsNullOrEmpty(moduleName) ? moduleName : null);
                        preview.ModuleName = moduleName;
                        preview.BugType = row.Cell(9).GetString().Trim();

                        var rawProgress = row.Cell(10).GetString().Trim().Replace("%", "");
                        if (int.TryParse(rawProgress, out var pVal)) preview.Progress = Math.Clamp(pVal, 0, 100);

                        preview.StartDate = ExtractDateString(row.Cell(11));
                        preview.Deadline = ExtractDateString(row.Cell(12));
                        preview.EndDate = ExtractDateString(row.Cell(13));

                        preview.DeveloperEmails = row.Cell(14).GetString().Trim();
                        preview.BaEmails = row.Cell(15).GetString().Trim();
                        preview.InfraEmails = row.Cell(16).GetString().Trim();
                        preview.MasterDataEmails = row.Cell(17).GetString().Trim();
                        preview.TesterEmails = row.Cell(18).GetString().Trim();

                        preview.Obstacle = row.Cell(19).GetString().Trim(); // kendala
                        preview.Solution = row.Cell(20).GetString().Trim(); // solusi
                        preview.NotesTracker = row.Cell(21).GetString().Trim();

                        if (!string.IsNullOrEmpty(preview.DeveloperEmails))
                        {
                            var firstDev = preview.DeveloperEmails.Split(new[] { ';', ',' }, StringSplitOptions.RemoveEmptyEntries).Select(e => e.Trim()).FirstOrDefault();
                            preview.Assignee = firstDev;
                        }
                    }
                    else if (hasPicColumn)
                    {
                        // ── LEGACY 9-COLUMN FORMAT WITH PIC ─────────────────────
                        preview.Title = row.Cell(1).GetString().Trim();
                        preview.Category = row.Cell(2).GetString().Trim();
                        preview.Project = row.Cell(3).GetString().Trim();
                        preview.Assignee = row.Cell(4).GetString().Trim();
                        preview.Priority = row.Cell(5).GetString().Trim();
                        preview.Status = row.Cell(6).GetString().Trim();
                        preview.StartDate = ExtractDateString(row.Cell(7));
                        preview.EndDate = ExtractDateString(row.Cell(8));
                        preview.Deadline = ExtractDateString(row.Cell(9));
                    }
                    else
                    {
                        // ── LEGACY 8-COLUMN FORMAT WITHOUT PIC ──────────────────
                        preview.Title = row.Cell(1).GetString().Trim();
                        preview.Category = row.Cell(2).GetString().Trim();
                        preview.Project = row.Cell(3).GetString().Trim();
                        preview.Priority = row.Cell(4).GetString().Trim();
                        preview.Status = row.Cell(5).GetString().Trim();
                        preview.StartDate = ExtractDateString(row.Cell(6));
                        preview.EndDate = ExtractDateString(row.Cell(7));
                        preview.Deadline = ExtractDateString(row.Cell(8));
                    }

                    // Validation
                    if (string.IsNullOrWhiteSpace(preview.Title))
                    {
                        preview.IsValid = false;
                        preview.ErrorMessage = "Nama Task tidak boleh kosong.";
                        result.FailedRows++;
                    }
                    else
                    {
                        // Normalize values
                        if (string.IsNullOrWhiteSpace(preview.Priority)) preview.Priority = "Medium";
                        if (string.IsNullOrWhiteSpace(preview.Status)) preview.Status = "Todo";

                        // Done status automatically sets progress to 100%
                        if (string.Equals(preview.Status, "Done", StringComparison.OrdinalIgnoreCase) ||
                            string.Equals(preview.Status, "Selesai", StringComparison.OrdinalIgnoreCase))
                        {
                            preview.Status = "Done";
                            preview.Progress = 100;
                        }
                        else
                        {
                            preview.Progress = 0;
                        }

                        var warnings = new List<string>();

                        if (!string.IsNullOrEmpty(preview.Priority) && !Enum.TryParse<TaskPriority>(preview.Priority, out _))
                        {
                            preview.Priority = "Medium";
                            warnings.Add("Prioritas tidak valid → diset ke Medium");
                        }
                        if (!string.IsNullOrEmpty(preview.Status) && !Enum.TryParse<Models.TaskStatus>(preview.Status, out _))
                        {
                            preview.Status = "Todo";
                            warnings.Add("Status tidak valid → diset ke Todo");
                        }

                        // Validate Assignee if provided
                        if (!string.IsNullOrEmpty(preview.Assignee))
                        {
                            var matched = users.FirstOrDefault(u =>
                                (!string.IsNullOrEmpty(u.Email) && u.Email.Equals(preview.Assignee, StringComparison.OrdinalIgnoreCase)) ||
                                (!string.IsNullOrEmpty(u.FullName) && u.FullName.Equals(preview.Assignee, StringComparison.OrdinalIgnoreCase)) ||
                                (!string.IsNullOrEmpty(u.UserName) && u.UserName.Equals(preview.Assignee, StringComparison.OrdinalIgnoreCase)));
                            if (matched != null)
                            {
                                preview.AssigneeUserId = matched.Id;
                                preview.Assignee = matched.FullName ?? matched.Email;
                            }
                            else
                            {
                                warnings.Add($"PIC '{preview.Assignee}' tidak terdaftar di sistem");
                            }
                        }

                        // Validate parsed dates for preview feedback
                        var parsedStart = ParseDateRobust(preview.StartDate);
                        var parsedEnd = ParseDateRobust(preview.EndDate);
                        var parsedDeadline = ParseDateRobust(preview.Deadline);

                        if (!string.IsNullOrEmpty(preview.StartDate) && !parsedStart.HasValue)
                            warnings.Add($"Format tanggal mulai '{preview.StartDate}' tidak dikenali");

                        if (!string.IsNullOrEmpty(preview.Deadline) && !parsedDeadline.HasValue)
                            warnings.Add($"Format deadline '{preview.Deadline}' tidak dikenali");

                        if (warnings.Any())
                            preview.WarningMessage = string.Join("; ", warnings);

                        result.SuccessRows++;
                    }

                    result.Rows.Add(preview);
                }

                ViewBag.Users = users.OrderBy(u => u.FullName).ToList();
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Gagal membaca file: {ex.Message}";
                return RedirectToAction("Index");
            }

            // Store preview in Session for confirm step
            TempData["ImportFileName"] = result.FileName;
            HttpContext.Session.SetString("ImportPreview", System.Text.Json.JsonSerializer.Serialize(result));

            return View("Preview", result);
        }

        // ── CONFIRM & SAVE ────────────────────────────────────
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
                TempData["Error"] = "Session import habis. Upload ulang file Anda.";
                return RedirectToAction("Index");
            }

            var importData = System.Text.Json.JsonSerializer.Deserialize<ImportResultViewModel>(json);
            if (importData == null) return RedirectToAction("Index");

            var user = await _userManager.GetUserAsync(User);
            var validRows = importData.Rows.Where(r => r.IsValid).ToList();
            int saved = 0;
            var errors = new List<string>();

            foreach (var row in validRows)
            {
                try
                {
                    // Resolve or create Project
                    Project? project = null;
                    if (!string.IsNullOrWhiteSpace(row.Project))
                    {
                        project = await _db.Projects
                            .FirstOrDefaultAsync(p => p.Name.ToLower() == row.Project.ToLower());

                        if (project == null)
                        {
                            project = new Project
                            {
                                Name = row.Project,
                                Color = "#6366F1",
                                Status = ProjectStatus.Active,
                                CreatedAt = DateTime.Now
                            };
                            _db.Projects.Add(project);
                            await _db.SaveChangesAsync();
                        }
                    }

                    // Resolve or create Category
                    Category? category = null;
                    if (!string.IsNullOrWhiteSpace(row.Category))
                    {
                        category = await _db.Categories
                            .FirstOrDefaultAsync(c => c.Name.ToLower() == row.Category.ToLower());

                        if (category == null)
                        {
                            category = new Category
                            {
                                Name = row.Category,
                                Color = "#94A3B8"
                            };
                            _db.Categories.Add(category);
                            await _db.SaveChangesAsync();
                        }
                    }

                    // Resolve Assignee / PIC (supports interactive override from preview dropdown)
                    string? assignedToUserId = null;
                    if (rowAssignees != null && rowAssignees.TryGetValue(row.RowNumber, out var overrideValue) && !string.IsNullOrWhiteSpace(overrideValue))
                    {
                        if (overrideValue != "none")
                        {
                            var targetUser = await _db.Users.FirstOrDefaultAsync(u =>
                                u.Id == overrideValue ||
                                (u.Email != null && u.Email.ToLower() == overrideValue.ToLower()) ||
                                (u.FullName != null && u.FullName.ToLower() == overrideValue.ToLower()));
                            assignedToUserId = targetUser?.Id;
                        }
                    }
                    else if (!string.IsNullOrWhiteSpace(row.AssigneeUserId))
                    {
                        assignedToUserId = row.AssigneeUserId;
                    }
                    else if (!string.IsNullOrWhiteSpace(row.Assignee))
                    {
                        var targetUser = await _db.Users.FirstOrDefaultAsync(u =>
                            (u.Email != null && u.Email.ToLower() == row.Assignee.ToLower()) ||
                            (u.FullName != null && u.FullName.ToLower() == row.Assignee.ToLower()) ||
                            (u.UserName != null && u.UserName.ToLower() == row.Assignee.ToLower()));
                        assignedToUserId = targetUser?.Id;
                    }

                    // Robust Date Parsing
                    var parsedStart = ParseDateRobust(row.StartDate);
                    var parsedEnd = ParseDateRobust(row.EndDate);
                    var parsedDeadline = ParseDateRobust(row.Deadline);

                    Enum.TryParse<TaskPriority>(row.Priority, out var priority);
                    Enum.TryParse<Models.TaskStatus>(row.Status, out var status);

                    // Logic: jika status adalah Done atau progress >= 100, maka progress dibuat 100%
                    int progress = (status == Models.TaskStatus.Done || row.Progress >= 100) ? 100 : row.Progress;
                    if (progress >= 100)
                    {
                        status = Models.TaskStatus.Done;
                        progress = 100;
                    }

                    // Resolve ParentTask if requirement specified
                    int? parentTaskId = null;
                    if (!string.IsNullOrWhiteSpace(row.Requirement))
                    {
                        var reqClean = row.Requirement.Trim();
                        var parentMatch = await _db.Tasks.FirstOrDefaultAsync(t => 
                            t.Title.ToLower() == reqClean.ToLower() ||
                            (!string.IsNullOrEmpty(t.Description) && t.Description.ToLower().Contains(reqClean.ToLower())) ||
                            reqClean.Contains($"TASK-{t.Id:D4}"));
                        parentTaskId = parentMatch?.Id;
                    }

                    // Build tags from roles / metadata if present
                    var tagList = new List<string>();
                    if (!string.IsNullOrWhiteSpace(row.BugType)) tagList.Add(row.BugType.Trim());
                    if (!string.IsNullOrWhiteSpace(row.BaEmails)) tagList.Add($"BA:{row.BaEmails.Trim()}");
                    if (!string.IsNullOrWhiteSpace(row.TesterEmails)) tagList.Add($"QA:{row.TesterEmails.Trim()}");
                    if (!string.IsNullOrWhiteSpace(row.InfraEmails)) tagList.Add($"Infra:{row.InfraEmails.Trim()}");
                    if (!string.IsNullOrWhiteSpace(row.MasterDataEmails)) tagList.Add($"MD:{row.MasterDataEmails.Trim()}");
                    if (!string.IsNullOrWhiteSpace(row.TwEmails)) tagList.Add($"TW:{row.TwEmails.Trim()}");

                    var milestone = !string.IsNullOrWhiteSpace(row.ModuleName) ? row.ModuleName.Trim() : (!string.IsNullOrWhiteSpace(row.Requirement) ? row.Requirement.Trim() : "Implementation");
                    var description = !string.IsNullOrWhiteSpace(row.NotesTracker) ? row.NotesTracker.Trim() : null;

                    var task = new WorkTask
                    {
                        Title = row.Title,
                        Description = description,
                        ProjectId = project?.Id,
                        CategoryId = category?.Id,
                        AssignedToUserId = assignedToUserId,
                        ParentTaskId = parentTaskId,
                        Priority = priority,
                        Status = status,
                        Progress = progress,
                        StartDate = parsedStart,
                        DueDate = parsedDeadline ?? parsedEnd,
                        Obstacle = row.Obstacle,
                        Solution = row.Solution,
                        Milestone = milestone,
                        Tags = tagList.Any() ? System.Text.Json.JsonSerializer.Serialize(tagList) : null,
                        CreatedAt = DateTime.Now,
                        UpdatedAt = DateTime.Now
                    };

                    _db.Tasks.Add(task);
                    saved++;
                }
                catch (Exception ex)
                {
                    errors.Add($"Baris {row.RowNumber}: {ex.Message}");
                }
            }

            await _db.SaveChangesAsync();

            // Log the import
            _db.ImportLogs.Add(new ImportLog
            {
                FileName = importData.FileName,
                TotalRows = importData.TotalRows,
                SuccessRows = saved,
                FailedRows = importData.FailedRows + errors.Count,
                Errors = errors.Any() ? string.Join("\n", errors) : null,
                ImportedAt = DateTime.Now,
                ImportedBy = user?.FullName ?? user?.Email
            });
            await _db.SaveChangesAsync();

            HttpContext.Session.Remove("ImportPreview");
            TempData["Success"] = $"Import berhasil! {saved} task berhasil disimpan dari {importData.TotalRows} baris.";
            return RedirectToAction("Index", "Task");
        }
    }
}
