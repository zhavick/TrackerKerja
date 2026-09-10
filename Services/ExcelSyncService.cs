using ClosedXML.Excel;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Text.RegularExpressions;
using TrackerKerja.Data;
using TrackerKerja.Models;
using TrackerKerja.ViewModels;
using ModelTaskStatus = TrackerKerja.Models.TaskStatus;

namespace TrackerKerja.Services
{
    public class ExcelSyncService : IExcelSyncService
    {
        private readonly AppDbContext _db;
        private readonly UserManager<AppUser> _userManager;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<ExcelSyncService> _logger;

        private const string DefaultLocalPath = @"C:\Users\WAHANA 24\Downloads\Task Tracker - Update (1).xlsx";

        public ExcelSyncService(
            AppDbContext db,
            UserManager<AppUser> userManager,
            IHttpClientFactory httpClientFactory,
            ILogger<ExcelSyncService> logger)
        {
            _db = db;
            _userManager = userManager;
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        public string GetDefaultLocalSyncPath()
        {
            if (File.Exists(DefaultLocalPath))
            {
                return DefaultLocalPath;
            }

            // Check alternative locations in user profile or current directory
            var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            var altPath = Path.Combine(userProfile, "Downloads", "Task Tracker - Update (1).xlsx");
            if (File.Exists(altPath)) return altPath;

            return DefaultLocalPath;
        }

        public async Task<ImportResultViewModel> ParseFromUrlAsync(string url)
        {
            if (string.IsNullOrWhiteSpace(url))
            {
                throw new ArgumentException("URL tautan file Excel tidak boleh kosong.");
            }

            url = url.Trim();
            string downloadUrl = NormalizeExcelDownloadUrl(url);

            try
            {
                var client = _httpClientFactory.CreateClient();
                client.Timeout = TimeSpan.FromSeconds(45);
                client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) TrackerKerja/3.1");

                var response = await client.GetAsync(downloadUrl);
                if (!response.IsSuccessStatusCode)
                {
                    throw new HttpRequestException($"Gagal mengunduh file dari server (HTTP {(int)response.StatusCode} {response.ReasonPhrase}).");
                }

                var contentBytes = await response.Content.ReadAsByteArrayAsync();
                if (contentBytes == null || contentBytes.Length < 100)
                {
                    throw new InvalidOperationException("File yang diunduh dari link kosong atau tidak valid.");
                }

                // Verify if content is HTML (often happens when Google Drive / OneDrive link requires login)
                var headerText = System.Text.Encoding.ASCII.GetString(contentBytes.Take(100).ToArray());
                if (headerText.Contains("<!DOCTYPE html", StringComparison.OrdinalIgnoreCase) || 
                    headerText.Contains("<html", StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException(
                        "Tautan mengarah ke halaman login atau pratinjau HTML, bukan file Excel langsung. " +
                        "Pastikan hak akses file diatur ke 'Siapa saja yang memiliki tautan' (Anyone with link) atau gunakan Metode 2 (Upload File Excel Utuh).");
                }

                using var memStream = new MemoryStream(contentBytes);
                var fileName = ExtractFileNameFromUrl(url);
                return await ParseFromStreamAsync(memStream, fileName, "Link", url);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error importing Excel from URL {Url}", url);
                throw new InvalidOperationException(
                    $"Gagal mengimpor dari link: {ex.Message} " +
                    "Silakan gunakan Metode 2 (Upload File Excel Utuh) jika link dibatasi izin akses.", ex);
            }
        }

        public async Task<ImportResultViewModel> ParseFromLocalPathAsync(string? localPath = null)
        {
            var targetPath = !string.IsNullOrWhiteSpace(localPath) ? localPath.Trim() : GetDefaultLocalSyncPath();
            if (!File.Exists(targetPath))
            {
                throw new FileNotFoundException($"File Excel tidak ditemukan pada lokasi: {targetPath}");
            }

            var fileName = Path.GetFileName(targetPath);
            await using var fileStream = new FileStream(targetPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using var memStream = new MemoryStream();
            await fileStream.CopyToAsync(memStream);
            memStream.Position = 0;

            return await ParseFromStreamAsync(memStream, fileName, "LocalFile", targetPath);
        }

        public async Task<ImportResultViewModel> ParseFromStreamAsync(
            Stream stream, 
            string fileName, 
            string sourceType = "Upload", 
            string? sourceUrl = null, 
            List<string>? specificSheets = null)
        {
            var result = new ImportResultViewModel
            {
                FileName = fileName,
                SourceType = sourceType,
                SourceUrl = sourceUrl
            };

            var allUsers = await _db.Users.AsNoTracking().ToListAsync();
            var existingTasks = await _db.Tasks
                .Include(t => t.Project)
                .Include(t => t.Category)
                .Include(t => t.AssignedToUser)
                .AsNoTracking()
                .ToListAsync();

            using var wb = new XLWorkbook(stream);

            // Step 1: Pre-scan Master_Data sheet for lookup if available
            var masterDataLookup = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var masterDataByProjectTitle = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            var masterSheet = wb.Worksheets.FirstOrDefault(w => 
                w.Name.Equals("Master_Data", StringComparison.OrdinalIgnoreCase) || 
                w.Name.Equals("MasterData", StringComparison.OrdinalIgnoreCase) ||
                w.Name.Equals("Master Data", StringComparison.OrdinalIgnoreCase));

            if (masterSheet != null)
            {
                var mRows = masterSheet.RowsUsed().Skip(1);
                foreach (var mr in mRows)
                {
                    // Person, Project, Task Title, Status, Start Date, Duration, Due Date, Gantt Label
                    var person = mr.Cell(1).GetString().Trim();
                    var proj = mr.Cell(2).GetString().Trim();
                    var taskTitle = mr.Cell(3).GetString().Trim();

                    if (!string.IsNullOrEmpty(person) && !string.IsNullOrEmpty(taskTitle) &&
                        !person.Equals("#REF!", StringComparison.OrdinalIgnoreCase) &&
                        !person.Equals("#N/A", StringComparison.OrdinalIgnoreCase))
                    {
                        var keyBoth = $"{proj.ToLower()}|{taskTitle.ToLower()}";
                        masterDataByProjectTitle[keyBoth] = person;
                        if (!masterDataLookup.ContainsKey(taskTitle.ToLower()))
                        {
                            masterDataLookup[taskTitle.ToLower()] = person;
                        }
                    }
                }
            }

            // Step 2: Determine sheets to process
            var sheetsToProcess = new List<IXLWorksheet>();
            var ignoredSheetNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "Lookup", "RealTime_Dashboard", "RealTime Dashboard", "Dashboard",
                "Petunjuk Pengisian", "Petunjuk", "ARMS Tasks Export", "Summary"
            };

            foreach (var ws in wb.Worksheets)
            {
                if (specificSheets != null && specificSheets.Any())
                {
                    if (specificSheets.Contains(ws.Name, StringComparer.OrdinalIgnoreCase))
                    {
                        sheetsToProcess.Add(ws);
                    }
                }
                else
                {
                    // If no specific sheets requested, process all task sheets (skip utility / summary sheets)
                    if (!ignoredSheetNames.Contains(ws.Name) && !ws.Name.Equals("Master_Data", StringComparison.OrdinalIgnoreCase))
                    {
                        sheetsToProcess.Add(ws);
                    }
                }
            }

            // Fallback: If all sheets were ignored (e.g. file only has 1 sheet named Sheet1 or Master_Data), include the first sheet
            if (!sheetsToProcess.Any() && wb.Worksheets.Any())
            {
                sheetsToProcess.Add(wb.Worksheets.First());
            }

            int globalRowNumber = 0;

            foreach (var ws in sheetsToProcess)
            {
                result.ProcessedSheets.Add(ws.Name);

                // Extract Sheet Person candidate from sheet name: e.g. "Integrasi TCES TICS (Syafix)" -> "Syafix", "Glenn" -> "Glenn"
                string? sheetPersonCandidate = ExtractPersonFromSheetName(ws.Name);

                var rowsUsed = ws.RowsUsed().ToList();
                if (rowsUsed.Count < 2) continue; // Only header or empty

                var headerRow = rowsUsed.First();
                var dataRows = rowsUsed.Skip(1).ToList();

                // Build header column lookup
                var colMap = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                var lastCol = ws.LastColumnUsed()?.ColumnNumber() ?? headerRow.LastCellUsed()?.Address.ColumnNumber ?? 0;
                for (int c = 1; c <= lastCol; c++)
                {
                    var hText = ws.Cell(headerRow.RowNumber(), c).GetString().Trim();
                    if (!string.IsNullOrEmpty(hText) && !colMap.ContainsKey(hText))
                    {
                        colMap[hText] = c;
                    }
                }

                // Detect format
                var h1 = ws.Cell(headerRow.RowNumber(), 1).GetString().Trim().ToLower();
                var h2 = ws.Cell(headerRow.RowNumber(), 2).GetString().Trim().ToLower();
                var h3 = ws.Cell(headerRow.RowNumber(), 3).GetString().Trim().ToLower();
                var h4 = ws.Cell(headerRow.RowNumber(), 4).GetString().Trim().ToLower();
                var h13 = ws.Cell(headerRow.RowNumber(), 13).GetString().Trim().ToLower();

                bool isProposed21 = colMap.ContainsKey("project_name") || colMap.ContainsKey("developer_emails") ||
                                    h1.Contains("project_name") || h2.Contains("requirement_code") || h13.Contains("developer_emails") ||
                                    (h1.Contains("project") && h3.Contains("title")) || (colMap.ContainsKey("title") && colMap.ContainsKey("status"));

                bool isArms21WithTaskCode = !isProposed21 && (h1.Contains("task code") || h1.Contains("task_code") || (h2.Contains("project") && h4.Contains("title")));
                bool hasPicColumn = !isProposed21 && !isArms21WithTaskCode && (h4.Contains("pic") || h4.Contains("penugasan") || ws.ColumnsUsed().Count() >= 9);

                foreach (var row in dataRows)
                {
                    // Check if entire row is empty
                    if (row.CellsUsed().All(c => string.IsNullOrWhiteSpace(c.GetString())))
                    {
                        continue;
                    }

                    globalRowNumber++;
                    var preview = new ImportPreviewRow
                    {
                        RowNumber = globalRowNumber,
                        SheetName = ws.Name
                    };

                    string rawPic = string.Empty;

                    if (isProposed21)
                    {
                        preview.Project = GetCellByColName(row, colMap, "project_name", 1);
                        preview.Requirement = GetCellByColName(row, colMap, "requirement_code", 2);
                        preview.Title = GetCellByColName(row, colMap, "title", 3);

                        // If title is empty in column 3, try column 4 or check other columns
                        if (string.IsNullOrWhiteSpace(preview.Title))
                        {
                            preview.Title = GetCellByColName(row, colMap, "Task Title", 4);
                        }

                        var rawStatus = GetCellByColName(row, colMap, "status", 4).ToUpper();
                        preview.Status = NormalizeStatusString(rawStatus);

                        var rawPriority = GetCellByColName(row, colMap, "priority", 5).ToUpper();
                        preview.Priority = NormalizePriorityString(rawPriority);

                        var jenisTask = GetCellByColName(row, colMap, "jenis_task", 6);
                        var moduleName = GetCellByColName(row, colMap, "module_name", 7);
                        preview.Category = !string.IsNullOrEmpty(jenisTask) ? jenisTask : (!string.IsNullOrEmpty(moduleName) ? moduleName : "General");
                        preview.ModuleName = moduleName;
                        preview.BugType = GetCellByColName(row, colMap, "bug_type", 8);

                        var rawProgress = GetCellByColName(row, colMap, "progress", 9).Replace("%", "");
                        if (double.TryParse(rawProgress, NumberStyles.Any, CultureInfo.InvariantCulture, out var pVal))
                        {
                            // If formatted as decimal e.g. 0.5 -> 50%
                            if (pVal > 0 && pVal <= 1) pVal *= 100;
                            preview.Progress = Math.Clamp((int)Math.Round(pVal), 0, 100);
                        }

                        preview.StartDate = ExtractDateString(GetCellRef(row, colMap, "start_date", 10));
                        preview.Deadline = ExtractDateString(GetCellRef(row, colMap, "due_date", 11));
                        preview.EndDate = ExtractDateString(GetCellRef(row, colMap, "completed_date", 12));

                        preview.DeveloperEmails = GetCellByColName(row, colMap, "developer_emails", 13);
                        preview.BaEmails = GetCellByColName(row, colMap, "ba_emails", 14);
                        preview.InfraEmails = GetCellByColName(row, colMap, "infra_emails", 15);
                        preview.MasterDataEmails = GetCellByColName(row, colMap, "master_data_emails", 16);
                        preview.TesterEmails = GetCellByColName(row, colMap, "tester_emails", 17);
                        preview.TwEmails = GetCellByColName(row, colMap, "tw_emails", 18);

                        preview.Obstacle = GetCellByColName(row, colMap, "kendala", 19);
                        preview.Solution = GetCellByColName(row, colMap, "solusi", 20);
                        preview.NotesTracker = GetCellByColName(row, colMap, "Notes Tracker", 21);

                        // Extract username or full_name if available in later columns
                        var rowUsername = GetCellByColName(row, colMap, "username", 0);
                        var rowFullName = GetCellByColName(row, colMap, "full_name", 0);
                        var explicitPic = GetCellByColName(row, colMap, "PIC", 22);

                        rawPic = !string.IsNullOrEmpty(explicitPic) ? explicitPic :
                                 !string.IsNullOrEmpty(rowUsername) ? rowUsername :
                                 !string.IsNullOrEmpty(rowFullName) ? rowFullName :
                                 preview.DeveloperEmails;
                    }
                    else if (isArms21WithTaskCode)
                    {
                        preview.Project = row.Cell(2).GetString().Trim();
                        preview.Requirement = row.Cell(3).GetString().Trim();
                        preview.Title = row.Cell(4).GetString().Trim();

                        preview.Status = NormalizeStatusString(row.Cell(5).GetString().Trim());
                        preview.Priority = NormalizePriorityString(row.Cell(6).GetString().Trim());

                        var jenisTask = row.Cell(7).GetString().Trim();
                        var moduleName = row.Cell(8).GetString().Trim();
                        preview.Category = !string.IsNullOrEmpty(jenisTask) ? jenisTask : (!string.IsNullOrEmpty(moduleName) ? moduleName : "General");
                        preview.ModuleName = moduleName;
                        preview.BugType = row.Cell(9).GetString().Trim();

                        var rawProgress = row.Cell(10).GetString().Trim().Replace("%", "");
                        if (double.TryParse(rawProgress, NumberStyles.Any, CultureInfo.InvariantCulture, out var pVal))
                        {
                            if (pVal > 0 && pVal <= 1) pVal *= 100;
                            preview.Progress = Math.Clamp((int)Math.Round(pVal), 0, 100);
                        }

                        preview.StartDate = ExtractDateString(row.Cell(11));
                        preview.Deadline = ExtractDateString(row.Cell(12));
                        preview.EndDate = ExtractDateString(row.Cell(13));

                        preview.DeveloperEmails = row.Cell(14).GetString().Trim();
                        preview.BaEmails = row.Cell(15).GetString().Trim();
                        preview.InfraEmails = row.Cell(16).GetString().Trim();
                        preview.MasterDataEmails = row.Cell(17).GetString().Trim();
                        preview.TesterEmails = row.Cell(18).GetString().Trim();

                        preview.Obstacle = row.Cell(19).GetString().Trim();
                        preview.Solution = row.Cell(20).GetString().Trim();
                        preview.NotesTracker = row.Cell(21).GetString().Trim();

                        rawPic = GetCellByColName(row, colMap, "PIC", 0);
                        if (string.IsNullOrEmpty(rawPic)) rawPic = preview.DeveloperEmails;
                    }
                    else if (hasPicColumn)
                    {
                        preview.Title = row.Cell(1).GetString().Trim();
                        preview.Category = row.Cell(2).GetString().Trim();
                        preview.Project = row.Cell(3).GetString().Trim();
                        rawPic = row.Cell(4).GetString().Trim();
                        preview.Priority = NormalizePriorityString(row.Cell(5).GetString().Trim());
                        preview.Status = NormalizeStatusString(row.Cell(6).GetString().Trim());
                        preview.StartDate = ExtractDateString(row.Cell(7));
                        preview.EndDate = ExtractDateString(row.Cell(8));
                        preview.Deadline = ExtractDateString(row.Cell(9));
                    }
                    else
                    {
                        preview.Title = row.Cell(1).GetString().Trim();
                        preview.Category = row.Cell(2).GetString().Trim();
                        preview.Project = row.Cell(3).GetString().Trim();
                        preview.Priority = NormalizePriorityString(row.Cell(4).GetString().Trim());
                        preview.Status = NormalizeStatusString(row.Cell(5).GetString().Trim());
                        preview.StartDate = ExtractDateString(row.Cell(6));
                        preview.EndDate = ExtractDateString(row.Cell(7));
                        preview.Deadline = ExtractDateString(row.Cell(8));
                    }

                    // Validation of Title
                    if (string.IsNullOrWhiteSpace(preview.Title))
                    {
                        // Check if row has any actual task information or is just an empty template row
                        bool hasAnyContent = !string.IsNullOrWhiteSpace(preview.Project) || 
                                             !string.IsNullOrWhiteSpace(preview.Requirement) || 
                                             !string.IsNullOrWhiteSpace(preview.Obstacle) || 
                                             !string.IsNullOrWhiteSpace(preview.Solution) || 
                                             !string.IsNullOrWhiteSpace(preview.NotesTracker);

                        if (!hasAnyContent)
                        {
                            // Blank formula row in template -> skip cleanly
                            continue;
                        }

                        preview.IsValid = false;
                        preview.ErrorMessage = "Nama Task tidak boleh kosong.";
                        preview.ActionType = "ERROR";
                        result.FailedRows++;
                        result.Rows.Add(preview);
                        continue;
                    }

                    // Done status automatically sets progress to 100%
                    if (preview.Status.Equals("Done", StringComparison.OrdinalIgnoreCase) || preview.Progress >= 100)
                    {
                        preview.Status = "Done";
                        preview.Progress = 100;
                    }

                    var warnings = new List<string>();

                    // ── SMART PIC RESOLUTION STRATEGY ──────────────────────────────────
                    // 1. Check direct rawPic from row cell
                    // 2. Check Sheet Name candidate (e.g. "Integrasi TCES TICS (Syafix)" -> "Syafix")
                    // 3. Check Master_Data sheet lookup by (Project, Title) or (Title)
                    string? candidatePic = rawPic?.Trim();

                    // If cell PIC is empty, try sheet person candidate
                    if (string.IsNullOrEmpty(candidatePic) && !string.IsNullOrEmpty(sheetPersonCandidate))
                    {
                        candidatePic = sheetPersonCandidate;
                    }

                    // If still empty or needs verification, fallback to Master_Data sheet lookup
                    if (string.IsNullOrEmpty(candidatePic) || candidatePic.Equals("haviz.indra@elistec.com", StringComparison.OrdinalIgnoreCase))
                    {
                        // In some files, developer_emails was filled with a default email while Master_Data has the true owner
                        var keyBoth = $"{preview.Project?.Trim().ToLower()}|{preview.Title.Trim().ToLower()}";
                        if (masterDataByProjectTitle.TryGetValue(keyBoth, out var mdPerson))
                        {
                            candidatePic = mdPerson;
                        }
                        else if (masterDataLookup.TryGetValue(preview.Title.Trim().ToLower(), out var mdTitlePerson))
                        {
                            candidatePic = mdTitlePerson;
                        }
                    }

                    // Resolve candidate string to database AppUser
                    AppUser? matchedUser = ResolveAppUser(candidatePic, allUsers);
                    if (matchedUser == null && !string.IsNullOrEmpty(sheetPersonCandidate))
                    {
                        // Fallback to sheet person if still unmatched
                        matchedUser = ResolveAppUser(sheetPersonCandidate, allUsers);
                    }

                    if (matchedUser != null)
                    {
                        preview.AssigneeUserId = matchedUser.Id;
                        preview.Assignee = matchedUser.FullName ?? matchedUser.Email;
                        preview.Pic = matchedUser.FullName ?? candidatePic;
                    }
                    else
                    {
                        preview.Pic = candidatePic;
                        preview.Assignee = candidatePic;
                        if (!string.IsNullOrEmpty(candidatePic))
                        {
                            warnings.Add($"PIC '{candidatePic}' belum terdaftar di sistem (dapat disesuaikan pada pratinjau)");
                        }
                    }

                    // Validate dates
                    var parsedStart = ParseDateRobust(preview.StartDate);
                    var parsedEnd = ParseDateRobust(preview.EndDate);
                    var parsedDeadline = ParseDateRobust(preview.Deadline);

                    if (!string.IsNullOrEmpty(preview.StartDate) && !parsedStart.HasValue)
                        warnings.Add($"Format tanggal mulai '{preview.StartDate}' tidak dikenali");

                    if (!string.IsNullOrEmpty(preview.Deadline) && !parsedDeadline.HasValue)
                        warnings.Add($"Format deadline '{preview.Deadline}' tidak dikenali");

                    if (warnings.Any())
                    {
                        preview.WarningMessage = string.Join("; ", warnings);
                    }

                    // ── DIFF ENGINE: CASE 1 (UPDATE) vs CASE 2 (INSERT) ─────────────
                    var cleanTitle = preview.Title.Trim();
                    var cleanProject = preview.Project?.Trim() ?? string.Empty;

                    var existingTask = existingTasks.FirstOrDefault(t =>
                        t.Title.Trim().Equals(cleanTitle, StringComparison.OrdinalIgnoreCase) &&
                        (string.IsNullOrEmpty(cleanProject) || t.Project == null || t.Project.Name.Trim().Equals(cleanProject, StringComparison.OrdinalIgnoreCase)));

                    if (existingTask == null && !string.IsNullOrEmpty(preview.Requirement))
                    {
                        // Check match by requirement or task code if applicable
                        var reqCode = preview.Requirement.Trim();
                        existingTask = existingTasks.FirstOrDefault(t =>
                            (!string.IsNullOrEmpty(t.Description) && t.Description.Contains(reqCode, StringComparison.OrdinalIgnoreCase)) ||
                            t.Title.Trim().Equals(cleanTitle, StringComparison.OrdinalIgnoreCase));
                    }

                    if (existingTask != null)
                    {
                        preview.ExistingTaskId = existingTask.Id;
                        var changes = new List<string>();

                        // Compare Status
                        Enum.TryParse<ModelTaskStatus>(preview.Status, true, out var incomingStatus);
                        if (existingTask.Status != incomingStatus)
                        {
                            changes.Add($"Status: {existingTask.Status} → {incomingStatus}");
                        }

                        // Compare Priority
                        Enum.TryParse<TaskPriority>(preview.Priority, true, out var incomingPriority);
                        if (existingTask.Priority != incomingPriority)
                        {
                            changes.Add($"Prioritas: {existingTask.Priority} → {incomingPriority}");
                        }

                        // Compare Progress
                        if (existingTask.Progress != preview.Progress)
                        {
                            changes.Add($"Progress: {existingTask.Progress}% → {preview.Progress}%");
                        }

                        // Compare Start Date
                        if (parsedStart.HasValue && (!existingTask.StartDate.HasValue || existingTask.StartDate.Value.Date != parsedStart.Value.Date))
                        {
                            changes.Add($"Tgl Mulai: {existingTask.StartDate:yyyy-MM-dd} → {parsedStart:yyyy-MM-dd}");
                        }

                        // Compare Due Date / Deadline
                        var targetDue = parsedDeadline ?? parsedEnd;
                        if (targetDue.HasValue && (!existingTask.DueDate.HasValue || existingTask.DueDate.Value.Date != targetDue.Value.Date))
                        {
                            changes.Add($"Deadline: {existingTask.DueDate:yyyy-MM-dd} → {targetDue:yyyy-MM-dd}");
                        }

                        // Compare Assignee
                        if (!string.IsNullOrEmpty(preview.AssigneeUserId) && existingTask.AssignedToUserId != preview.AssigneeUserId)
                        {
                            var oldUserName = existingTask.AssignedToUser?.FullName ?? "Belum Ditugaskan";
                            var newUserName = preview.Assignee ?? "Baru";
                            changes.Add($"PIC: {oldUserName} → {newUserName}");
                        }

                        // Compare Obstacle
                        if (!string.IsNullOrEmpty(preview.Obstacle) && !string.Equals(existingTask.Obstacle, preview.Obstacle, StringComparison.OrdinalIgnoreCase))
                        {
                            changes.Add("Kendala diperbarui");
                        }

                        // Compare Solution
                        if (!string.IsNullOrEmpty(preview.Solution) && !string.Equals(existingTask.Solution, preview.Solution, StringComparison.OrdinalIgnoreCase))
                        {
                            changes.Add("Solusi diperbarui");
                        }

                        if (changes.Any())
                        {
                            preview.ActionType = "UPDATE";
                            preview.ChangedFields = changes;
                            result.UpdateCount++;
                        }
                        else
                        {
                            preview.ActionType = "UNCHANGED";
                            result.UnchangedCount++;
                        }
                    }
                    else
                    {
                        preview.ActionType = "INSERT";
                        result.InsertCount++;
                    }

                    result.SuccessRows++;
                    result.Rows.Add(preview);
                }
            }

            result.TotalRows = result.Rows.Count;
            return result;
        }

        public async Task<SyncExecutionResult> ExecuteSyncAsync(
            ImportResultViewModel model, 
            Dictionary<int, string>? rowPicOverrides = null, 
            string? currentUserName = null)
        {
            var result = new SyncExecutionResult();
            var validRows = model.Rows.Where(r => r.IsValid).ToList();

            if (!validRows.Any())
            {
                result.Success = false;
                result.Message = "Tidak ada baris tugas valid yang dapat disinkronkan.";
                return result;
            }

            var users = await _db.Users.ToListAsync();
            var projects = await _db.Projects.ToListAsync();
            var categories = await _db.Categories.ToListAsync();

            using var transaction = await _db.Database.BeginTransactionAsync();

            try
            {
                foreach (var row in validRows)
                {
                    try
                    {
                        // 1. Resolve or create Project
                        Project? project = null;
                        if (!string.IsNullOrWhiteSpace(row.Project))
                        {
                            var pName = row.Project.Trim();
                            project = projects.FirstOrDefault(p => p.Name.Equals(pName, StringComparison.OrdinalIgnoreCase));
                            if (project == null)
                            {
                                project = new Project
                                {
                                    Name = pName,
                                    Color = "#6366F1",
                                    Status = ProjectStatus.Active,
                                    CreatedAt = DateTime.Now
                                };
                                _db.Projects.Add(project);
                                await _db.SaveChangesAsync();
                                projects.Add(project);
                            }
                        }

                        // 2. Resolve or create Category
                        Category? category = null;
                        if (!string.IsNullOrWhiteSpace(row.Category))
                        {
                            var cName = row.Category.Trim();
                            category = categories.FirstOrDefault(c => c.Name.Equals(cName, StringComparison.OrdinalIgnoreCase));
                            if (category == null)
                            {
                                category = new Category
                                {
                                    Name = cName,
                                    Color = "#94A3B8"
                                };
                                _db.Categories.Add(category);
                                await _db.SaveChangesAsync();
                                categories.Add(category);
                            }
                        }

                        // 3. Resolve PIC / Assignee (with dropdown override support)
                        string? assignedUserId = null;
                        if (rowPicOverrides != null && rowPicOverrides.TryGetValue(row.RowNumber, out var overrideVal) && !string.IsNullOrWhiteSpace(overrideVal))
                        {
                            if (overrideVal != "none")
                            {
                                var u = users.FirstOrDefault(x => x.Id == overrideVal ||
                                    (x.Email != null && x.Email.Equals(overrideVal, StringComparison.OrdinalIgnoreCase)) ||
                                    (x.FullName != null && x.FullName.Equals(overrideVal, StringComparison.OrdinalIgnoreCase)));
                                assignedUserId = u?.Id;
                            }
                        }
                        else if (!string.IsNullOrWhiteSpace(row.AssigneeUserId))
                        {
                            assignedUserId = row.AssigneeUserId;
                        }
                        else if (!string.IsNullOrWhiteSpace(row.Assignee) || !string.IsNullOrWhiteSpace(row.Pic))
                        {
                            var targetPic = !string.IsNullOrWhiteSpace(row.Assignee) ? row.Assignee : row.Pic!;
                            var u = ResolveAppUser(targetPic, users);
                            assignedUserId = u?.Id;
                        }

                        // 4. Parse Dates & Status
                        var parsedStart = ParseDateRobust(row.StartDate);
                        var parsedEnd = ParseDateRobust(row.EndDate);
                        var parsedDeadline = ParseDateRobust(row.Deadline);

                        Enum.TryParse<TaskPriority>(row.Priority, true, out var priority);
                        Enum.TryParse<ModelTaskStatus>(row.Status, true, out var status);

                        int progress = (status == ModelTaskStatus.Done || row.Progress >= 100) ? 100 : Math.Clamp(row.Progress, 0, 100);
                        if (progress >= 100)
                        {
                            status = ModelTaskStatus.Done;
                            progress = 100;
                        }

                        var milestone = !string.IsNullOrWhiteSpace(row.ModuleName) ? row.ModuleName.Trim() :
                                        (!string.IsNullOrWhiteSpace(row.Requirement) ? row.Requirement.Trim() : "Implementation");

                        var description = !string.IsNullOrWhiteSpace(row.NotesTracker) ? row.NotesTracker.Trim() : null;

                        // Build tags
                        var tagList = new List<string>();
                        if (!string.IsNullOrWhiteSpace(row.BugType)) tagList.Add(row.BugType.Trim());
                        if (!string.IsNullOrWhiteSpace(row.SheetName)) tagList.Add($"Sheet:{row.SheetName.Trim()}");
                        if (!string.IsNullOrWhiteSpace(row.BaEmails)) tagList.Add($"BA:{row.BaEmails.Trim()}");
                        if (!string.IsNullOrWhiteSpace(row.TesterEmails)) tagList.Add($"QA:{row.TesterEmails.Trim()}");
                        if (!string.IsNullOrWhiteSpace(row.InfraEmails)) tagList.Add($"Infra:{row.InfraEmails.Trim()}");
                        if (!string.IsNullOrWhiteSpace(row.MasterDataEmails)) tagList.Add($"MD:{row.MasterDataEmails.Trim()}");

                        // ── CASE 1: UPDATE EXISTING TASK ─────────────────────────────
                        if (row.ActionType == "UPDATE" && row.ExistingTaskId.HasValue)
                        {
                            var existing = await _db.Tasks.FirstOrDefaultAsync(t => t.Id == row.ExistingTaskId.Value);
                            if (existing != null)
                            {
                                existing.Title = row.Title.Trim();
                                if (!string.IsNullOrEmpty(description)) existing.Description = description;
                                if (project != null) existing.ProjectId = project.Id;
                                if (category != null) existing.CategoryId = category.Id;
                                if (assignedUserId != null) existing.AssignedToUserId = assignedUserId;
                                existing.Priority = priority;
                                existing.Status = status;
                                existing.Progress = progress;
                                if (parsedStart.HasValue) existing.StartDate = parsedStart.Value;
                                if (parsedDeadline.HasValue || parsedEnd.HasValue) existing.DueDate = parsedDeadline ?? parsedEnd;
                                if (!string.IsNullOrEmpty(row.Obstacle)) existing.Obstacle = row.Obstacle;
                                if (!string.IsNullOrEmpty(row.Solution)) existing.Solution = row.Solution;
                                if (!string.IsNullOrEmpty(milestone)) existing.Milestone = milestone;
                                if (tagList.Any()) existing.Tags = System.Text.Json.JsonSerializer.Serialize(tagList);
                                existing.UpdatedAt = DateTime.Now;

                                result.UpdatedCount++;
                                result.AffectedTaskIds.Add(existing.Id);
                            }
                            else
                            {
                                // If not found in DB anymore, insert as new
                                var newTask = CreateTaskEntity(row, project?.Id, category?.Id, assignedUserId, priority, status, progress, parsedStart, parsedDeadline ?? parsedEnd, milestone, description, tagList);
                                _db.Tasks.Add(newTask);
                                await _db.SaveChangesAsync();
                                result.InsertedCount++;
                                result.AffectedTaskIds.Add(newTask.Id);
                            }
                        }
                        // ── CASE 2: INSERT NEW TASK ──────────────────────────────────
                        else if (row.ActionType == "INSERT")
                        {
                            var newTask = CreateTaskEntity(row, project?.Id, category?.Id, assignedUserId, priority, status, progress, parsedStart, parsedDeadline ?? parsedEnd, milestone, description, tagList);
                            _db.Tasks.Add(newTask);
                            await _db.SaveChangesAsync();
                            result.InsertedCount++;
                            result.AffectedTaskIds.Add(newTask.Id);
                        }
                        else
                        {
                            // UNCHANGED: Update PIC if explicitly overridden from dropdown
                            if (row.ExistingTaskId.HasValue && assignedUserId != null)
                            {
                                var existing = await _db.Tasks.FirstOrDefaultAsync(t => t.Id == row.ExistingTaskId.Value);
                                if (existing != null && existing.AssignedToUserId != assignedUserId)
                                {
                                    existing.AssignedToUserId = assignedUserId;
                                    existing.UpdatedAt = DateTime.Now;
                                    result.UpdatedCount++;
                                    result.AffectedTaskIds.Add(existing.Id);
                                }
                                else
                                {
                                    result.UnchangedCount++;
                                }
                            }
                            else
                            {
                                result.UnchangedCount++;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        result.FailedCount++;
                        result.Errors.Add($"Baris {row.RowNumber} [{row.SheetName}]: {ex.Message}");
                    }
                }

                await _db.SaveChangesAsync();
                await transaction.CommitAsync();

                // Record Import Audit Log
                _db.ImportLogs.Add(new ImportLog
                {
                    FileName = $"{model.FileName} ({model.SourceType})",
                    TotalRows = model.TotalRows,
                    SuccessRows = result.InsertedCount + result.UpdatedCount + result.UnchangedCount,
                    FailedRows = result.FailedCount,
                    Errors = result.Errors.Any() ? string.Join("\n", result.Errors) : null,
                    ImportedAt = DateTime.Now,
                    ImportedBy = currentUserName ?? "System Sync"
                });
                await _db.SaveChangesAsync();

                result.Message = $"Sinkronisasi berhasil! {result.InsertedCount} tugas baru ditambahkan, {result.UpdatedCount} tugas diperbarui, dan {result.UnchangedCount} tugas identik.";
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                result.Success = false;
                result.Message = $"Gagal mengeksekusi sinkronisasi: {ex.Message}";
                result.Errors.Add(ex.Message);
                _logger.LogError(ex, "Failed to execute Excel sync");
            }

            return result;
        }

        #region Helper Methods

        private WorkTask CreateTaskEntity(
            ImportPreviewRow row,
            int? projectId,
            int? categoryId,
            string? assignedUserId,
            TaskPriority priority,
            ModelTaskStatus status,
            int progress,
            DateTime? startDate,
            DateTime? dueDate,
            string milestone,
            string? description,
            List<string> tagList)
        {
            return new WorkTask
            {
                Title = row.Title.Trim(),
                Description = description,
                ProjectId = projectId,
                CategoryId = categoryId,
                AssignedToUserId = assignedUserId,
                Priority = priority,
                Status = status,
                Progress = progress,
                StartDate = startDate,
                DueDate = dueDate,
                Obstacle = row.Obstacle,
                Solution = row.Solution,
                Milestone = milestone,
                Tags = tagList.Any() ? System.Text.Json.JsonSerializer.Serialize(tagList) : null,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };
        }

        public static string? ExtractPersonFromSheetName(string sheetName)
        {
            if (string.IsNullOrWhiteSpace(sheetName)) return null;

            // Pattern 1: e.g. "Integrasi TCES TICS (Syafix)" -> "Syafix"
            var match = Regex.Match(sheetName, @"\(([^)]+)\)");
            if (match.Success)
            {
                return match.Groups[1].Value.Trim();
            }

            // Pattern 2: Single person names e.g. "Glenn", "Danang", "Heni"
            var clean = sheetName.Trim();
            if (clean.Equals("Glenn", StringComparison.OrdinalIgnoreCase) ||
                clean.Equals("Syafix", StringComparison.OrdinalIgnoreCase) ||
                clean.Equals("Danang", StringComparison.OrdinalIgnoreCase) ||
                clean.Equals("Heni", StringComparison.OrdinalIgnoreCase) ||
                clean.Equals("Iqbal", StringComparison.OrdinalIgnoreCase) ||
                clean.Equals("Haviz", StringComparison.OrdinalIgnoreCase) ||
                clean.Equals("Atha", StringComparison.OrdinalIgnoreCase) ||
                clean.Equals("Athallah", StringComparison.OrdinalIgnoreCase) ||
                clean.Equals("Nanda", StringComparison.OrdinalIgnoreCase))
            {
                return clean;
            }

            return null;
        }

        public static AppUser? ResolveAppUser(string? rawPic, List<AppUser> users)
        {
            if (string.IsNullOrWhiteSpace(rawPic)) return null;

            // Strip role in parentheses if present e.g. "Syafix (Business Analyst)" -> "Syafix"
            var cleanPic = rawPic.Trim();
            var roleMatch = Regex.Match(cleanPic, @"^([^(]+)\s*\(.*?\)");
            if (roleMatch.Success)
            {
                cleanPic = roleMatch.Groups[1].Value.Trim();
            }

            // Split by ';' or ',' in case multiple emails
            cleanPic = cleanPic.Split(new[] { ';', ',' }, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault()?.Trim() ?? cleanPic;

            // 1. Exact Email Match
            var matched = users.FirstOrDefault(u => !string.IsNullOrEmpty(u.Email) && u.Email.Equals(cleanPic, StringComparison.OrdinalIgnoreCase));
            if (matched != null) return matched;

            // 2. Exact UserName Match
            matched = users.FirstOrDefault(u => !string.IsNullOrEmpty(u.UserName) && u.UserName.Equals(cleanPic, StringComparison.OrdinalIgnoreCase));
            if (matched != null) return matched;

            // 3. Exact FullName Match
            matched = users.FirstOrDefault(u => !string.IsNullOrEmpty(u.FullName) && u.FullName.Equals(cleanPic, StringComparison.OrdinalIgnoreCase));
            if (matched != null) return matched;

            // 4. Known Team Aliases / First Name matching
            var aliasLower = cleanPic.ToLower();
            if (aliasLower.Contains("syafix"))
                return users.FirstOrDefault(u => u.Email?.Contains("syafix", StringComparison.OrdinalIgnoreCase) == true || u.FullName.Contains("Syafix", StringComparison.OrdinalIgnoreCase));
            if (aliasLower.Contains("glenn"))
                return users.FirstOrDefault(u => u.Email?.Contains("glenn", StringComparison.OrdinalIgnoreCase) == true || u.FullName.Contains("Glenn", StringComparison.OrdinalIgnoreCase));
            if (aliasLower.Contains("danang"))
                return users.FirstOrDefault(u => u.Email?.Contains("danang", StringComparison.OrdinalIgnoreCase) == true || u.FullName.Contains("Danang", StringComparison.OrdinalIgnoreCase));
            if (aliasLower.Contains("heni"))
                return users.FirstOrDefault(u => u.Email?.Contains("heni", StringComparison.OrdinalIgnoreCase) == true || u.FullName.Contains("Heni", StringComparison.OrdinalIgnoreCase));
            if (aliasLower.Contains("iqbal"))
                return users.FirstOrDefault(u => u.Email?.Contains("iqbal", StringComparison.OrdinalIgnoreCase) == true || u.FullName.Contains("Iqbal", StringComparison.OrdinalIgnoreCase));
            if (aliasLower.Contains("haviz"))
                return users.FirstOrDefault(u => u.Email?.Contains("haviz", StringComparison.OrdinalIgnoreCase) == true || u.FullName.Contains("Haviz", StringComparison.OrdinalIgnoreCase));
            if (aliasLower.Contains("atha"))
                return users.FirstOrDefault(u => u.Email?.Contains("athallah", StringComparison.OrdinalIgnoreCase) == true || u.FullName.Contains("Athallah", StringComparison.OrdinalIgnoreCase));
            if (aliasLower.Contains("nanda"))
                return users.FirstOrDefault(u => u.Email?.Contains("nanda", StringComparison.OrdinalIgnoreCase) == true || u.FullName.Contains("Nanda", StringComparison.OrdinalIgnoreCase));

            // 5. Partial Substring Match
            matched = users.FirstOrDefault(u =>
                (!string.IsNullOrEmpty(u.FullName) && (u.FullName.Contains(cleanPic, StringComparison.OrdinalIgnoreCase) || cleanPic.Contains(u.FullName, StringComparison.OrdinalIgnoreCase))) ||
                (!string.IsNullOrEmpty(u.Email) && (u.Email.StartsWith(cleanPic, StringComparison.OrdinalIgnoreCase) || cleanPic.Contains(u.Email, StringComparison.OrdinalIgnoreCase))));

            return matched;
        }

        private static string NormalizeExcelDownloadUrl(string url)
        {
            // Google Sheets export XLSX transformation
            // Example: https://docs.google.com/spreadsheets/d/{SPREADSHEET_ID}/edit#gid=... -> https://docs.google.com/spreadsheets/d/{SPREADSHEET_ID}/export?format=xlsx
            var gMatch = Regex.Match(url, @"docs\.google\.com/spreadsheets/d/([a-zA-Z0-9-_]+)");
            if (gMatch.Success)
            {
                var docId = gMatch.Groups[1].Value;
                return $"https://docs.google.com/spreadsheets/d/{docId}/export?format=xlsx";
            }

            // Dropbox direct download parameter (?dl=1)
            if (url.Contains("dropbox.com", StringComparison.OrdinalIgnoreCase) && !url.Contains("dl=1"))
            {
                return url.Contains("?") ? $"{url}&dl=1" : $"{url}?dl=1";
            }

            return url;
        }

        private static string ExtractFileNameFromUrl(string url)
        {
            try
            {
                var uri = new Uri(url);
                var segment = Path.GetFileName(uri.LocalPath);
                if (!string.IsNullOrEmpty(segment) && (segment.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase) || segment.EndsWith(".xls", StringComparison.OrdinalIgnoreCase)))
                {
                    return segment;
                }
            }
            catch { }

            return $"Spreadsheet_Sync_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
        }

        private static string GetCellByColName(IXLRow row, Dictionary<string, int> colMap, string colName, int defaultColIdx)
        {
            if (colMap.TryGetValue(colName, out var colIdx) && colIdx > 0)
            {
                return row.Cell(colIdx).GetString()?.Trim() ?? string.Empty;
            }
            if (defaultColIdx > 0 && defaultColIdx <= (row.LastCellUsed()?.Address.ColumnNumber ?? 0))
            {
                return row.Cell(defaultColIdx).GetString()?.Trim() ?? string.Empty;
            }
            return string.Empty;
        }

        private static IXLCell GetCellRef(IXLRow row, Dictionary<string, int> colMap, string colName, int defaultColIdx)
        {
            if (colMap.TryGetValue(colName, out var colIdx) && colIdx > 0)
            {
                return row.Cell(colIdx);
            }
            if (defaultColIdx > 0 && defaultColIdx <= (row.LastCellUsed()?.Address.ColumnNumber ?? 0))
            {
                return row.Cell(defaultColIdx);
            }
            return row.Cell(1);
        }

        public static string NormalizeStatusString(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return "Todo";
            var clean = raw.Trim().ToUpper();
            return clean switch
            {
                "DONE" or "SELESAI" => "Done",
                "IN_PROGRESS" or "INPROGRESS" or "PROGRESS" or "IN PROGRESS" or "SEDANG DIKERJAKAN" => "InProgress",
                "OVERDUE" or "TERLAMBAT" => "Overdue",
                _ => "Todo"
            };
        }

        public static string NormalizePriorityString(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return "Medium";
            var clean = raw.Trim().ToUpper();
            return clean switch
            {
                "CRITICAL" or "KRITIS" or "URGENT" => "Critical",
                "HIGH" or "TINGGI" => "High",
                "LOW" or "RENDAH" => "Low",
                _ => "Medium"
            };
        }

        public static string ExtractDateString(IXLCell cell)
        {
            if (cell.IsEmpty()) return string.Empty;

            try
            {
                if (cell.DataType == XLDataType.DateTime || cell.Value.IsDateTime)
                {
                    if (cell.TryGetValue<DateTime>(out var dt))
                        return dt.ToString("yyyy-MM-dd");
                }

                if (cell.DataType == XLDataType.Number && cell.TryGetValue<double>(out var num))
                {
                    if (num >= 30000 && num <= 75000)
                    {
                        return DateTime.FromOADate(num).ToString("yyyy-MM-dd");
                    }
                }

                var formatted = cell.GetFormattedString()?.Trim();
                if (!string.IsNullOrEmpty(formatted)) return formatted;

                return cell.GetString()?.Trim() ?? string.Empty;
            }
            catch
            {
                return cell.GetString()?.Trim() ?? string.Empty;
            }
        }

        public static DateTime? ParseDateRobust(string? val)
        {
            if (string.IsNullOrWhiteSpace(val)) return null;
            val = val.Trim();

            // 1. Numeric OA Date
            if (double.TryParse(val, NumberStyles.Any, CultureInfo.InvariantCulture, out var oa) && oa >= 30000 && oa <= 75000)
            {
                try { return DateTime.FromOADate(oa); } catch { }
            }

            // 2. Standard Formats
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

            var idCulture = new CultureInfo("id-ID");
            if (DateTime.TryParse(val, idCulture, DateTimeStyles.None, out var dtId))
                return dtId;

            if (DateTime.TryParse(val, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dtInv))
                return dtInv;

            if (DateTime.TryParse(val, out var dtGen))
                return dtGen;

            return null;
        }

        #endregion
    }
}
