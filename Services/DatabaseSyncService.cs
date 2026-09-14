using System.Data.Common;
using System.Diagnostics;
using System.Globalization;
using System.IO.Compression;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using TrackerKerja.Data;
using TrackerKerja.Models;
using TrackerKerja.ViewModels;

namespace TrackerKerja.Services
{
    public interface IDatabaseSyncService
    {
        Task<SyncPingResponseDto> PingHostAsync(string hostUrl, string apiKey);
        Task<string> GenerateSyncSqlDumpAsync(bool cleanBeforeSync = true);
        Task<SyncPayloadDto> BuildSyncPayloadAsync(string? sourceLabel = null, bool includeFiles = true);
        Task<SyncResultDto> PushSyncToHostAsync(string hostUrl, string apiKey, bool cleanBeforeSync = true, bool backupBeforeSync = true, string? sourceLabel = null, bool syncFiles = true);
        Task<SyncResultDto> PullSyncFromHostAsync(string hostUrl, string apiKey, bool cleanBeforeSync = true, bool backupBeforeSync = true);
        Task<SyncResultDto> ExecuteSqlSyncAsync(string sqlScript, bool cleanBeforeSync = true, bool backupBeforeSync = true, string? sourceLabel = null, string? filesZipBase64 = null);
        Task<SyncResultDto> ExecutePackageSyncAsync(Stream packageZipStream, bool cleanBeforeSync = true, bool backupBeforeSync = true, string? sourceLabel = null);
        Task<byte[]> CreateUploadsZipArchiveAsync();
        Task<(int FileCount, long TotalBytes)> ExtractUploadsZipArchiveAsync(byte[] zipBytes);
        Task<byte[]> GenerateFullSyncPackageZipAsync(bool cleanBeforeSync = true);
        Task<(int FileCount, long TotalBytes, string FormattedSize)> GetUploadsStatsAsync();
        Task<SyncSettingsDto> GetSyncSettingsAsync();
        Task<bool> SaveSyncSettingsAsync(SyncSettingsDto settings);
        Task<bool> VerifyApiKeyAsync(string providedKey);
    }

    public class DatabaseSyncService : IDatabaseSyncService
    {
        private readonly AppDbContext _db;
        private readonly IConfiguration _config;
        private readonly IWebHostEnvironment _env;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IDatabaseExportService _exportService;

        public DatabaseSyncService(
            AppDbContext db,
            IConfiguration config,
            IWebHostEnvironment env,
            IHttpClientFactory httpClientFactory,
            IDatabaseExportService exportService)
        {
            _db = db;
            _config = config;
            _env = env;
            _httpClientFactory = httpClientFactory;
            _exportService = exportService;
        }

        public async Task<SyncSettingsDto> GetSyncSettingsAsync()
        {
            var targetUrl = (await _db.SystemSettings.FirstOrDefaultAsync(s => s.Key == "HostSync_TargetUrl"))?.Value ?? "http://localhost:5000";
            var apiKey = (await _db.SystemSettings.FirstOrDefaultAsync(s => s.Key == "HostSync_ApiKey"))?.Value ?? "TK-SYNC-SECRET-KEY-2026";
            var role = (await _db.SystemSettings.FirstOrDefaultAsync(s => s.Key == "HostSync_Role"))?.Value ?? "child";
            var lastSyncAtStr = (await _db.SystemSettings.FirstOrDefaultAsync(s => s.Key == "HostSync_LastSyncAt"))?.Value;
            var lastSyncStatus = (await _db.SystemSettings.FirstOrDefaultAsync(s => s.Key == "HostSync_LastSyncStatus"))?.Value;

            DateTime? lastSyncAt = null;
            if (!string.IsNullOrWhiteSpace(lastSyncAtStr) && DateTime.TryParse(lastSyncAtStr, out var parsed))
            {
                lastSyncAt = parsed;
            }

            return new SyncSettingsDto
            {
                TargetHostUrl = targetUrl,
                ApiKey = apiKey,
                Role = role,
                LastSyncAt = lastSyncAt,
                LastSyncStatus = lastSyncStatus,
                CleanBeforeSyncDefault = true,
                BackupBeforeSyncDefault = true,
                SyncFilesDefault = true
            };
        }

        public async Task<bool> SaveSyncSettingsAsync(SyncSettingsDto settings)
        {
            await UpsertSettingAsync("HostSync_TargetUrl", settings.TargetHostUrl?.Trim().TrimEnd('/') ?? "http://localhost:5000", "Target URL Host Induk untuk sinkronisasi API");
            await UpsertSettingAsync("HostSync_ApiKey", settings.ApiKey?.Trim() ?? string.Empty, "Secret API Key untuk autentikasi sinkronisasi antar instance");
            await UpsertSettingAsync("HostSync_Role", settings.Role?.Trim() ?? "child", "Peran instance: child (pengisian) atau host (induk)");

            if (settings.LastSyncAt.HasValue)
            {
                await UpsertSettingAsync("HostSync_LastSyncAt", settings.LastSyncAt.Value.ToString("yyyy-MM-dd HH:mm:ss"), "Waktu terakhir sinkronisasi berhasil");
            }

            if (!string.IsNullOrEmpty(settings.LastSyncStatus))
            {
                await UpsertSettingAsync("HostSync_LastSyncStatus", settings.LastSyncStatus, "Status atau keterangan sinkronisasi terakhir");
            }

            await _db.SaveChangesAsync();
            return true;
        }

        public async Task<bool> VerifyApiKeyAsync(string providedKey)
        {
            if (string.IsNullOrWhiteSpace(providedKey)) return false;

            var configuredKey = (await _db.SystemSettings.FirstOrDefaultAsync(s => s.Key == "HostSync_ApiKey"))?.Value;
            if (string.IsNullOrWhiteSpace(configuredKey))
            {
                configuredKey = "TK-SYNC-SECRET-KEY-2026";
            }

            return string.Equals(providedKey.Trim(), configuredKey.Trim(), StringComparison.Ordinal);
        }

        public Task<(int FileCount, long TotalBytes, string FormattedSize)> GetUploadsStatsAsync()
        {
            var uploadsDir = GetUploadsDirectory();
            if (!Directory.Exists(uploadsDir))
            {
                return Task.FromResult((0, 0L, "0 B"));
            }

            var files = Directory.GetFiles(uploadsDir, "*.*", SearchOption.AllDirectories);
            int count = files.Length;
            long totalBytes = 0;

            foreach (var f in files)
            {
                try
                {
                    var fi = new FileInfo(f);
                    totalBytes += fi.Length;
                }
                catch { }
            }

            string formatted = FormatBytes(totalBytes);
            return Task.FromResult((count, totalBytes, formatted));
        }

        public async Task<SyncPingResponseDto> PingHostAsync(string hostUrl, string apiKey)
        {
            if (string.IsNullOrWhiteSpace(hostUrl))
            {
                return new SyncPingResponseDto
                {
                    IsOnline = false,
                    Message = "Target URL Host Induk tidak boleh kosong."
                };
            }

            var cleanUrl = hostUrl.Trim().TrimEnd('/');
            var client = _httpClientFactory.CreateClient();
            client.Timeout = TimeSpan.FromSeconds(15);

            try
            {
                var request = new HttpRequestMessage(HttpMethod.Get, $"{cleanUrl}/api/sync/ping");
                if (!string.IsNullOrWhiteSpace(apiKey))
                {
                    request.Headers.Add("X-Sync-ApiKey", apiKey.Trim());
                }

                var response = await client.SendAsync(request);
                if (response.IsSuccessStatusCode)
                {
                    var pingResult = await response.Content.ReadFromJsonAsync<ApiResponse<SyncPingResponseDto>>();
                    if (pingResult?.Data != null)
                    {
                        pingResult.Data.IsOnline = true;
                        return pingResult.Data;
                    }
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized || response.StatusCode == System.Net.HttpStatusCode.Forbidden)
                {
                    return new SyncPingResponseDto
                    {
                        IsOnline = false,
                        Message = $"Autentikasi Ditolak oleh Host Induk (HTTP {(int)response.StatusCode}). API Key tidak cocok atau belum disetel sama dengan Host Induk."
                    };
                }
                else
                {
                    return new SyncPingResponseDto
                    {
                        IsOnline = false,
                        Message = $"Host Induk merespons dengan HTTP {(int)response.StatusCode} {response.ReasonPhrase}."
                    };
                }
            }
            catch (HttpRequestException ex)
            {
                return new SyncPingResponseDto
                {
                    IsOnline = false,
                    Message = $"Gagal terhubung ke Host Induk di '{cleanUrl}'. Pastikan server lokal/induk aktif dan port dapat diakses: {ex.Message}"
                };
            }
            catch (TaskCanceledException)
            {
                return new SyncPingResponseDto
                {
                    IsOnline = false,
                    Message = $"Koneksi ke Host Induk timed out (melebihi batas 15 detik) di '{cleanUrl}'."
                };
            }
            catch (Exception ex)
            {
                return new SyncPingResponseDto
                {
                    IsOnline = false,
                    Message = $"Terjadi kesalahan saat ping Host Induk: {ex.Message}"
                };
            }

            return new SyncPingResponseDto { IsOnline = false, Message = "Tidak dapat memverifikasi respons dari Host Induk." };
        }

        public async Task<string> GenerateSyncSqlDumpAsync(bool cleanBeforeSync = true)
        {
            var sb = new StringBuilder();
            var now = DateTime.Now;

            sb.AppendLine("-- ==============================================================================");
            sb.AppendLine("-- TrackerKerja - Multi-Instance Synchronization SQL Package");
            sb.AppendLine($"-- Generated At  : {now:yyyy-MM-dd HH:mm:ss} (Local)");
            sb.AppendLine($"-- Mode          : {(cleanBeforeSync ? "Clean Slate & Full Replace" : "Merge / Upsert")}");
            sb.AppendLine($"-- Application   : Work Tracker Pro (TrackerKerja)");
            sb.AppendLine("-- ==============================================================================");
            sb.AppendLine();
            sb.AppendLine("PRAGMA foreign_keys = OFF;");
            sb.AppendLine("BEGIN TRANSACTION;");
            sb.AppendLine();

            var conn = _db.Database.GetDbConnection();
            var wasOpen = conn.State == System.Data.ConnectionState.Open;
            if (!wasOpen) await conn.OpenAsync();

            try
            {
                // Ensure WAL checkpoint
                try
                {
                    using var checkCmd = conn.CreateCommand();
                    checkCmd.CommandText = "PRAGMA wal_checkpoint(FULL);";
                    await checkCmd.ExecuteNonQueryAsync();
                }
                catch { }

                // 1. Fetch tables
                var tables = new List<(string Name, string Sql)>();
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "SELECT name, sql FROM sqlite_master WHERE type='table' AND name NOT LIKE 'sqlite_%' ORDER BY name;";
                    using var reader = await cmd.ExecuteReaderAsync();
                    while (await reader.ReadAsync())
                    {
                        var name = reader.GetString(0);
                        var sql = reader.IsDBNull(1) ? string.Empty : reader.GetString(1);
                        tables.Add((name, sql));
                    }
                }

                // 2. Fetch indexes
                var indexes = new List<(string Name, string TableName, string Sql)>();
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "SELECT name, tbl_name, sql FROM sqlite_master WHERE type='index' AND sql IS NOT NULL AND name NOT LIKE 'sqlite_%' ORDER BY tbl_name, name;";
                    using var reader = await cmd.ExecuteReaderAsync();
                    while (await reader.ReadAsync())
                    {
                        var name = reader.GetString(0);
                        var tblName = reader.GetString(1);
                        var sql = reader.IsDBNull(2) ? string.Empty : reader.GetString(2);
                        indexes.Add((name, tblName, sql));
                    }
                }

                // 3. For each table: Drop/Recreate (if clean) or Delete, then Insert All Records
                foreach (var table in tables)
                {
                    sb.AppendLine($"-- ──────────────────────────────────────────────────────────────────────────────");
                    sb.AppendLine($"-- TABLE: \"{table.Name}\"");
                    sb.AppendLine($"-- ──────────────────────────────────────────────────────────────────────────────");

                    if (cleanBeforeSync)
                    {
                        sb.AppendLine($"DROP TABLE IF EXISTS \"{table.Name}\";");
                        if (!string.IsNullOrWhiteSpace(table.Sql))
                        {
                            sb.AppendLine(table.Sql + ";");
                        }
                    }
                    else
                    {
                        if (!string.IsNullOrWhiteSpace(table.Sql))
                        {
                            var createIfNotExists = table.Sql.StartsWith("CREATE TABLE", StringComparison.OrdinalIgnoreCase)
                                ? "CREATE TABLE IF NOT EXISTS " + table.Sql.Substring(12).TrimStart()
                                : table.Sql;
                            sb.AppendLine(createIfNotExists + ";");
                        }
                    }

                    sb.AppendLine();

                    // Read Table Data
                    using var selectCmd = conn.CreateCommand();
                    selectCmd.CommandText = $"SELECT * FROM \"{table.Name}\";";

                    using var reader = await selectCmd.ExecuteReaderAsync();
                    int fieldCount = reader.FieldCount;
                    var colNames = new List<string>();
                    for (int i = 0; i < fieldCount; i++)
                    {
                        colNames.Add($"\"{reader.GetName(i)}\"");
                    }
                    var colListStr = string.Join(", ", colNames);

                    int rowCount = 0;
                    var insertKeyword = cleanBeforeSync ? "INSERT INTO" : "INSERT OR REPLACE INTO";

                    while (await reader.ReadAsync())
                    {
                        var values = new List<string>();
                        for (int i = 0; i < fieldCount; i++)
                        {
                            if (reader.IsDBNull(i))
                            {
                                values.Add("NULL");
                            }
                            else
                            {
                                var val = reader.GetValue(i);
                                if (val is byte[] bytes)
                                {
                                    values.Add("X'" + Convert.ToHexString(bytes) + "'");
                                }
                                else if (val is bool b)
                                {
                                    values.Add(b ? "1" : "0");
                                }
                                else if (val is DateTime dt)
                                {
                                    values.Add($"'{dt:yyyy-MM-dd HH:mm:ss.fff}'");
                                }
                                else if (val is DateTimeOffset dto)
                                {
                                    values.Add($"'{dto:yyyy-MM-dd HH:mm:ss.fff zzz}'");
                                }
                                else if (val is sbyte or byte or short or ushort or int or uint or long or ulong or float or double or decimal)
                                {
                                    values.Add(Convert.ToString(val, CultureInfo.InvariantCulture)!);
                                }
                                else
                                {
                                    var strVal = val.ToString() ?? string.Empty;
                                    values.Add("'" + strVal.Replace("'", "''") + "'");
                                }
                            }
                        }

                        sb.AppendLine($"{insertKeyword} \"{table.Name}\" ({colListStr}) VALUES ({string.Join(", ", values)});");
                        rowCount++;
                    }

                    sb.AppendLine($"-- Total Records in \"{table.Name}\": {rowCount}");
                    sb.AppendLine();
                }

                // 4. Output Indexes
                if (indexes.Any())
                {
                    sb.AppendLine($"-- ──────────────────────────────────────────────────────────────────────────────");
                    sb.AppendLine($"-- INDEXES");
                    sb.AppendLine($"-- ──────────────────────────────────────────────────────────────────────────────");
                    foreach (var idx in indexes)
                    {
                        sb.AppendLine($"CREATE INDEX IF NOT EXISTS \"{idx.Name}\" ON \"{idx.TableName}\" {GetIndexColumns(idx.Sql)};");
                    }
                    sb.AppendLine();
                }
            }
            finally
            {
                if (!wasOpen) await conn.CloseAsync();
            }

            sb.AppendLine("COMMIT;");
            sb.AppendLine("PRAGMA foreign_keys = ON;");
            sb.AppendLine();
            sb.AppendLine("-- ==============================================================================");
            sb.AppendLine($"-- End of Sync Script - Generated successfully ({now:yyyy-MM-dd HH:mm:ss})");
            sb.AppendLine("-- ==============================================================================");

            return sb.ToString();
        }

        public async Task<byte[]> CreateUploadsZipArchiveAsync()
        {
            var uploadsDir = GetUploadsDirectory();
            if (!Directory.Exists(uploadsDir))
            {
                Directory.CreateDirectory(uploadsDir);
            }

            using var memoryStream = new MemoryStream();
            using (var archive = new ZipArchive(memoryStream, ZipArchiveMode.Create, true))
            {
                var files = Directory.GetFiles(uploadsDir, "*.*", SearchOption.AllDirectories);
                var baseUri = new Uri(uploadsDir.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar);

                foreach (var file in files)
                {
                    var fileUri = new Uri(file);
                    var relativePath = Uri.UnescapeDataString(baseUri.MakeRelativeUri(fileUri).ToString());
                    
                    var entry = archive.CreateEntry(relativePath, CompressionLevel.Optimal);
                    using var entryStream = entry.Open();
                    using var fileStream = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.Read);
                    await fileStream.CopyToAsync(entryStream);
                }
            }

            return memoryStream.ToArray();
        }

        public async Task<(int FileCount, long TotalBytes)> ExtractUploadsZipArchiveAsync(byte[] zipBytes)
        {
            if (zipBytes == null || zipBytes.Length == 0) return (0, 0);

            var uploadsDir = GetUploadsDirectory();
            if (!Directory.Exists(uploadsDir))
            {
                Directory.CreateDirectory(uploadsDir);
            }

            int count = 0;
            long totalBytes = 0;
            var targetRoot = Path.GetFullPath(uploadsDir);

            using var memoryStream = new MemoryStream(zipBytes);
            using var archive = new ZipArchive(memoryStream, ZipArchiveMode.Read);

            foreach (var entry in archive.Entries)
            {
                var normalized = entry.FullName.Replace('\\', '/').TrimStart('/');
                if (string.IsNullOrWhiteSpace(normalized) || normalized.EndsWith('/'))
                {
                    // Directory entry
                    var dirPath = Path.GetFullPath(Path.Combine(targetRoot, normalized));
                    if (dirPath.StartsWith(targetRoot, StringComparison.OrdinalIgnoreCase))
                    {
                        Directory.CreateDirectory(dirPath);
                    }
                    continue;
                }

                var destPath = Path.GetFullPath(Path.Combine(targetRoot, normalized));
                // Security check against Zip Slip (Directory Traversal)
                if (!destPath.StartsWith(targetRoot, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var parentDir = Path.GetDirectoryName(destPath);
                if (!string.IsNullOrEmpty(parentDir) && !Directory.Exists(parentDir))
                {
                    Directory.CreateDirectory(parentDir);
                }

                using (var entryStream = entry.Open())
                using (var fileStream = new FileStream(destPath, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    await entryStream.CopyToAsync(fileStream);
                }

                count++;
                totalBytes += entry.Length;
            }

            return (count, totalBytes);
        }

        public async Task<byte[]> GenerateFullSyncPackageZipAsync(bool cleanBeforeSync = true)
        {
            var sqlDump = await GenerateSyncSqlDumpAsync(cleanBeforeSync);
            var stats = await GetUploadsStatsAsync();
            var setting = await _db.SystemSettings.FirstOrDefaultAsync(s => s.Key == "GlobalBaseUrl");
            var baseUrl = setting?.Value ?? "https://tracker.saidilmuna.space";

            var manifest = new
            {
                Application = "Work Tracker Pro (TrackerKerja)",
                Version = "v3.1",
                GeneratedAt = DateTime.Now,
                CleanBeforeSync = cleanBeforeSync,
                SourceInstanceUrl = baseUrl,
                TotalUploadFiles = stats.FileCount,
                TotalUploadsSizeBytes = stats.TotalBytes,
                TotalTasks = await _db.Tasks.CountAsync(),
                TotalSessions = await _db.Sessions.CountAsync(),
                TotalNotes = await _db.Notes.CountAsync(),
                TotalAttachments = await _db.NoteAttachments.CountAsync()
            };

            var manifestJson = JsonSerializer.Serialize(manifest, new JsonSerializerOptions { WriteIndented = true });

            using var memoryStream = new MemoryStream();
            using (var archive = new ZipArchive(memoryStream, ZipArchiveMode.Create, true))
            {
                // 1. Add SQL Dump
                var sqlEntry = archive.CreateEntry("sync_data.sql", CompressionLevel.Optimal);
                using (var writer = new StreamWriter(sqlEntry.Open(), Encoding.UTF8))
                {
                    await writer.WriteAsync(sqlDump);
                }

                // 2. Add Manifest JSON
                var manifestEntry = archive.CreateEntry("manifest.json", CompressionLevel.Optimal);
                using (var writer = new StreamWriter(manifestEntry.Open(), Encoding.UTF8))
                {
                    await writer.WriteAsync(manifestJson);
                }

                // 3. Add all files in uploads directory
                var uploadsDir = GetUploadsDirectory();
                if (Directory.Exists(uploadsDir))
                {
                    var files = Directory.GetFiles(uploadsDir, "*.*", SearchOption.AllDirectories);
                    var baseUri = new Uri(uploadsDir.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar);

                    foreach (var file in files)
                    {
                        var fileUri = new Uri(file);
                        var relativePath = Uri.UnescapeDataString(baseUri.MakeRelativeUri(fileUri).ToString());
                        var entryPath = "uploads/" + relativePath.Replace('\\', '/').TrimStart('/');

                        var entry = archive.CreateEntry(entryPath, CompressionLevel.Optimal);
                        using var entryStream = entry.Open();
                        using var fileStream = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.Read);
                        await fileStream.CopyToAsync(entryStream);
                    }
                }
            }

            return memoryStream.ToArray();
        }

        public async Task<SyncResultDto> ExecutePackageSyncAsync(
            Stream packageZipStream,
            bool cleanBeforeSync = true,
            bool backupBeforeSync = true,
            string? sourceLabel = null)
        {
            var sw = Stopwatch.StartNew();
            string? sqlContent = null;
            int extractedFilesCount = 0;
            long extractedFilesBytes = 0;
            var uploadsDir = GetUploadsDirectory();
            var targetUploadsRoot = Path.GetFullPath(uploadsDir);

            if (!Directory.Exists(targetUploadsRoot))
            {
                Directory.CreateDirectory(targetUploadsRoot);
            }

            try
            {
                using var archive = new ZipArchive(packageZipStream, ZipArchiveMode.Read, leaveOpen: true);

                // 1. Look for SQL file
                var sqlEntry = archive.Entries.FirstOrDefault(e => e.FullName.Equals("sync_data.sql", StringComparison.OrdinalIgnoreCase))
                               ?? archive.Entries.FirstOrDefault(e => e.FullName.EndsWith(".sql", StringComparison.OrdinalIgnoreCase));

                if (sqlEntry != null)
                {
                    using var reader = new StreamReader(sqlEntry.Open(), Encoding.UTF8);
                    sqlContent = await reader.ReadToEndAsync();
                }

                // 2. Extract uploaded files from archive
                foreach (var entry in archive.Entries)
                {
                    var normalized = entry.FullName.Replace('\\', '/').TrimStart('/');
                    if (normalized.Equals("sync_data.sql", StringComparison.OrdinalIgnoreCase) ||
                        normalized.Equals("manifest.json", StringComparison.OrdinalIgnoreCase) ||
                        normalized.EndsWith('/'))
                    {
                        continue;
                    }

                    // Remove leading "uploads/" if present in archive
                    string relativeInUploads = normalized;
                    if (relativeInUploads.StartsWith("uploads/", StringComparison.OrdinalIgnoreCase))
                    {
                        relativeInUploads = relativeInUploads.Substring(8);
                    }

                    if (string.IsNullOrWhiteSpace(relativeInUploads)) continue;

                    var destPath = Path.GetFullPath(Path.Combine(targetUploadsRoot, relativeInUploads));
                    if (!destPath.StartsWith(targetUploadsRoot, StringComparison.OrdinalIgnoreCase))
                    {
                        // Zip slip protection
                        continue;
                    }

                    var parentDir = Path.GetDirectoryName(destPath);
                    if (!string.IsNullOrEmpty(parentDir) && !Directory.Exists(parentDir))
                    {
                        Directory.CreateDirectory(parentDir);
                    }

                    using (var entryStream = entry.Open())
                    using (var fileStream = new FileStream(destPath, FileMode.Create, FileAccess.Write, FileShare.None))
                    {
                        await entryStream.CopyToAsync(fileStream);
                    }

                    extractedFilesCount++;
                    extractedFilesBytes += entry.Length;
                }
            }
            catch (InvalidDataException)
            {
                // Not a zip file, possibly a plain .sql file stream
                packageZipStream.Position = 0;
                using var reader = new StreamReader(packageZipStream, Encoding.UTF8);
                sqlContent = await reader.ReadToEndAsync();
            }

            if (string.IsNullOrWhiteSpace(sqlContent))
            {
                return new SyncResultDto
                {
                    Success = false,
                    Message = "Paket sinkronisasi tidak berisi naskah database SQL yang valid."
                };
            }

            // Execute SQL sync and combine stats
            var sqlResult = await ExecuteSqlSyncAsync(sqlContent, cleanBeforeSync, backupBeforeSync, sourceLabel ?? "Paket Sinkronisasi (.zip)");
            sqlResult.SyncedFilesCount = extractedFilesCount;
            sqlResult.SyncedFilesSizeBytes = extractedFilesBytes;

            if (sqlResult.Success && extractedFilesCount > 0)
            {
                sqlResult.Message += $" Serta {extractedFilesCount} file lampiran/upload ({FormatBytes(extractedFilesBytes)}) berhasil disalin ke server.";
            }

            return sqlResult;
        }

        public async Task<SyncPayloadDto> BuildSyncPayloadAsync(string? sourceLabel = null, bool includeFiles = true)
        {
            var setting = await _db.SystemSettings.FirstOrDefaultAsync(s => s.Key == "GlobalBaseUrl");
            var baseUrl = setting?.Value ?? "https://tracker.saidilmuna.space";

            var sqlScript = await GenerateSyncSqlDumpAsync(cleanBeforeSync: true);
            string? filesZipBase64 = null;
            int filesCount = 0;
            long filesSizeBytes = 0;

            if (includeFiles)
            {
                var zipBytes = await CreateUploadsZipArchiveAsync();
                var stats = await GetUploadsStatsAsync();
                filesCount = stats.FileCount;
                filesSizeBytes = stats.TotalBytes;
                if (zipBytes.Length > 0)
                {
                    filesZipBase64 = Convert.ToBase64String(zipBytes);
                }
            }

            var counts = new Dictionary<string, int>
            {
                { "Tasks", await _db.Tasks.CountAsync() },
                { "Sessions", await _db.Sessions.CountAsync() },
                { "Projects", await _db.Projects.CountAsync() },
                { "Categories", await _db.Categories.CountAsync() },
                { "Notes", await _db.Notes.CountAsync() },
                { "NoteAttachments", await _db.NoteAttachments.CountAsync() },
                { "Users", await _db.Users.CountAsync() },
                { "AuditLogs", await _db.AuditLogs.CountAsync() },
                { "MasterPriorities", await _db.MasterPriorities.CountAsync() },
                { "MasterStatuses", await _db.MasterStatuses.CountAsync() },
                { "MasterMilestones", await _db.MasterMilestones.CountAsync() },
                { "MasterBadges", await _db.MasterBadges.CountAsync() },
                { "UserBadges", await _db.UserBadges.CountAsync() }
            };

            return new SyncPayloadDto
            {
                SourceInstanceUrl = baseUrl,
                SourceLabel = string.IsNullOrWhiteSpace(sourceLabel) ? "Child Instance (" + baseUrl + ")" : sourceLabel,
                ExportedAt = DateTime.Now,
                Version = "v3.1",
                RecordCounts = counts,
                SqlScript = sqlScript,
                FilesZipBase64 = filesZipBase64,
                IncludeFiles = includeFiles,
                FilesCount = filesCount,
                FilesSizeBytes = filesSizeBytes
            };
        }

        public async Task<SyncResultDto> PushSyncToHostAsync(
            string hostUrl,
            string apiKey,
            bool cleanBeforeSync = true,
            bool backupBeforeSync = true,
            string? sourceLabel = null,
            bool syncFiles = true)
        {
            var sw = Stopwatch.StartNew();

            if (string.IsNullOrWhiteSpace(hostUrl))
            {
                return new SyncResultDto
                {
                    Success = false,
                    Message = "Target URL Host Induk wajib diisi."
                };
            }

            var cleanUrl = hostUrl.Trim().TrimEnd('/');
            var payload = await BuildSyncPayloadAsync(sourceLabel, includeFiles: syncFiles);

            var client = _httpClientFactory.CreateClient();
            client.Timeout = TimeSpan.FromMinutes(5); // 5 minutes timeout for payload + files

            var receiveReq = new SyncReceiveRequestDto
            {
                CleanBeforeSync = cleanBeforeSync,
                BackupBeforeSync = backupBeforeSync,
                SourceInstanceUrl = payload.SourceInstanceUrl,
                SourceLabel = payload.SourceLabel,
                Timestamp = DateTime.Now,
                SqlScript = payload.SqlScript,
                RecordCounts = payload.RecordCounts,
                FilesZipBase64 = payload.FilesZipBase64,
                IncludeFiles = payload.IncludeFiles,
                FilesCount = payload.FilesCount,
                FilesSizeBytes = payload.FilesSizeBytes
            };

            try
            {
                var httpRequest = new HttpRequestMessage(HttpMethod.Post, $"{cleanUrl}/api/sync/receive")
                {
                    Content = JsonContent.Create(receiveReq)
                };

                if (!string.IsNullOrWhiteSpace(apiKey))
                {
                    httpRequest.Headers.Add("X-Sync-ApiKey", apiKey.Trim());
                }

                var response = await client.SendAsync(httpRequest);
                sw.Stop();

                if (response.IsSuccessStatusCode)
                {
                    var resultObj = await response.Content.ReadFromJsonAsync<ApiResponse<SyncResultDto>>();
                    var data = resultObj?.Data ?? new SyncResultDto { Success = true, Message = "Sinkronisasi berhasil diterima oleh Host Induk." };

                    // Save local record of sync
                    var filesInfo = payload.FilesCount > 0 ? $", {payload.FilesCount} files ({FormatBytes(payload.FilesSizeBytes)})" : "";
                    await UpsertSettingAsync("HostSync_LastSyncAt", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), "Waktu sinkronisasi terakhir");
                    await UpsertSettingAsync("HostSync_LastSyncStatus", $"Berhasil dikirim ke Host ({cleanUrl}{filesInfo}) dalam {sw.ElapsedMilliseconds}ms.", "Status sinkronisasi terakhir");
                    await _db.SaveChangesAsync();

                    data.ExecutionDurationMs = sw.ElapsedMilliseconds;
                    return data;
                }
                else
                {
                    string errorBody = "";
                    try { errorBody = await response.Content.ReadAsStringAsync(); } catch { }

                    var statusText = $"Gagal mengirim data ke Host Induk (HTTP {(int)response.StatusCode}): {response.ReasonPhrase}. {errorBody}";
                    await UpsertSettingAsync("HostSync_LastSyncStatus", statusText, "Status sinkronisasi terakhir");
                    await _db.SaveChangesAsync();

                    return new SyncResultDto
                    {
                        Success = false,
                        Message = statusText,
                        ExecutionDurationMs = sw.ElapsedMilliseconds
                    };
                }
            }
            catch (Exception ex)
            {
                sw.Stop();
                var err = $"Koneksi ke Host Induk gagal: {ex.Message}";
                await UpsertSettingAsync("HostSync_LastSyncStatus", err, "Status sinkronisasi terakhir");
                await _db.SaveChangesAsync();

                return new SyncResultDto
                {
                    Success = false,
                    Message = err,
                    ErrorDetails = ex.ToString(),
                    ExecutionDurationMs = sw.ElapsedMilliseconds
                };
            }
        }

        public async Task<SyncResultDto> PullSyncFromHostAsync(
            string hostUrl,
            string apiKey,
            bool cleanBeforeSync = true,
            bool backupBeforeSync = true)
        {
            var sw = Stopwatch.StartNew();

            if (string.IsNullOrWhiteSpace(hostUrl))
            {
                return new SyncResultDto
                {
                    Success = false,
                    Message = "Target URL Host Induk wajib diisi."
                };
            }

            var cleanUrl = hostUrl.Trim().TrimEnd('/');
            var client = _httpClientFactory.CreateClient();
            client.Timeout = TimeSpan.FromMinutes(5);

            try
            {
                var request = new HttpRequestMessage(HttpMethod.Get, $"{cleanUrl}/api/sync/export-package?cleanBeforeSync={cleanBeforeSync}");
                if (!string.IsNullOrWhiteSpace(apiKey))
                {
                    request.Headers.Add("X-Sync-ApiKey", apiKey.Trim());
                }

                var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
                if (!response.IsSuccessStatusCode)
                {
                    sw.Stop();
                    var errBody = await response.Content.ReadAsStringAsync();
                    return new SyncResultDto
                    {
                        Success = false,
                        Message = $"Gagal mengunduh paket sinkronisasi dari Host Induk (HTTP {(int)response.StatusCode}): {response.ReasonPhrase}. {errBody}",
                        ExecutionDurationMs = sw.ElapsedMilliseconds
                    };
                }

                using var stream = await response.Content.ReadAsStreamAsync();
                var result = await ExecutePackageSyncAsync(stream, cleanBeforeSync, backupBeforeSync, $"Pull dari Host Induk ({cleanUrl})");
                sw.Stop();

                result.ExecutionDurationMs = sw.ElapsedMilliseconds;

                if (result.Success)
                {
                    await UpsertSettingAsync("HostSync_LastSyncAt", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), "Waktu sinkronisasi terakhir");
                    await UpsertSettingAsync("HostSync_LastSyncStatus", $"Berhasil ditarik dari Host Induk ({cleanUrl}) dalam {sw.ElapsedMilliseconds}ms.", "Status sinkronisasi terakhir");
                    await _db.SaveChangesAsync();
                }

                return result;
            }
            catch (Exception ex)
            {
                sw.Stop();
                var err = $"Gagal melakukan pull data dari Host Induk: {ex.Message}";
                return new SyncResultDto
                {
                    Success = false,
                    Message = err,
                    ErrorDetails = ex.ToString(),
                    ExecutionDurationMs = sw.ElapsedMilliseconds
                };
            }
        }

        public async Task<SyncResultDto> ExecuteSqlSyncAsync(
            string sqlScript,
            bool cleanBeforeSync = true,
            bool backupBeforeSync = true,
            string? sourceLabel = null,
            string? filesZipBase64 = null)
        {
            var sw = Stopwatch.StartNew();

            if (string.IsNullOrWhiteSpace(sqlScript))
            {
                return new SyncResultDto
                {
                    Success = false,
                    Message = "Script SQL sinkronisasi kosong atau tidak valid."
                };
            }

            string? backupFileName = null;
            int extractedFilesCount = 0;
            long extractedFilesBytes = 0;

            // 1. Extract attached files if provided in payload
            if (!string.IsNullOrWhiteSpace(filesZipBase64))
            {
                try
                {
                    var zipBytes = Convert.FromBase64String(filesZipBase64);
                    var extractStats = await ExtractUploadsZipArchiveAsync(zipBytes);
                    extractedFilesCount = extractStats.FileCount;
                    extractedFilesBytes = extractStats.TotalBytes;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[SyncService] Warning extracting files payload: {ex.Message}");
                }
            }

            // 2. Safety Backup before syncing if requested
            if (backupBeforeSync)
            {
                try
                {
                    var backupBytes = await _exportService.GetDatabaseBinarySnapshotAsync();
                    var backupsDir = Path.Combine(Directory.GetCurrentDirectory(), "backups");
                    if (!Directory.Exists(backupsDir)) Directory.CreateDirectory(backupsDir);

                    backupFileName = $"TrackerKerja_PreSync_{DateTime.Now:yyyyMMdd_HHmmss}.db";
                    var backupPath = Path.Combine(backupsDir, backupFileName);
                    await File.WriteAllBytesAsync(backupPath, backupBytes);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[SyncService] Warning taking pre-sync backup: {ex.Message}");
                }
            }

            var conn = _db.Database.GetDbConnection();
            var wasOpen = conn.State == System.Data.ConnectionState.Open;
            if (!wasOpen) await conn.OpenAsync();

            int executedCount = 0;
            var affectedTables = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            try
            {
                // Execute entire batch script in SQLite
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = sqlScript;
                    cmd.CommandTimeout = 300; // 5 minutes timeout
                    await cmd.ExecuteNonQueryAsync();
                }

                // Checkpoint WAL and optimize
                try
                {
                    using (var chkCmd = conn.CreateCommand())
                    {
                        chkCmd.CommandText = "PRAGMA wal_checkpoint(FULL);";
                        await chkCmd.ExecuteNonQueryAsync();
                    }
                }
                catch { }

                // Gather table names and final record statistics
                var finalStats = new Dictionary<string, int>();
                using (var countCmd = conn.CreateCommand())
                {
                    countCmd.CommandText = "SELECT name FROM sqlite_master WHERE type='table' AND name NOT LIKE 'sqlite_%' ORDER BY name;";
                    using var rdr = await countCmd.ExecuteReaderAsync();
                    var tblNames = new List<string>();
                    while (await rdr.ReadAsync())
                    {
                        tblNames.Add(rdr.GetString(0));
                    }

                    rdr.Close();

                    foreach (var tbl in tblNames)
                    {
                        affectedTables.Add(tbl);
                        using var singleCount = conn.CreateCommand();
                        singleCount.CommandText = $"SELECT COUNT(*) FROM \"{tbl}\";";
                        var cnt = await singleCount.ExecuteScalarAsync();
                        finalStats[tbl] = cnt != null ? Convert.ToInt32(cnt) : 0;
                    }
                }

                sw.Stop();

                // Audit Log
                try
                {
                    var fileAuditInfo = extractedFilesCount > 0 ? $", {extractedFilesCount} file lampiran ({FormatBytes(extractedFilesBytes)}) disalin" : "";
                    _db.AuditLogs.Add(new AuditLog
                    {
                        ActionName = "HostSync",
                        ControllerName = "Sync",
                        HttpMethod = "POST",
                        Path = "/api/sync/receive",
                        IpAddress = "127.0.0.1",
                        StatusCode = 200,
                        DurationMs = sw.ElapsedMilliseconds,
                        Timestamp = DateTime.Now,
                        Details = $"Sinkronisasi data dari '{sourceLabel ?? "Remote Instance"}' berhasil dieksekusi ({finalStats.GetValueOrDefault("Tasks", 0)} tasks, {finalStats.GetValueOrDefault("Sessions", 0)} sessions{fileAuditInfo}) dalam {sw.ElapsedMilliseconds}ms."
                    });
                    await _db.SaveChangesAsync();
                }
                catch { }

                var fileMsg = extractedFilesCount > 0 ? $" serta {extractedFilesCount} berkas lampiran ({FormatBytes(extractedFilesBytes)}) berhasil disinkronkan" : "";

                return new SyncResultDto
                {
                    Success = true,
                    Message = $"Sinkronisasi database berhasil diterapkan! {affectedTables.Count} tabel diperbarui{fileMsg} dalam {sw.ElapsedMilliseconds}ms.",
                    ExecutedStatementsCount = executedCount > 0 ? executedCount : affectedTables.Count,
                    AffectedTables = affectedTables.ToList(),
                    FinalTableStats = finalStats,
                    SyncedFilesCount = extractedFilesCount,
                    SyncedFilesSizeBytes = extractedFilesBytes,
                    ExecutionDurationMs = sw.ElapsedMilliseconds,
                    Timestamp = DateTime.Now,
                    BackupFileName = backupFileName
                };
            }
            catch (Exception ex)
            {
                sw.Stop();
                return new SyncResultDto
                {
                    Success = false,
                    Message = $"Gagal mengeksekusi sinkronisasi SQL: {ex.Message}",
                    ErrorDetails = ex.ToString(),
                    ExecutionDurationMs = sw.ElapsedMilliseconds,
                    BackupFileName = backupFileName
                };
            }
            finally
            {
                if (!wasOpen) await conn.CloseAsync();
            }
        }

        #region Helpers
        private string GetUploadsDirectory()
        {
            var webRoot = _env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
            return Path.Combine(webRoot, "uploads");
        }

        private static string FormatBytes(long bytes)
        {
            if (bytes < 1024) return $"{bytes} B";
            if (bytes < 1024 * 1024) return $"{bytes / 1024.0:F1} KB";
            return $"{bytes / (1024.0 * 1024.0):F2} MB";
        }

        private async Task UpsertSettingAsync(string key, string value, string description)
        {
            var item = await _db.SystemSettings.FirstOrDefaultAsync(s => s.Key == key);
            if (item == null)
            {
                _db.SystemSettings.Add(new SystemSetting
                {
                    Key = key,
                    Value = value,
                    Description = description,
                    UpdatedAt = DateTime.Now
                });
            }
            else
            {
                item.Value = value;
                item.Description = description;
                item.UpdatedAt = DateTime.Now;
            }
        }

        private static string GetIndexColumns(string? fullIndexSql)
        {
            if (string.IsNullOrWhiteSpace(fullIndexSql)) return "();";
            var onIdx = fullIndexSql.IndexOf(" ON ", StringComparison.OrdinalIgnoreCase);
            if (onIdx != -1)
            {
                var sub = fullIndexSql.Substring(onIdx + 4).Trim();
                var spaceIdx = sub.IndexOf(' ');
                if (spaceIdx != -1)
                {
                    return sub.Substring(spaceIdx).TrimEnd(';');
                }
            }
            return "();";
        }
        #endregion
    }
}
