using System.Data.Common;
using System.Globalization;
using System.Text;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using TrackerKerja.Data;

namespace TrackerKerja.Services
{
    public class DatabaseRestoreResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string? BackupFileName { get; set; }
        public int AffectedTablesCount { get; set; }
        public long DurationMs { get; set; }
        public string? ErrorDetails { get; set; }
    }

    public interface IDatabaseExportService
    {
        Task<byte[]> GetDatabaseBinarySnapshotAsync();
        Task<string> GenerateFullSqlDumpAsync();
        string GetDatabaseFilePath();
        Task<DatabaseRestoreResult> RestoreFromBinaryAsync(Stream sourceStream, bool backupBeforeRestore = true);
        Task<DatabaseRestoreResult> RestoreFromSqlAsync(string sqlScript, bool backupBeforeRestore = true);
    }

    public class DatabaseExportService : IDatabaseExportService
    {
        private readonly AppDbContext _db;
        private readonly IConfiguration _config;

        public DatabaseExportService(AppDbContext db, IConfiguration config)
        {
            _db = db;
            _config = config;
        }

        public string GetDatabaseFilePath()
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

        public async Task<byte[]> GetDatabaseBinarySnapshotAsync()
        {
            var dbPath = GetDatabaseFilePath();

            // Run checkpoint to flush WAL logs to main DB file
            try
            {
                var conn = _db.Database.GetDbConnection();
                var wasOpen = conn.State == System.Data.ConnectionState.Open;
                if (!wasOpen) await conn.OpenAsync();

                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "PRAGMA wal_checkpoint(FULL);";
                    await cmd.ExecuteNonQueryAsync();
                }

                if (!wasOpen) await conn.CloseAsync();
            }
            catch { }

            // Read file with FileShare.ReadWrite so active connection doesn't block copy
            if (File.Exists(dbPath))
            {
                using var fs = new FileStream(dbPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
                using var ms = new MemoryStream();
                await fs.CopyToAsync(ms);
                return ms.ToArray();
            }

            throw new FileNotFoundException($"File database tidak ditemukan di {dbPath}");
        }

        public async Task<string> GenerateFullSqlDumpAsync()
        {
            var sb = new StringBuilder();

            var now = DateTime.Now;
            sb.AppendLine("-- ==============================================================================");
            sb.AppendLine("-- TrackerKerja - Full Database Dump (Schema DDL & Data)");
            sb.AppendLine($"-- Generated At  : {now:yyyy-MM-dd HH:mm:ss} (Local)");
            sb.AppendLine($"-- Application   : Work Tracker Pro (TrackerKerja)");
            sb.AppendLine($"-- Database Type : SQLite");
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
                using (var checkCmd = conn.CreateCommand())
                {
                    checkCmd.CommandText = "PRAGMA wal_checkpoint(FULL);";
                    await checkCmd.ExecuteNonQueryAsync();
                }

                // 1. Fetch all tables from sqlite_master (excluding internal sqlite tables)
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

                // 2. Fetch all user indexes
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

                // 3. Output DDL and DATA for each table
                foreach (var table in tables)
                {
                    sb.AppendLine($"-- ──────────────────────────────────────────────────────────────────────────────");
                    sb.AppendLine($"-- TABLE: \"{table.Name}\"");
                    sb.AppendLine($"-- ──────────────────────────────────────────────────────────────────────────────");
                    sb.AppendLine($"DROP TABLE IF EXISTS \"{table.Name}\";");
                    if (!string.IsNullOrWhiteSpace(table.Sql))
                    {
                        sb.AppendLine(table.Sql + ";");
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

                        sb.AppendLine($"INSERT INTO \"{table.Name}\" ({colListStr}) VALUES ({string.Join(", ", values)});");
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
                        sb.AppendLine(idx.Sql + ";");
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
            sb.AppendLine($"-- End of Dump - Generated successfully ({now:yyyy-MM-dd HH:mm:ss})");
            sb.AppendLine("-- ==============================================================================");

            return sb.ToString();
        }

        public async Task<DatabaseRestoreResult> RestoreFromBinaryAsync(Stream sourceStream, bool backupBeforeRestore = true)
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            string? backupFileName = null;
            var dbPath = GetDatabaseFilePath();
            var tempDir = Path.GetDirectoryName(dbPath) ?? Directory.GetCurrentDirectory();
            var tempFile = Path.Combine(tempDir, $"restore_{Guid.NewGuid():N}.tmp");

            try
            {
                // 1. Copy uploaded stream to temp file first to inspect safely
                using (var fs = new FileStream(tempFile, FileMode.Create, FileAccess.ReadWrite, FileShare.None))
                {
                    await sourceStream.CopyToAsync(fs);
                    fs.Position = 0;

                    // 2. Validate SQLite header magic bytes (16 bytes: "SQLite format 3\0")
                    var header = new byte[16];
                    var bytesRead = await fs.ReadAsync(header, 0, 16);
                    if (bytesRead < 16 || Encoding.ASCII.GetString(header) != "SQLite format 3\0")
                    {
                        fs.Close();
                        if (File.Exists(tempFile)) File.Delete(tempFile);
                        return new DatabaseRestoreResult
                        {
                            Success = false,
                            Message = "Berkas yang diunggah bukan file database SQLite (.db) yang valid atau berkas rusak.",
                            DurationMs = sw.ElapsedMilliseconds
                        };
                    }
                }

                // 3. Pre-restore safety backup
                if (backupBeforeRestore)
                {
                    try
                    {
                        var backupBytes = await GetDatabaseBinarySnapshotAsync();
                        var backupsDir = Path.Combine(Directory.GetCurrentDirectory(), "backups");
                        if (!Directory.Exists(backupsDir)) Directory.CreateDirectory(backupsDir);

                        backupFileName = $"TrackerKerja_PreRestore_{DateTime.Now:yyyyMMdd_HHmmss}.db";
                        var backupPath = Path.Combine(backupsDir, backupFileName);
                        await File.WriteAllBytesAsync(backupPath, backupBytes);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[DatabaseExportService] Warning creating pre-restore backup: {ex.Message}");
                    }
                }

                // 4. Release locks on active database
                try
                {
                    var currentConn = _db.Database.GetDbConnection();
                    if (currentConn.State == System.Data.ConnectionState.Open)
                    {
                        using var walCmd = currentConn.CreateCommand();
                        walCmd.CommandText = "PRAGMA wal_checkpoint(FULL);";
                        await walCmd.ExecuteNonQueryAsync();
                        await currentConn.CloseAsync();
                    }
                }
                catch { }

                // Clear all active and idle connection pools so file handle is unlocked on Windows
                SqliteConnection.ClearAllPools();

                // 5. Delete active WAL and SHM journal files
                var walFile = dbPath + "-wal";
                var shmFile = dbPath + "-shm";
                if (File.Exists(walFile)) { try { File.Delete(walFile); } catch { } }
                if (File.Exists(shmFile)) { try { File.Delete(shmFile); } catch { } }

                // 6. Overwrite active DB file with retry for transient file lock
                bool replaced = false;
                Exception? lastEx = null;
                for (int i = 0; i < 5; i++)
                {
                    try
                    {
                        File.Move(tempFile, dbPath, overwrite: true);
                        replaced = true;
                        break;
                    }
                    catch (Exception ex)
                    {
                        lastEx = ex;
                        SqliteConnection.ClearAllPools();
                        await Task.Delay(100);
                    }
                }

                if (!replaced)
                {
                    if (File.Exists(tempFile)) File.Delete(tempFile);
                    throw lastEx ?? new IOException("Gagal menimpa berkas database aktif karena berkas masih terkunci oleh proses lain.");
                }

                // 7. Verify new database integrity & count tables
                int tableCount = 0;
                var verifyConn = _db.Database.GetDbConnection();
                await verifyConn.OpenAsync();
                try
                {
                    using var cmd = verifyConn.CreateCommand();
                    cmd.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name NOT LIKE 'sqlite_%';";
                    var obj = await cmd.ExecuteScalarAsync();
                    tableCount = obj != null ? Convert.ToInt32(obj) : 0;
                }
                finally
                {
                    await verifyConn.CloseAsync();
                }

                sw.Stop();
                return new DatabaseRestoreResult
                {
                    Success = true,
                    Message = $"Restore database SQLite (.db) berhasil dipulihkan! Terdapat {tableCount} tabel aktif dalam database.",
                    BackupFileName = backupFileName,
                    AffectedTablesCount = tableCount,
                    DurationMs = sw.ElapsedMilliseconds
                };
            }
            catch (Exception ex)
            {
                sw.Stop();
                if (File.Exists(tempFile))
                {
                    try { File.Delete(tempFile); } catch { }
                }

                return new DatabaseRestoreResult
                {
                    Success = false,
                    Message = $"Gagal memulihkan database SQLite: {ex.Message}",
                    ErrorDetails = ex.ToString(),
                    BackupFileName = backupFileName,
                    DurationMs = sw.ElapsedMilliseconds
                };
            }
        }

        public async Task<DatabaseRestoreResult> RestoreFromSqlAsync(string sqlScript, bool backupBeforeRestore = true)
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            string? backupFileName = null;

            if (string.IsNullOrWhiteSpace(sqlScript))
            {
                return new DatabaseRestoreResult
                {
                    Success = false,
                    Message = "Script SQL restore kosong atau tidak valid."
                };
            }

            try
            {
                // 1. Pre-restore safety backup
                if (backupBeforeRestore)
                {
                    try
                    {
                        var backupBytes = await GetDatabaseBinarySnapshotAsync();
                        var backupsDir = Path.Combine(Directory.GetCurrentDirectory(), "backups");
                        if (!Directory.Exists(backupsDir)) Directory.CreateDirectory(backupsDir);

                        backupFileName = $"TrackerKerja_PreRestore_{DateTime.Now:yyyyMMdd_HHmmss}.db";
                        var backupPath = Path.Combine(backupsDir, backupFileName);
                        await File.WriteAllBytesAsync(backupPath, backupBytes);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[DatabaseExportService] Warning creating pre-restore backup: {ex.Message}");
                    }
                }

                var conn = _db.Database.GetDbConnection();
                var wasOpen = conn.State == System.Data.ConnectionState.Open;
                if (!wasOpen) await conn.OpenAsync();

                try
                {
                    // Disable foreign keys temporarily during restore execution
                    using (var fkOffCmd = conn.CreateCommand())
                    {
                        fkOffCmd.CommandText = "PRAGMA foreign_keys = OFF;";
                        await fkOffCmd.ExecuteNonQueryAsync();
                    }

                    // Execute batch SQL script
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = sqlScript;
                        cmd.CommandTimeout = 600; // 10 minutes timeout for large scripts
                        await cmd.ExecuteNonQueryAsync();
                    }

                    // Re-enable foreign keys
                    using (var fkOnCmd = conn.CreateCommand())
                    {
                        fkOnCmd.CommandText = "PRAGMA foreign_keys = ON;";
                        await fkOnCmd.ExecuteNonQueryAsync();
                    }

                    // WAL checkpoint
                    try
                    {
                        using (var chkCmd = conn.CreateCommand())
                        {
                            chkCmd.CommandText = "PRAGMA wal_checkpoint(FULL);";
                            await chkCmd.ExecuteNonQueryAsync();
                        }
                    }
                    catch { }

                    // Count tables
                    int tableCount = 0;
                    using (var countCmd = conn.CreateCommand())
                    {
                        countCmd.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name NOT LIKE 'sqlite_%';";
                        var obj = await countCmd.ExecuteScalarAsync();
                        tableCount = obj != null ? Convert.ToInt32(obj) : 0;
                    }

                    sw.Stop();
                    return new DatabaseRestoreResult
                    {
                        Success = true,
                        Message = $"Restore database dari script SQL berhasil diterapkan! Terdapat {tableCount} tabel aktif dalam database.",
                        BackupFileName = backupFileName,
                        AffectedTablesCount = tableCount,
                        DurationMs = sw.ElapsedMilliseconds
                    };
                }
                finally
                {
                    if (!wasOpen) await conn.CloseAsync();
                }
            }
            catch (Exception ex)
            {
                sw.Stop();
                return new DatabaseRestoreResult
                {
                    Success = false,
                    Message = $"Gagal mengeksekusi restore SQL: {ex.Message}",
                    ErrorDetails = ex.ToString(),
                    BackupFileName = backupFileName,
                    DurationMs = sw.ElapsedMilliseconds
                };
            }
        }
    }
}
