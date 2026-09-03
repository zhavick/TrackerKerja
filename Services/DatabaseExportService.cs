using System.Data.Common;
using System.Globalization;
using System.Text;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using TrackerKerja.Data;

namespace TrackerKerja.Services
{
    public interface IDatabaseExportService
    {
        Task<byte[]> GetDatabaseBinarySnapshotAsync();
        Task<string> GenerateFullSqlDumpAsync();
        string GetDatabaseFilePath();
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
    }
}
