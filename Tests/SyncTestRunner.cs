using System;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using TrackerKerja.Data;
using TrackerKerja.Services;

namespace TrackerKerja.Tests
{
    public class MockEnvironment : IWebHostEnvironment
    {
        public string WebRootPath { get; set; } = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
        public IFileProvider WebRootFileProvider { get; set; } = null!;
        public string ApplicationName { get; set; } = "TrackerKerja";
        public IFileProvider ContentRootFileProvider { get; set; } = null!;
        public string ContentRootPath { get; set; } = Directory.GetCurrentDirectory();
        public string EnvironmentName { get; set; } = "Development";
    }

    public static class SyncTestRunner
    {
        public static async Task<int> RunAllTestsAsync()
        {
            Console.WriteLine("==========================================================");
            Console.WriteLine("RUNNING COMPREHENSIVE FILE & DATABASE SYNC TESTS");
            Console.WriteLine("==========================================================");

            // 1. Setup sample files in wwwroot/uploads
            var testNoteDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "notes", "user_test");
            var testAvatarDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "avatars");
            Directory.CreateDirectory(testNoteDir);
            Directory.CreateDirectory(testAvatarDir);

            var noteFile = Path.Combine(testNoteDir, "meeting_attachment.pdf");
            var avatarFile = Path.Combine(testAvatarDir, "avatar_test.png");
            await File.WriteAllTextAsync(noteFile, "Sample note meeting PDF attachment content.");
            await File.WriteAllTextAsync(avatarFile, "Sample avatar image binary PNG data.");

            // 2. Setup DI
            var services = new ServiceCollection();
            services.AddDbContext<AppDbContext>(opt => opt.UseSqlite("Data Source=trackerkerja.db"));
            services.AddHttpClient();
            services.AddSingleton<IWebHostEnvironment>(new MockEnvironment());
            services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
            services.AddScoped<IDatabaseExportService, DatabaseExportService>();
            services.AddScoped<IDatabaseSyncService, DatabaseSyncService>();

            var sp = services.BuildServiceProvider();
            using var scope = sp.CreateScope();
            var syncService = scope.ServiceProvider.GetRequiredService<IDatabaseSyncService>();

            // Test 1: Get Uploads Stats
            var (fileCount, totalBytes, formattedSize) = await syncService.GetUploadsStatsAsync();
            Console.WriteLine($"[TEST 1] Uploads Stats: {fileCount} files, {totalBytes} bytes ({formattedSize})");
            if (fileCount < 2) throw new Exception("Expected at least 2 files in uploads.");

            // Test 2: Create Uploads Zip
            var zipBytes = await syncService.CreateUploadsZipArchiveAsync();
            Console.WriteLine($"[TEST 2] Created Uploads Zip: {zipBytes.Length} bytes.");
            if (zipBytes.Length == 0) throw new Exception("Zip bytes cannot be empty.");

            // Test 3: Extract Uploads Zip
            var (extractedCount, extractedBytes) = await syncService.ExtractUploadsZipArchiveAsync(zipBytes);
            Console.WriteLine($"[TEST 3] Extracted Archive: {extractedCount} files, {extractedBytes} bytes.");
            if (extractedCount < 2) throw new Exception("Expected at least 2 files extracted.");

            // Test 4: Full Package Zip (.zip)
            var fullPackage = await syncService.GenerateFullSyncPackageZipAsync(cleanBeforeSync: false);
            Console.WriteLine($"[TEST 4] Full Sync Package (.zip): {fullPackage.Length} bytes.");
            using (var mem = new MemoryStream(fullPackage))
            using (var zip = new ZipArchive(mem, ZipArchiveMode.Read))
            {
                Console.WriteLine($"[TEST 4] Package Entries ({zip.Entries.Count} items):");
                foreach (var entry in zip.Entries)
                {
                    Console.WriteLine($"         - {entry.FullName} ({entry.Length} bytes)");
                }
                if (zip.GetEntry("sync_data.sql") == null) throw new Exception("sync_data.sql missing in package.");
                if (zip.GetEntry("manifest.json") == null) throw new Exception("manifest.json missing in package.");
            }

            // Test 5: Build Sync Payload with Base64 files
            var payload = await syncService.BuildSyncPayloadAsync("ChildTestNode", includeFiles: true);
            Console.WriteLine($"[TEST 5] Sync Payload: SQL={payload.SqlScript.Length} chars, FilesCount={payload.FilesCount}, ZipBase64={payload.FilesZipBase64?.Length ?? 0} chars");
            if (string.IsNullOrEmpty(payload.FilesZipBase64)) throw new Exception("FilesZipBase64 must not be empty.");

            // Test 6: Execute Package Sync from Stream
            using (var pkgStream = new MemoryStream(fullPackage))
            {
                var execResult = await syncService.ExecutePackageSyncAsync(pkgStream, cleanBeforeSync: false, backupBeforeSync: false, "TestPackageImport");
                Console.WriteLine($"[TEST 6] Execute Package Sync: Success={execResult.Success}, Message='{execResult.Message}', SyncedFiles={execResult.SyncedFilesCount}");
                if (!execResult.Success) throw new Exception($"Package sync execution failed: {execResult.Message}");
            }

            Console.WriteLine("\n[ALL TESTS PASSED SUCCESSFULLY] 100% Verified!");
            return 0;
        }
    }
}
