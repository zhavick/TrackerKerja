using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TrackerKerja.Data;
using TrackerKerja.Models;
using TrackerKerja.Services;

namespace TrackerKerja.Tests
{
    public static class SyifaImportTestRunner
    {
        public static async Task<int> RunAllTestsAsync()
        {
            Console.WriteLine("==========================================================");
            Console.WriteLine("RUNNING SYIFA & PROJECT IMPORT VERIFICATION TESTS");
            Console.WriteLine("==========================================================");

            var services = new ServiceCollection();
            services.AddDbContext<AppDbContext>(opt => opt.UseSqlite("Data Source=trackerkerja.db"));
            services.AddHttpClient();
            services.AddLogging(b => b.AddConsole());
            services.AddSingleton<IWebHostEnvironment>(new MockEnvironment());
            services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
            services.AddIdentity<AppUser, IdentityRole>()
                .AddEntityFrameworkStores<AppDbContext>()
                .AddDefaultTokenProviders();
            services.AddScoped<IExcelSyncService, ExcelSyncService>();

            var sp = services.BuildServiceProvider();
            using var scope = sp.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var syncService = scope.ServiceProvider.GetRequiredService<IExcelSyncService>();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();

            var users = await db.Users.ToListAsync();
            var syifaUser = users.FirstOrDefault(u => u.Email != null && u.Email.Contains("syifa", StringComparison.OrdinalIgnoreCase));
            if (syifaUser == null)
            {
                Console.WriteLine("[SETUP] Creating Syifa user in database...");
                syifaUser = new AppUser
                {
                    UserName = "syifa.kurnia@elistec.com",
                    Email = "syifa.kurnia@elistec.com",
                    FullName = "Syifa Kurnia Wijaya",
                    JobTitle = "Quality Assurance",
                    AvatarColor = "#14B8A6",
                    CompanyId = 1,
                    CreatedAt = DateTime.Now,
                    EmailConfirmed = true,
                    IsApproved = true,
                    ApprovedAt = DateTime.Now
                };
                await userManager.CreateAsync(syifaUser, "Password123!");
                await userManager.AddToRoleAsync(syifaUser, "User");
                users.Add(syifaUser);
            }
            Console.WriteLine($"[TEST 1] Syifa User Found: {syifaUser.FullName} ({syifaUser.Email}, ID: {syifaUser.Id})");

            // Test 1: Test ExtractPersonFromSheetName
            var extracted = ExcelSyncService.ExtractPersonFromSheetName("Syifa", users);
            Console.WriteLine($"[TEST 2] ExtractPersonFromSheetName('Syifa'): '{extracted}'");
            if (string.IsNullOrEmpty(extracted) || (!extracted.Contains("Syifa", StringComparison.OrdinalIgnoreCase)))
            {
                throw new Exception($"Expected ExtractPersonFromSheetName to recognize Syifa, got '{extracted}'");
            }

            // Test 2: Test ResolveAppUser
            var resolved = ExcelSyncService.ResolveAppUser("syifa", users);
            Console.WriteLine($"[TEST 3] ResolveAppUser('syifa'): '{resolved?.FullName}'");
            if (resolved == null || resolved.Id != syifaUser.Id)
            {
                throw new Exception($"Expected ResolveAppUser to resolve to Syifa, got '{resolved?.FullName}'");
            }

            // Test 3: Test local file path resolution
            var localPath = syncService.GetDefaultLocalSyncPath();
            Console.WriteLine($"[TEST 4] GetDefaultLocalSyncPath(): '{localPath}' (Exists: {File.Exists(localPath)})");
            if (!File.Exists(localPath))
            {
                throw new Exception($"Local sync path does not exist: {localPath}");
            }

            // Test 4: Parse Excel file
            Console.WriteLine($"[TEST 5] Parsing Excel file: {localPath}...");
            var previewResult = await syncService.ParseFromLocalPathAsync(localPath);
            Console.WriteLine($"[TEST 5] Parsed Total Rows: {previewResult.TotalRows}, Sheets: {string.Join(", ", previewResult.ProcessedSheets)}");

            var syifaRows = previewResult.Rows.Where(r => r.SheetName.Equals("Syifa", StringComparison.OrdinalIgnoreCase)).ToList();
            Console.WriteLine($"[TEST 6] Rows in Syifa Sheet: {syifaRows.Count}");
            if (!syifaRows.Any())
            {
                throw new Exception("No rows found in Syifa sheet!");
            }

            foreach (var row in syifaRows)
            {
                Console.WriteLine($"   - Task: '{row.Title}' | Project: '{row.Project}' | Assignee: '{row.Assignee}' (ID: {row.AssigneeUserId}) | Valid: {row.IsValid}");
                if (row.AssigneeUserId != syifaUser.Id)
                {
                    throw new Exception($"Expected task '{row.Title}' to be assigned to Syifa ({syifaUser.Id}), but got '{row.Assignee}' ({row.AssigneeUserId})");
                }
            }

            // Test 5: Verify Syafix has tasks across multiple projects
            var syafixRows = previewResult.Rows.Where(r => r.SheetName.Equals("Syafix", StringComparison.OrdinalIgnoreCase)).ToList();
            var syafixProjects = syafixRows.Select(r => r.Project).Distinct().ToList();
            Console.WriteLine($"[TEST 7] Syafix Sheet Row Count: {syafixRows.Count}, Distinct Projects: {string.Join(", ", syafixProjects)}");
            if (syafixProjects.Count <= 1)
            {
                throw new Exception("Expected Syafix to work across multiple projects!");
            }

            // Test 6: Execute Sync
            Console.WriteLine("[TEST 8] Executing sync into database...");
            var syncExec = await syncService.ExecuteSyncAsync(previewResult, null, "SyifaSyncTest");
            Console.WriteLine($"[TEST 8] Sync Execution Result: Success={syncExec.Success}, Message='{syncExec.Message}', Inserted={syncExec.InsertedCount}, Updated={syncExec.UpdatedCount}");
            if (!syncExec.Success)
            {
                throw new Exception($"Sync execution failed: {syncExec.Message}");
            }

            // Test 7: Verify database tasks for Syifa
            var syifaDbTasks = await db.Tasks.Where(t => t.AssignedToUserId == syifaUser.Id).ToListAsync();
            Console.WriteLine($"[TEST 9] Database Tasks for Syifa: {syifaDbTasks.Count} tasks");
            if (syifaDbTasks.Count < syifaRows.Count)
            {
                throw new Exception($"Expected at least {syifaRows.Count} tasks for Syifa in DB, found {syifaDbTasks.Count}");
            }
            foreach (var t in syifaDbTasks)
            {
                Console.WriteLine($"   - DB Task: ID={t.Id}, Title='{t.Title}', Status={t.Status}, ProjectId={t.ProjectId}");
            }

            Console.WriteLine("\n[ALL SYIFA IMPORT & PROJECT TESTS PASSED SUCCESSFULLY] 100% Verified!");
            return 0;
        }
    }
}
