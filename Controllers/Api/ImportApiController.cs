using ClosedXML.Excel;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TrackerKerja.Data;
using TrackerKerja.Models;
using TrackerKerja.ViewModels;
using ModelTaskStatus = TrackerKerja.Models.TaskStatus;

namespace TrackerKerja.Controllers.Api
{
    /// <summary>
    /// Modul API Import dan Ekspor Data Tugas Excel / ARMS
    /// </summary>
    [ApiController]
    [Route("api/import")]
    [Produces("application/json")]
    public class ImportApiController : ControllerBase
    {
        private readonly AppDbContext _db;

        public ImportApiController(AppDbContext db)
        {
            _db = db;
        }

        /// <summary>
        /// Mengunduh file template Excel standar (.xlsx) untuk impor tugas (GET /api/import/template)
        /// </summary>
        [HttpGet("template")]
        [ProducesResponseType(typeof(FileResult), StatusCodes.Status200OK)]
        public IActionResult DownloadTemplate()
        {
            using var wb = new XLWorkbook();
            var ws = wb.Worksheets.Add("Sheet1");

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
                cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#4F46E5");
                cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
                cell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                cell.Style.Border.OutsideBorderColor = XLColor.FromHtml("#3730A3");
            }

            var samples = new object[,]
            {
                { "Integrasi TCES TICS", "TSD-001", "Checking & Testing Product, Scheming & premium class TICS vs Mass Product", "IN_PROGRESS", "HIGH", "ENHANCEMENT", "TCES", "Feature", 40, "2026-08-10", "2026-08-25", "", "haviz.indra@elistec.com", "syafix.said@elistec.com", "", "", "heni.rahayu@elistec.com", "", "Menunggu sinkronisasi", "Koordinasi lead", "Sprint 4 Target" },
                { "Integrasi TCES TICS", "TSD-002", "Melakukan Deployment ke Server Staging & Smoke Testing", "DONE", "HIGH", "NEW_APP", "TCES", "Task", 100, "2026-08-10", "2026-08-18", "2026-08-18", "haviz.indra@elistec.com", "syafix.said@elistec.com", "mohammad.danang@elistec.com", "", "heni.rahayu@elistec.com", "", "", "", "Deployed successfully" }
            };

            for (int r = 0; r < samples.GetLength(0); r++)
            {
                for (int c = 0; c < samples.GetLength(1); c++)
                {
                    ws.Cell(r + 2, c + 1).Value = samples[r, c]?.ToString() ?? "";
                }
            }

            ws.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            wb.SaveAs(stream);
            var content = stream.ToArray();

            return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Template_Import_Task_21Kolom.xlsx");
        }

        /// <summary>
        /// Mengunggah file Excel untuk diparsing dan divalidasi baris per baris sebelum diimpor (POST /api/import/preview)
        /// </summary>
        /// <param name="upload">Payload file spreadsheet (.xlsx / .xls)</param>
        [HttpPost("preview")]
        [Consumes("multipart/form-data")]
        [ProducesResponseType(typeof(ApiResponse<ImportPreviewResponseDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Preview([FromForm] FileUploadDto upload)
        {
            var file = upload?.File;
            if (file == null || file.Length == 0)
            {
                return BadRequest(ApiResponse<object>.Fail("Silakan unggah file spreadsheet Excel (.xlsx atau .xls)."));
            }

            var ext = Path.GetExtension(file.FileName).ToLower();
            if (ext != ".xlsx" && ext != ".xls")
            {
                return BadRequest(ApiResponse<object>.Fail("Format file tidak didukung. Hanya file .xlsx atau .xls yang diperbolehkan."));
            }

            var rows = new List<ImportPreviewRowDto>();
            var allUsers = await _db.Users.AsNoTracking().ToListAsync();

            using (var stream = file.OpenReadStream())
            using (var wb = new XLWorkbook(stream))
            {
                var ws = wb.Worksheets.FirstOrDefault();
                if (ws == null)
                {
                    return BadRequest(ApiResponse<object>.Fail("File Excel tidak memiliki worksheet yang valid."));
                }

                var rowCount = ws.LastRowUsed()?.RowNumber() ?? 0;
                var h1 = ws.Cell(1, 1).GetString()?.Trim().ToLower() ?? "";
                var h2 = ws.Cell(1, 2).GetString()?.Trim().ToLower() ?? "";
                var h13 = ws.Cell(1, 13).GetString()?.Trim().ToLower() ?? "";

                bool isProposed21 = h1.Contains("project_name") || h2.Contains("requirement_code") || h13.Contains("developer_emails") || (h1.Contains("project") && ws.Cell(1, 3).GetString().ToLower().Contains("title"));

                for (int r = 2; r <= rowCount; r++)
                {
                    var row = ws.Row(r);
                    string? title = null;
                    string? cat = null;
                    string? proj = null;
                    string? pic = null;
                    string? priority = null;
                    string? status = null;
                    string? start = null;
                    string? end = null;
                    string? deadline = null;
                    int progress = 0;
                    string? obstacle = null;
                    string? solution = null;
                    string? moduleName = null;
                    string? requirement = null;
                    string? notesTracker = null;

                    if (isProposed21)
                    {
                        proj = row.Cell(1).GetString()?.Trim();
                        requirement = row.Cell(2).GetString()?.Trim();
                        title = row.Cell(3).GetString()?.Trim();
                        status = row.Cell(4).GetString()?.Trim();
                        priority = row.Cell(5).GetString()?.Trim();
                        cat = row.Cell(6).GetString()?.Trim();
                        moduleName = row.Cell(7).GetString()?.Trim();
                        var rawProgress = row.Cell(9).GetString()?.Trim().Replace("%", "");
                        if (int.TryParse(rawProgress, out var pVal)) progress = Math.Clamp(pVal, 0, 100);
                        start = row.Cell(10).GetString()?.Trim();
                        deadline = row.Cell(11).GetString()?.Trim();
                        end = row.Cell(12).GetString()?.Trim();
                        pic = row.Cell(13).GetString()?.Trim();
                        obstacle = row.Cell(19).GetString()?.Trim();
                        solution = row.Cell(20).GetString()?.Trim();
                        notesTracker = row.Cell(21).GetString()?.Trim();
                    }
                    else
                    {
                        title = row.Cell(1).GetString()?.Trim();
                        cat = row.Cell(2).GetString()?.Trim();
                        proj = row.Cell(3).GetString()?.Trim();
                        pic = row.Cell(4).GetString()?.Trim();
                        priority = row.Cell(5).GetString()?.Trim();
                        status = row.Cell(6).GetString()?.Trim();
                        start = row.Cell(7).GetString()?.Trim();
                        end = row.Cell(8).GetString()?.Trim();
                        deadline = row.Cell(9).GetString()?.Trim();
                    }

                    if (string.IsNullOrWhiteSpace(title)) continue;

                    string? matchedUserId = null;
                    if (!string.IsNullOrWhiteSpace(pic))
                    {
                        var primaryEmail = pic.Split(new[] { ';', ',' }, StringSplitOptions.RemoveEmptyEntries).Select(e => e.Trim()).FirstOrDefault();
                        var u = allUsers.FirstOrDefault(u =>
                            (!string.IsNullOrEmpty(u.Email) && u.Email.Equals(primaryEmail, StringComparison.OrdinalIgnoreCase)) ||
                            (!string.IsNullOrEmpty(u.FullName) && u.FullName.Equals(primaryEmail, StringComparison.OrdinalIgnoreCase)));
                        if (u != null) matchedUserId = u.Id;
                    }

                    rows.Add(new ImportPreviewRowDto
                    {
                        RowNumber = r - 1,
                        IsValid = true,
                        Title = title,
                        Category = string.IsNullOrWhiteSpace(cat) ? "General" : cat,
                        Project = string.IsNullOrWhiteSpace(proj) ? null : proj,
                        Assignee = string.IsNullOrWhiteSpace(pic) ? null : pic,
                        AssigneeUserId = matchedUserId,
                        Priority = string.IsNullOrWhiteSpace(priority) ? "Medium" : priority,
                        Status = string.IsNullOrWhiteSpace(status) ? "Todo" : status,
                        Progress = progress,
                        StartDate = start,
                        Deadline = string.IsNullOrWhiteSpace(deadline) ? end : deadline,
                        EndDate = end,
                        Milestone = !string.IsNullOrWhiteSpace(moduleName) ? moduleName : requirement,
                        Requirement = requirement,
                        ModuleName = moduleName,
                        Obstacle = obstacle,
                        Solution = solution,
                        NotesTracker = notesTracker
                    });
                }
            }

            var response = new ImportPreviewResponseDto
            {
                FileName = file.FileName,
                TotalRows = rows.Count,
                SuccessRows = rows.Count(r => r.IsValid),
                FailedRows = rows.Count(r => !r.IsValid),
                Rows = rows
            };

            return Ok(ApiResponse<ImportPreviewResponseDto>.Ok(response, $"Berhasil memproses {rows.Count} baris data tugas."));
        }

        /// <summary>
        /// Mengeksekusi impor data baris tugas yang telah divalidasi ke database (POST /api/import/execute)
        /// </summary>
        /// <param name="dto">Payload daftar baris tugas yang akan diimpor</param>
        [HttpPost("execute")]
        [ProducesResponseType(typeof(ApiResponse<ExecuteImportResponseDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Execute([FromBody] ExecuteImportRequestDto dto)
        {
            if (!ModelState.IsValid || dto.Rows.Count == 0)
            {
                return BadRequest(ApiResponse<object>.Fail("Tidak ada baris tugas yang valid untuk diimpor."));
            }

            var createdIds = new List<int>();
            int imported = 0;

            foreach (var r in dto.Rows.Where(x => x.IsValid))
            {
                // Cek / Buat Project jika ada
                int? projId = dto.DefaultProjectId;
                if (!string.IsNullOrWhiteSpace(r.Project))
                {
                    var p = await _db.Projects.FirstOrDefaultAsync(p => p.Name.ToLower() == r.Project.Trim().ToLower());
                    if (p == null)
                    {
                        p = new Project { Name = r.Project.Trim(), Color = "#6366F1", CreatedAt = DateTime.Now };
                        _db.Projects.Add(p);
                        await _db.SaveChangesAsync();
                    }
                    projId = p.Id;
                }

                // Cek / Buat Category jika ada
                int? catId = null;
                if (!string.IsNullOrWhiteSpace(r.Category))
                {
                    var c = await _db.Categories.FirstOrDefaultAsync(c => c.Name.ToLower() == r.Category.Trim().ToLower());
                    if (c == null)
                    {
                        c = new Category { Name = r.Category.Trim(), Color = "#6366F1" };
                        _db.Categories.Add(c);
                        await _db.SaveChangesAsync();
                    }
                    catId = c.Id;
                }

                // Parse Prioritas & Status
                Enum.TryParse<TaskPriority>(r.Priority, true, out var priorityVal);
                Enum.TryParse<ModelTaskStatus>(r.Status, true, out var statusVal);

                DateTime? parsedStart = null;
                if (DateTime.TryParse(r.StartDate, out var dtStart)) parsedStart = dtStart;

                DateTime? parsedDue = null;
                if (DateTime.TryParse(r.Deadline, out var dtDue)) parsedDue = dtDue;

                var task = new WorkTask
                {
                    Title = r.Title.Trim(),
                    Description = !string.IsNullOrWhiteSpace(r.NotesTracker) ? r.NotesTracker.Trim() : null,
                    ProjectId = projId,
                    CategoryId = catId,
                    AssignedToUserId = !string.IsNullOrWhiteSpace(r.AssigneeUserId) ? r.AssigneeUserId : dto.DefaultAssigneeId,
                    Priority = priorityVal,
                    Status = statusVal,
                    Progress = statusVal == ModelTaskStatus.Done ? 100 : Math.Clamp(r.Progress, 0, 100),
                    StartDate = parsedStart,
                    DueDate = parsedDue,
                    Milestone = string.IsNullOrWhiteSpace(r.Milestone) ? "Implementation" : r.Milestone.Trim(),
                    Obstacle = r.Obstacle,
                    Solution = r.Solution,
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now
                };

                _db.Tasks.Add(task);
                await _db.SaveChangesAsync();

                createdIds.Add(task.Id);
                imported++;
            }

            var result = new ExecuteImportResponseDto
            {
                ImportedCount = imported,
                SkippedCount = dto.Rows.Count - imported,
                CreatedTaskIds = createdIds,
                Message = $"Berhasil mengimpor {imported} tugas ke database."
            };

            return Ok(ApiResponse<ExecuteImportResponseDto>.Ok(result, result.Message));
        }

        /// <summary>
        /// Mengekspor seluruh data tugas ke dalam format standar ARMS Excel (.xlsx) (GET /api/import/export-arms)
        /// </summary>
        [HttpGet("export-arms")]
        [ProducesResponseType(typeof(FileResult), StatusCodes.Status200OK)]
        public async Task<IActionResult> ExportArms()
        {
            var tasks = await _db.Tasks
                .Include(t => t.Project)
                .Include(t => t.Category)
                .Include(t => t.AssignedToUser)
                .Include(t => t.ParentTask)
                .Include(t => t.Sessions)
                .AsNoTracking()
                .OrderBy(t => t.Id)
                .ToListAsync();

            using var wb = new XLWorkbook();
            var ws = wb.Worksheets.Add("ARMS Tasks Export");

            var headers = new[]
            {
                "No", "Kode Tugas", "Judul Tugas", "Tugas Induk", "Milestone SDLC", "Kategori",
                "Proyek", "PIC / Penugasan", "Email PIC", "Prioritas", "Status", "Progress (%)",
                "Tgl Mulai", "Tgl Jatuh Tempo", "Total Jam Kerja", "Kendala", "Solusi"
            };

            for (int i = 0; i < headers.Length; i++)
            {
                var cell = ws.Cell(1, i + 1);
                cell.Value = headers[i];
                cell.Style.Font.Bold = true;
                cell.Style.Font.FontColor = XLColor.White;
                cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#4F46E5");
                cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            }

            for (int i = 0; i < tasks.Count; i++)
            {
                var t = tasks[i];
                var r = i + 2;

                ws.Cell(r, 1).Value = i + 1;
                ws.Cell(r, 2).Value = t.TaskCode;
                ws.Cell(r, 3).Value = t.Title;
                ws.Cell(r, 4).Value = t.ParentCode;
                ws.Cell(r, 5).Value = t.Milestone ?? "Implementation";
                ws.Cell(r, 6).Value = t.Category?.Name ?? "General";
                ws.Cell(r, 7).Value = t.Project?.Name ?? "Tanpa Proyek";
                ws.Cell(r, 8).Value = t.AssignedToUser?.FullName ?? "Belum Ditugaskan";
                ws.Cell(r, 9).Value = t.AssignedToUser?.Email ?? "-";
                ws.Cell(r, 10).Value = t.Priority.ToString();
                ws.Cell(r, 11).Value = t.Status.ToString();
                ws.Cell(r, 12).Value = t.Progress;
                ws.Cell(r, 13).Value = t.StartDate?.ToString("yyyy-MM-dd") ?? "-";
                ws.Cell(r, 14).Value = t.DueDate?.ToString("yyyy-MM-dd") ?? "-";
                ws.Cell(r, 15).Value = t.TotalDurationFormatted;
                ws.Cell(r, 16).Value = t.Obstacle ?? "-";
                ws.Cell(r, 17).Value = t.Solution ?? "-";
            }

            ws.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            wb.SaveAs(stream);
            var content = stream.ToArray();

            return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"ARMS_Tasks_Export_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx");
        }
    }
}
