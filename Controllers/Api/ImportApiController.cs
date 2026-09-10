using ClosedXML.Excel;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TrackerKerja.Data;
using TrackerKerja.Models;
using TrackerKerja.Services;
using TrackerKerja.ViewModels;
using ModelTaskStatus = TrackerKerja.Models.TaskStatus;

namespace TrackerKerja.Controllers.Api
{
    /// <summary>
    /// Modul REST API Import dan Sinkronisasi Data Tugas Excel / ARMS (Multi-Sheet, URL Sync, Local Path Sync, Upsert Engine)
    /// </summary>
    [ApiController]
    [Route("api/import")]
    [Produces("application/json")]
    public class ImportApiController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly IExcelSyncService _excelSyncService;
        private readonly ILogger<ImportApiController> _logger;

        public ImportApiController(
            AppDbContext db,
            IExcelSyncService excelSyncService,
            ILogger<ImportApiController> logger)
        {
            _db = db;
            _excelSyncService = excelSyncService;
            _logger = logger;
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
                "Notes Tracker", "PIC"
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
                { "Integrasi TCES TICS", "TSD-001", "Checking & Testing Product, Scheming & premium class TICS vs Mass Product", "IN_PROGRESS", "HIGH", "ENHANCEMENT", "TCES", "Feature", 40, "2026-08-10", "2026-08-25", "", "haviz.indra@elistec.com;athallah.bariq@elistec.com", "syafix.said@elistec.com", "", "", "heni.rahayu@elistec.com", "nanda.putri@elistec.com", "Menunggu sinkronisasi", "Koordinasi lead", "Sprint 4 Target", "syafix.said@elistec.com" },
                { "Integrasi TCES TICS", "TSD-002", "Melakukan Deployment ke Server Staging & Smoke Testing", "DONE", "HIGH", "NEW_APP", "TCES", "Task", 100, "2026-08-10", "2026-08-18", "2026-08-18", "haviz.indra@elistec.com", "syafix.said@elistec.com", "mohammad.danang@elistec.com", "", "heni.rahayu@elistec.com", "", "", "", "Deployed successfully", "haviz.indra@elistec.com" }
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

            return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Template_Import_Task_22Kolom.xlsx");
        }

        /// <summary>
        /// Mengunggah file Excel untuk diparsing seluruh sheet dan dianalisis perubahannya (POST /api/import/preview)
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

            try
            {
                using var stream = new MemoryStream();
                await file.CopyToAsync(stream);
                stream.Position = 0;

                var result = await _excelSyncService.ParseFromStreamAsync(stream, file.FileName, "Upload");
                var dto = MapToResponseDto(result);

                return Ok(ApiResponse<ImportPreviewResponseDto>.Ok(dto, $"Berhasil memproses {result.TotalRows} baris dari {result.ProcessedSheets.Count} sheet."));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "API Preview error");
                return BadRequest(ApiResponse<object>.Fail($"Gagal memproses file: {ex.Message}"));
            }
        }

        /// <summary>
        /// Menganalisis dan mengimpor data langsung dari link / URL (Google Sheets / OneDrive / direct XLSX link) (POST /api/import/sync-url)
        /// </summary>
        [HttpPost("sync-url")]
        [ProducesResponseType(typeof(ApiResponse<ImportPreviewResponseDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> SyncUrl([FromBody] SyncUrlRequestDto req)
        {
            if (string.IsNullOrWhiteSpace(req?.Url))
            {
                return BadRequest(ApiResponse<object>.Fail("URL tautan Excel wajib diisi."));
            }

            try
            {
                var result = await _excelSyncService.ParseFromUrlAsync(req.Url);
                var dto = MapToResponseDto(result);
                return Ok(ApiResponse<ImportPreviewResponseDto>.Ok(dto, $"Berhasil membaca {result.TotalRows} baris dari link URL."));
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "API Sync URL error for {Url}", req.Url);
                return BadRequest(ApiResponse<object>.Fail($"Gagal membaca dari link: {ex.Message} Pastikan link dapat diakses publik atau gunakan upload file."));
            }
        }

        /// <summary>
        /// Menganalisis dan mensinkronisasikan file lokal yang ada pada server host (POST /api/import/sync-local)
        /// </summary>
        [HttpPost("sync-local")]
        [ProducesResponseType(typeof(ApiResponse<ImportPreviewResponseDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> SyncLocal([FromBody] SyncLocalRequestDto? req)
        {
            try
            {
                var localPath = req?.FilePath;
                var result = await _excelSyncService.ParseFromLocalPathAsync(localPath);
                var dto = MapToResponseDto(result);
                return Ok(ApiResponse<ImportPreviewResponseDto>.Ok(dto, $"Berhasil membaca {result.TotalRows} baris dari file server lokal: {result.FileName}"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "API Sync Local error");
                return BadRequest(ApiResponse<object>.Fail($"Gagal membaca file server lokal: {ex.Message}"));
            }
        }

        /// <summary>
        /// Mengeksekusi impor dan pembaruan data tugas (Case 1: Update existing, Case 2: Insert new) ke database (POST /api/import/execute)
        /// </summary>
        [HttpPost("execute")]
        [ProducesResponseType(typeof(ApiResponse<ExecuteImportResponseDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Execute([FromBody] ExecuteImportRequestDto dto)
        {
            if (!ModelState.IsValid || dto.Rows.Count == 0)
            {
                return BadRequest(ApiResponse<object>.Fail("Tidak ada baris tugas yang valid untuk diimpor."));
            }

            var model = new ImportResultViewModel
            {
                FileName = "API Execute Import",
                SourceType = "API",
                TotalRows = dto.Rows.Count,
                SuccessRows = dto.Rows.Count(r => r.IsValid),
                FailedRows = dto.Rows.Count(r => !r.IsValid),
                Rows = dto.Rows.Select(r => new ImportPreviewRow
                {
                    RowNumber = r.RowNumber,
                    SheetName = r.SheetName,
                    IsValid = r.IsValid,
                    Title = r.Title,
                    Category = r.Category,
                    Project = r.Project,
                    Assignee = r.Assignee,
                    AssigneeUserId = r.AssigneeUserId ?? dto.DefaultAssigneeId,
                    Priority = r.Priority,
                    Status = r.Status,
                    Progress = r.Progress,
                    StartDate = r.StartDate,
                    Deadline = r.Deadline,
                    EndDate = r.EndDate,
                    Milestone = r.Milestone,
                    Requirement = r.Requirement,
                    ModuleName = r.ModuleName,
                    BugType = r.BugType,
                    DeveloperEmails = r.DeveloperEmails,
                    BaEmails = r.BaEmails,
                    InfraEmails = r.InfraEmails,
                    MasterDataEmails = r.MasterDataEmails,
                    TesterEmails = r.TesterEmails,
                    TwEmails = r.TwEmails,
                    Obstacle = r.Obstacle,
                    Solution = r.Solution,
                    NotesTracker = r.NotesTracker,
                    Pic = r.Pic,
                    ActionType = r.ActionType,
                    ExistingTaskId = r.ExistingTaskId,
                    ChangedFields = r.ChangedFields
                }).ToList()
            };

            var syncResult = await _excelSyncService.ExecuteSyncAsync(model, null, "API Client");

            var response = new ExecuteImportResponseDto
            {
                ImportedCount = syncResult.InsertedCount,
                UpdatedCount = syncResult.UpdatedCount,
                UnchangedCount = syncResult.UnchangedCount,
                SkippedCount = syncResult.FailedCount,
                CreatedTaskIds = syncResult.AffectedTaskIds,
                Message = syncResult.Message,
                Errors = syncResult.Errors
            };

            if (!syncResult.Success)
            {
                return BadRequest(ApiResponse<ExecuteImportResponseDto>.Fail(syncResult.Message));
            }

            return Ok(ApiResponse<ExecuteImportResponseDto>.Ok(response, response.Message));
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

        private static ImportPreviewResponseDto MapToResponseDto(ImportResultViewModel result)
        {
            return new ImportPreviewResponseDto
            {
                FileName = result.FileName,
                SourceType = result.SourceType,
                SourceUrl = result.SourceUrl,
                TotalRows = result.TotalRows,
                SuccessRows = result.SuccessRows,
                FailedRows = result.FailedRows,
                InsertCount = result.InsertCount,
                UpdateCount = result.UpdateCount,
                UnchangedCount = result.UnchangedCount,
                ProcessedSheets = result.ProcessedSheets,
                Rows = result.Rows.Select(r => new ImportPreviewRowDto
                {
                    RowNumber = r.RowNumber,
                    SheetName = r.SheetName,
                    IsValid = r.IsValid,
                    Title = r.Title,
                    Category = r.Category,
                    Project = r.Project,
                    Assignee = r.Assignee,
                    AssigneeUserId = r.AssigneeUserId,
                    Priority = r.Priority,
                    Status = r.Status,
                    Progress = r.Progress,
                    StartDate = r.StartDate,
                    Deadline = r.Deadline,
                    EndDate = r.EndDate,
                    Milestone = r.Milestone,
                    Requirement = r.Requirement,
                    ModuleName = r.ModuleName,
                    BugType = r.BugType,
                    DeveloperEmails = r.DeveloperEmails,
                    BaEmails = r.BaEmails,
                    InfraEmails = r.InfraEmails,
                    MasterDataEmails = r.MasterDataEmails,
                    TesterEmails = r.TesterEmails,
                    TwEmails = r.TwEmails,
                    Obstacle = r.Obstacle,
                    Solution = r.Solution,
                    NotesTracker = r.NotesTracker,
                    Pic = r.Pic,
                    ErrorMessage = r.ErrorMessage,
                    WarningMessage = r.WarningMessage,
                    ActionType = r.ActionType,
                    ExistingTaskId = r.ExistingTaskId,
                    ChangedFields = r.ChangedFields
                }).ToList()
            };
        }
    }
}
