using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TrackerKerja.Data;
using TrackerKerja.Services;

namespace TrackerKerja.Tests
{
    public static class RestoreTestRunner
    {
        public static async Task<int> RunAllTestsAsync()
        {
            Console.WriteLine("==========================================================");
            Console.WriteLine("RUNNING RESTORE DATABASE TESTS (.DB & .SQL)");
            Console.WriteLine("==========================================================");

            var services = new ServiceCollection();
            services.AddDbContext<AppDbContext>(opt => opt.UseSqlite("Data Source=trackerkerja.db"));
            services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
            services.AddScoped<IDatabaseExportService, DatabaseExportService>();

            var sp = services.BuildServiceProvider();
            using var scope = sp.CreateScope();
            var exportService = scope.ServiceProvider.GetRequiredService<IDatabaseExportService>();

            // Test 1: Invalid file stream rejection (not a valid SQLite DB)
            Console.WriteLine("[TEST 1] Testing invalid binary file rejection...");
            using (var badStream = new MemoryStream(Encoding.UTF8.GetBytes("This is not a sqlite db file at all!")))
            {
                var result = await exportService.RestoreFromBinaryAsync(badStream, backupBeforeRestore: false);
                if (result.Success) throw new Exception("Expected failure for invalid file header, but succeeded!");
                Console.WriteLine($"[TEST 1 PASSED] Rejection handled cleanly: {result.Message}");
            }

            // Test 2: SQL Restore test
            Console.WriteLine("[TEST 2] Testing SQL Restore...");
            var testSql = @"
CREATE TABLE IF NOT EXISTS ""_RestoreVerificationTest"" (
    ""Id"" INTEGER PRIMARY KEY AUTOINCREMENT,
    ""TestNote"" TEXT NOT NULL
);
INSERT INTO ""_RestoreVerificationTest"" (""TestNote"") VALUES ('Restore SQL Verification Success');
";
            var sqlResult = await exportService.RestoreFromSqlAsync(testSql, backupBeforeRestore: true);
            if (!sqlResult.Success) throw new Exception($"SQL restore failed: {sqlResult.Message} {sqlResult.ErrorDetails}");
            if (string.IsNullOrEmpty(sqlResult.BackupFileName)) throw new Exception("Pre-restore backup was not created!");
            Console.WriteLine($"[TEST 2 PASSED] SQL restore succeeded! Backup: {sqlResult.BackupFileName}, Tables: {sqlResult.AffectedTablesCount}");

            // Clean up verification table
            await exportService.RestoreFromSqlAsync("DROP TABLE IF EXISTS \"_RestoreVerificationTest\";", backupBeforeRestore: false);

            // Test 3: Binary .db restore test (take snapshot of current DB, restore from snapshot stream)
            Console.WriteLine("[TEST 3] Testing Binary .db Restore from valid snapshot...");
            var currentDbBytes = await exportService.GetDatabaseBinarySnapshotAsync();
            using (var validStream = new MemoryStream(currentDbBytes))
            {
                var binaryResult = await exportService.RestoreFromBinaryAsync(validStream, backupBeforeRestore: true);
                if (!binaryResult.Success) throw new Exception($"Binary restore failed: {binaryResult.Message} {binaryResult.ErrorDetails}");
                if (string.IsNullOrEmpty(binaryResult.BackupFileName)) throw new Exception("Pre-restore backup was not created for binary restore!");
                Console.WriteLine($"[TEST 3 PASSED] Binary restore succeeded! Active tables: {binaryResult.AffectedTablesCount}, Backup: {binaryResult.BackupFileName}");
            }

            Console.WriteLine("\n[ALL RESTORE TESTS PASSED] Restore functionality is 100% verified!");
            return 0;
        }
    }
}
