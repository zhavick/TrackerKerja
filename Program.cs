using Microsoft.AspNetCore.Identity;
using TrackerKerja.Data;
using TrackerKerja.Models;
using TrackerKerja.Filters;
using TrackerKerja.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// Register Audit Filter
builder.Services.AddScoped<AuditLogActionFilter>();

// Add services to the container with global Audit Filter
builder.Services.AddControllersWithViews(options =>
{
    options.Filters.Add<AuditLogActionFilter>();
});

// Add Swagger / OpenAPI documentation
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Work Tracker Pro REST API",
        Version = "v3.1",
        Description = "Dokumentasi RESTful API lengkap dan interaktif untuk seluruh modul Work Tracker Pro (v3.1): " +
                      "Autentikasi & Akun (Auth), Tugas (Tasks), Proyek (Projects), Catatan & Lampiran (Notes), " +
                      "Timesheet & Multi-Timer Serentak per Pengguna (active-timers), " +
                      "Manajemen Anggota Tim termasuk Admin Password Reset (Members), " +
                      "Ekspor Excel dengan Filter Periode (Standard & ARMS 21-kolom), " +
                      "Laporan Eksekutif & Gantt Chart (Reports), Master Data (Kategori, Prioritas, Status, Milestone SDLC Waterfall), " +
                      "Kalender Acara (Calendar), Import & Ekspor Excel/ARMS, JSON Development Tools, " +
                      "Notifikasi Sistem (Notifications), Dashboard & Background Sync, serta Konfigurasi & Audit Trail.",
        Contact = new OpenApiContact
        {
            Name = "Work Tracker Pro Engineering Team",
            Email = "admin@trackerkerja.com"
        }
    });

    var xmlFilename = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFilename);
    if (File.Exists(xmlPath))
    {
        options.IncludeXmlComments(xmlPath);
    }
});

// Ensure SQLite database directory exists if specified in connection string
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? "Data Source=trackerkerja.db";
if (connectionString.Contains("Data Source=", StringComparison.OrdinalIgnoreCase))
{
    var rawPath = connectionString.Substring(connectionString.IndexOf("Data Source=", StringComparison.OrdinalIgnoreCase) + 12).Split(';')[0].Trim();
    if (!string.IsNullOrEmpty(rawPath))
    {
        var dbDir = Path.GetDirectoryName(rawPath);
        if (!string.IsNullOrEmpty(dbDir) && !Directory.Exists(dbDir))
        {
            Directory.CreateDirectory(dbDir);
        }
    }
}

// Add EF Core with SQLite
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(connectionString));

// Add Gamification Service
builder.Services.AddScoped<IGamificationService, GamificationService>();
builder.Services.AddScoped<IDatabaseExportService, DatabaseExportService>();

// Add session support (for Import preview)
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// Add ASP.NET Core Identity
builder.Services.AddIdentity<AppUser, IdentityRole>(options =>
{
    // Password settings (relaxed for ease of use)
    options.Password.RequireDigit = true;
    options.Password.RequiredLength = 6;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
    options.Password.RequireLowercase = true;

    // Lockout settings
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
    options.Lockout.MaxFailedAccessAttempts = 5;

    // User settings
    options.User.RequireUniqueEmail = true;
    options.SignIn.RequireConfirmedAccount = false;
})
.AddEntityFrameworkStores<AppDbContext>()
.AddDefaultTokenProviders();

// Cookie configuration
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.LogoutPath = "/Account/Logout";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.ExpireTimeSpan = TimeSpan.FromDays(7);
    options.SlidingExpiration = true;
});

var app = builder.Build();

// Ensure upload folders exist
try
{
    var webRoot = app.Environment.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
    var uploadsDir = Path.Combine(webRoot, "uploads");
    Directory.CreateDirectory(Path.Combine(uploadsDir, "notes"));
    Directory.CreateDirectory(Path.Combine(uploadsDir, "avatars"));
}
catch { }

// Auto-migrate and seed database on startup
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();

    try { db.Database.ExecuteSqlRaw("ALTER TABLE Tasks ADD COLUMN Obstacle TEXT;"); } catch { }
    try { db.Database.ExecuteSqlRaw("ALTER TABLE Tasks ADD COLUMN Solution TEXT;"); } catch { }
    try { db.Database.ExecuteSqlRaw("ALTER TABLE Tasks ADD COLUMN Progress INTEGER NOT NULL DEFAULT 0;"); } catch { }
    try { db.Database.ExecuteSqlRaw("ALTER TABLE Tasks ADD COLUMN ParentTaskId INTEGER;"); } catch { }
    try { db.Database.ExecuteSqlRaw("ALTER TABLE Tasks ADD COLUMN Milestone TEXT;"); } catch { }
    try { db.Database.ExecuteSqlRaw("UPDATE Tasks SET Milestone = 'Implementation' WHERE Milestone IS NULL OR Milestone = '';"); } catch { }
    try { db.Database.ExecuteSqlRaw("ALTER TABLE Categories ADD COLUMN Description TEXT;"); } catch { }
    try { db.Database.ExecuteSqlRaw("ALTER TABLE Sessions ADD COLUMN UserId TEXT;"); } catch { }

    try
    {
        db.Database.ExecuteSqlRaw(@"
            CREATE TABLE IF NOT EXISTS MasterMilestones (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Name TEXT NOT NULL,
                Phase TEXT NOT NULL,
                Color TEXT NULL,
                Icon TEXT NULL,
                OrderIndex INTEGER NOT NULL DEFAULT 0,
                Description TEXT NULL,
                IsDefault INTEGER NOT NULL DEFAULT 0
            );");
    } catch { }

    try
    {
        db.Database.ExecuteSqlRaw(@"
            CREATE TABLE IF NOT EXISTS MasterPriorities (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Name TEXT NOT NULL,
                Color TEXT NULL,
                Icon TEXT NULL,
                OrderIndex INTEGER NOT NULL DEFAULT 0,
                Description TEXT NULL,
                IsDefault INTEGER NOT NULL DEFAULT 0
            );");
    } catch { }

    try
    {
        db.Database.ExecuteSqlRaw(@"
            CREATE TABLE IF NOT EXISTS MasterStatuses (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Name TEXT NOT NULL,
                Color TEXT NULL,
                IsDoneState INTEGER NOT NULL DEFAULT 0,
                OrderIndex INTEGER NOT NULL DEFAULT 0,
                Description TEXT NULL,
                IsDefault INTEGER NOT NULL DEFAULT 0
            );");
    } catch { }

    try
    {
        db.Database.ExecuteSqlRaw(@"
            CREATE TABLE IF NOT EXISTS NoteAttachments (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                NoteId INTEGER NOT NULL,
                FileName TEXT NOT NULL,
                FilePath TEXT NOT NULL,
                FileSize INTEGER NOT NULL DEFAULT 0,
                ContentType TEXT NULL,
                FileExtension TEXT NULL,
                UploadedAt TEXT NOT NULL,
                UploadedByUserId TEXT NULL,
                FOREIGN KEY (NoteId) REFERENCES Notes(Id) ON DELETE CASCADE,
                FOREIGN KEY (UploadedByUserId) REFERENCES AspNetUsers(Id) ON DELETE SET NULL
            );");
    } catch { }

    try
    {
        db.Database.ExecuteSqlRaw(@"
            CREATE TABLE IF NOT EXISTS SqlHistories (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Name TEXT NOT NULL,
                Content TEXT NOT NULL,
                Dialect TEXT NULL,
                TaskId INTEGER NULL,
                CreatedAt TEXT NOT NULL,
                FOREIGN KEY (TaskId) REFERENCES Tasks(Id) ON DELETE SET NULL
            );");
    } catch { }

    try
    {
        db.Database.ExecuteSqlRaw(@"
            CREATE TABLE IF NOT EXISTS SystemSettings (
                Key TEXT PRIMARY KEY,
                Value TEXT NOT NULL,
                Description TEXT NULL,
                UpdatedAt TEXT NOT NULL
            );");
    } catch { }

    if (!db.SystemSettings.Any(s => s.Key == "GlobalBaseUrl"))
    {
        db.SystemSettings.Add(new SystemSetting
        {
            Key = "GlobalBaseUrl",
            Value = "http://localhost:5000",
            Description = "Global Base URL untuk integrasi REST API, Swagger, dan Webhook",
            UpdatedAt = DateTime.Now
        });
        db.SaveChanges();
    }

    if (!db.MasterPriorities.Any())
    {
        db.MasterPriorities.AddRange(
            new MasterPriority { Name = "Low", Color = "#10B981", Icon = "fa-flag", OrderIndex = 1, Description = "Prioritas rendah / santai" },
            new MasterPriority { Name = "Medium", Color = "#3B82F6", Icon = "fa-flag", OrderIndex = 2, Description = "Prioritas normal / standar", IsDefault = true },
            new MasterPriority { Name = "High", Color = "#F59E0B", Icon = "fa-flag", OrderIndex = 3, Description = "Prioritas tinggi / penting" },
            new MasterPriority { Name = "Critical", Color = "#EF4444", Icon = "fa-bolt", OrderIndex = 4, Description = "Prioritas kritis / blocker" }
        );
        db.SaveChanges();
    }

    if (!db.MasterStatuses.Any())
    {
        db.MasterStatuses.AddRange(
            new MasterStatus { Name = "Todo", Color = "#64748B", IsDoneState = false, OrderIndex = 1, Description = "Tugas baru / belum dikerjakan", IsDefault = true },
            new MasterStatus { Name = "InProgress", Color = "#6366F1", IsDoneState = false, OrderIndex = 2, Description = "Sedang dalam pengerjaan aktif" },
            new MasterStatus { Name = "Review", Color = "#8B5CF6", IsDoneState = false, OrderIndex = 3, Description = "Sedang ditinjau / code review" },
            new MasterStatus { Name = "Done", Color = "#10B981", IsDoneState = true, OrderIndex = 4, Description = "Tugas selesai dikerjakan" },
            new MasterStatus { Name = "Overdue", Color = "#EF4444", IsDoneState = false, OrderIndex = 5, Description = "Tugas melewati batas deadline" }
        );
        db.SaveChanges();
    }

    if (!db.MasterMilestones.Any())
    {
        db.MasterMilestones.AddRange(
            new MasterMilestone { Name = "Requirement Analysis", Phase = "Requirement Analysis", Color = "#3B82F6", Icon = "fa-clipboard-list", OrderIndex = 1, Description = "Analisis kebutuhan sistem, penyusunan SRS, BRD, dan user stories" },
            new MasterMilestone { Name = "System Design", Phase = "System Design", Color = "#8B5CF6", Icon = "fa-drafting-compass", OrderIndex = 2, Description = "Desain arsitektur sistem, skema database ERD, API spec, dan UI/UX wireframe" },
            new MasterMilestone { Name = "Implementation", Phase = "Implementation", Color = "#6366F1", Icon = "fa-code", OrderIndex = 3, Description = "Pengembangan fitur, coding backend & frontend, REST API, dan integrasi modul", IsDefault = true },
            new MasterMilestone { Name = "Testing & QA", Phase = "Testing & QA", Color = "#F59E0B", Icon = "fa-vial", OrderIndex = 4, Description = "Pengujian sistem, QA test cases, bug fixing, performa, dan User Acceptance Testing (UAT)" },
            new MasterMilestone { Name = "Deployment", Phase = "Deployment", Color = "#10B981", Icon = "fa-rocket", OrderIndex = 5, Description = "Setup server hosting, CI/CD pipeline deployment, migrasi DB, dan go-live production" },
            new MasterMilestone { Name = "Maintenance", Phase = "Maintenance", Color = "#64748B", Icon = "fa-tools", OrderIndex = 6, Description = "Pemeliharaan sistem, monitoring server, penanganan bug pasca rilis, dan patch update" }
        );
        db.SaveChanges();
    }

    try
    {
        db.Database.ExecuteSqlRaw(@"
            CREATE TABLE IF NOT EXISTS MasterBadges (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Code TEXT NOT NULL,
                Name TEXT NOT NULL,
                Description TEXT NULL,
                Category TEXT NULL,
                Icon TEXT NULL,
                Color TEXT NULL,
                Points INTEGER NOT NULL DEFAULT 100,
                Rarity INTEGER NOT NULL DEFAULT 1,
                TriggerType INTEGER NOT NULL DEFAULT 0,
                TriggerThreshold INTEGER NOT NULL DEFAULT 1,
                IsActive INTEGER NOT NULL DEFAULT 1,
                OrderIndex INTEGER NOT NULL DEFAULT 0,
                CreatedAt TEXT NOT NULL
            );");
    } catch { }

    try
    {
        db.Database.ExecuteSqlRaw(@"
            CREATE TABLE IF NOT EXISTS UserBadges (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                UserId TEXT NOT NULL,
                BadgeId INTEGER NOT NULL,
                UnlockedAt TEXT NOT NULL,
                IsFeatured INTEGER NOT NULL DEFAULT 0,
                AwardedBy TEXT NULL,
                FOREIGN KEY (UserId) REFERENCES AspNetUsers(Id) ON DELETE CASCADE,
                FOREIGN KEY (BadgeId) REFERENCES MasterBadges(Id) ON DELETE CASCADE
            );");
    } catch { }

    if (!db.MasterBadges.Any())
    {
        db.MasterBadges.AddRange(
            new MasterBadge { Code = "TASK_FIRST", Name = "Langkah Pertama 🐾", Description = "Selesaikan tugas pertamamu di sistem", Category = "Tasks", Icon = "fa-solid fa-paw", Color = "#10B981", Points = 50, Rarity = BadgeRarity.Common, TriggerType = BadgeTriggerType.Auto_DoneTasks, TriggerThreshold = 1, IsActive = true, OrderIndex = 1, CreatedAt = DateTime.UtcNow },
            new MasterBadge { Code = "TASK_10", Name = "Task Crusher ⚡", Description = "Selesaikan 10 tugas dengan sukses", Category = "Tasks", Icon = "fa-solid fa-bolt", Color = "#F59E0B", Points = 150, Rarity = BadgeRarity.Rare, TriggerType = BadgeTriggerType.Auto_DoneTasks, TriggerThreshold = 10, IsActive = true, OrderIndex = 2, CreatedAt = DateTime.UtcNow },
            new MasterBadge { Code = "TASK_50", Name = "Master Executor ⚔️", Description = "Selesaikan 50 tugas secara produktif", Category = "Tasks", Icon = "fa-solid fa-shield-halved", Color = "#8B5CF6", Points = 400, Rarity = BadgeRarity.Epic, TriggerType = BadgeTriggerType.Auto_DoneTasks, TriggerThreshold = 50, IsActive = true, OrderIndex = 3, CreatedAt = DateTime.UtcNow },
            new MasterBadge { Code = "TASK_100", Name = "Century Hero 🏆", Description = "Menembus pencapaian 100 tugas terselesaikan!", Category = "Tasks", Icon = "fa-solid fa-trophy", Color = "#EAB308", Points = 1000, Rarity = BadgeRarity.Legendary, TriggerType = BadgeTriggerType.Auto_DoneTasks, TriggerThreshold = 100, IsActive = true, OrderIndex = 4, CreatedAt = DateTime.UtcNow },
            new MasterBadge { Code = "WORK_10H", Name = "Fokus Membara 🔥", Description = "Kumpulkan total 10 jam kerja produktif", Category = "Timesheets", Icon = "fa-solid fa-fire-flame-curved", Color = "#F97316", Points = 100, Rarity = BadgeRarity.Common, TriggerType = BadgeTriggerType.Auto_TotalHours, TriggerThreshold = 10, IsActive = true, OrderIndex = 5, CreatedAt = DateTime.UtcNow },
            new MasterBadge { Code = "WORK_50H", Name = "Coffee Fuelled ☕", Description = "Tembus 50 jam dedikasi kerja keras", Category = "Timesheets", Icon = "fa-solid fa-mug-hot", Color = "#EC4899", Points = 300, Rarity = BadgeRarity.Rare, TriggerType = BadgeTriggerType.Auto_TotalHours, TriggerThreshold = 50, IsActive = true, OrderIndex = 6, CreatedAt = DateTime.UtcNow },
            new MasterBadge { Code = "NOTE_FIRST", Name = "Juru Tulis 📜", Description = "Buat catatan kerja/dev log pertama", Category = "Notes", Icon = "fa-solid fa-scroll", Color = "#06B6D4", Points = 50, Rarity = BadgeRarity.Common, TriggerType = BadgeTriggerType.Auto_NotesCount, TriggerThreshold = 1, IsActive = true, OrderIndex = 7, CreatedAt = DateTime.UtcNow },
            new MasterBadge { Code = "NOTE_10", Name = "Knowledge Keeper 🧠", Description = "Bagikan 10 catatan & dokumentasi kerja", Category = "Notes", Icon = "fa-solid fa-brain", Color = "#6366F1", Points = 200, Rarity = BadgeRarity.Rare, TriggerType = BadgeTriggerType.Auto_NotesCount, TriggerThreshold = 10, IsActive = true, OrderIndex = 8, CreatedAt = DateTime.UtcNow },
            new MasterBadge { Code = "ROCKSTAR_DEV", Name = "Rockstar of The Month 🌟", Description = "Penghargaan khusus atas kinerja luar biasa dari Admin", Category = "Special", Icon = "fa-solid fa-star", Color = "#E11D48", Points = 500, Rarity = BadgeRarity.Legendary, TriggerType = BadgeTriggerType.Manual, TriggerThreshold = 1, IsActive = true, OrderIndex = 9, CreatedAt = DateTime.UtcNow }
        );
        db.SaveChanges();
    }

    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

    if (!await roleManager.RoleExistsAsync("Admin"))
        await roleManager.CreateAsync(new IdentityRole("Admin"));
    if (!await roleManager.RoleExistsAsync("User"))
        await roleManager.CreateAsync(new IdentityRole("User"));

    // 1. Seed default admin user
    if (await userManager.FindByEmailAsync("admin@trackerkerja.com") == null)
    {
        var adminUser = new AppUser
        {
            UserName = "admin@trackerkerja.com",
            Email = "admin@trackerkerja.com",
            FullName = "Administrator",
            JobTitle = "System Administrator",
            AvatarColor = "#6366F1",
            CreatedAt = DateTime.Now,
            EmailConfirmed = true
        };
        var result = await userManager.CreateAsync(adminUser, "Admin123!");
        if (result.Succeeded)
            await userManager.AddToRoleAsync(adminUser, "Admin");
    }

    // 2. Seed requested elistec.com team members
    var teamUsers = new (string Email, string Name, string Job, string Color)[]
    {
        ("glenn.hakim@elistec.com", "Glenn Hakim", "Project Lead", "#6366F1"),
        ("heni.rahayu@elistec.com", "Heni Rahayu", "QA & Product Specialist", "#EC4899"),
        ("haviz.indra@elistec.com", "Haviz Indra", "Frontend Developer", "#06B6D4"),
        ("Iqbal.ali@elistec.com", "Iqbal Ali", "Backend Developer", "#10B981"),
        ("mohammad.danang@elistec.com", "Mohammad Danang", "DevOps Engineer", "#F59E0B"),
        ("syafix.said@elistec.com", "Syafix Said", "System Analyst", "#8B5CF6"),
        ("nanda.putri@elistec.com", "Nanda Putri", "Technical Writer", "#0EA5E9"),
        ("athallah.bariq@elistec.com", "Athallah Bariq", "Fullstack Developer", "#3B82F6")
    };

    foreach (var (email, name, job, color) in teamUsers)
    {
        if (await userManager.FindByEmailAsync(email) == null)
        {
            var newUser = new AppUser
            {
                UserName = email,
                Email = email,
                FullName = name,
                JobTitle = job,
                AvatarColor = color,
                CreatedAt = DateTime.Now,
                EmailConfirmed = true
            };
            var res = await userManager.CreateAsync(newUser, "Password123!");
            if (res.Succeeded)
            {
                await userManager.AddToRoleAsync(newUser, "User");
            }
        }
    }

    // 4. Seed sample Notes (both Standalone and Linked to Tasks)
    if (!await db.Notes.AnyAsync())
    {
        var glennUser = await userManager.FindByEmailAsync("glenn.hakim@elistec.com");
        var iqbalUser = await userManager.FindByEmailAsync("Iqbal.ali@elistec.com");
        var heniUser = await userManager.FindByEmailAsync("heni.rahayu@elistec.com");

        var webhookTask = await db.Tasks.FirstOrDefaultAsync(t => t.Title.Contains("Webhook"));
        var uatTask = await db.Tasks.FirstOrDefaultAsync(t => t.Title.Contains("User Acceptance Testing"));

        db.Notes.AddRange(
            new WorkNote
            {
                Title = "Notula Kickoff Meeting & Kesepakatan Sprint Q3",
                Category = "Meeting",
                Color = "#6366F1",
                IsPinned = true,
                AuthorUserId = glennUser?.Id,
                TaskId = null, // Standalone
                ContentHtml = "<h2>Agenda Kickoff Sprint Q3</h2><p>Meeting dihadiri oleh seluruh tim <strong>@elistec.com</strong> untuk menyepakati deliverable utama.</p><h3>Poin Kesepakatan:</h3><ul><li>Modul <strong>ClosedXML Excel Import</strong> harus selesai dalam minggu ini.</li><li>Implementasi <strong>Audit Trail</strong> mencakup seluruh HTTP Controller.</li><li>Setiap tugas harus memiliki PIC penanggung jawab dan estimasi jam kerja.</li></ul><blockquote><em>Target rilis versi 1.2 adalah akhir bulan ini. Pastikan integrasi API berjalan stabil.</em></blockquote>",
                CreatedAt = DateTime.Now.AddDays(-3),
                UpdatedAt = DateTime.Now.AddDays(-1)
            },
            new WorkNote
            {
                Title = "Spesifikasi Endpoint & Format Payload Webhook",
                Category = "Technical",
                Color = "#10B981",
                IsPinned = false,
                AuthorUserId = iqbalUser?.Id,
                TaskId = webhookTask?.Id, // Linked to Webhook Task
                ContentHtml = "<h2>Arsitektur Webhook Service</h2><p>Webhook akan mengirimkan notifikasi event secara asinkronus ke subscriber URL.</p><pre><code>{\n  \"event\": \"task.status_changed\",\n  \"taskId\": 102,\n  \"oldStatus\": \"InProgress\",\n  \"newStatus\": \"Done\",\n  \"timestamp\": \"2026-08-19T14:30:00Z\"\n}</code></pre><p>Header autentikasi wajib menyertakan <code>X-Signature-SHA256</code> untuk verifikasi keaslian payload.</p>",
                CreatedAt = DateTime.Now.AddDays(-2),
                UpdatedAt = DateTime.Now.AddDays(-1)
            },
            new WorkNote
            {
                Title = "Panduan Skenario UAT Import Excel & Validasi Data",
                Category = "Task Note",
                Color = "#EC4899",
                IsPinned = false,
                AuthorUserId = heniUser?.Id,
                TaskId = uatTask?.Id, // Linked to UAT Task
                ContentHtml = "<h2>Skenario Pengujian File Excel</h2><p>Pengujian dilakukan terhadap berbagai variasi format file spreadsheet:</p><ol><li>File template standar dengan 10 baris tugas.</li><li>File dengan baris kosong di tengah data.</li><li>File dengan format tanggal selain YYYY-MM-DD.</li></ol><p>Status hasil: <strong>PASS</strong> pada semua skenario uji utama.</p>",
                CreatedAt = DateTime.Now.AddDays(-1),
                UpdatedAt = DateTime.Now
            }
        );
        await db.SaveChangesAsync();
    }
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

// Enable Swagger & Swagger UI
app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "Work Tracker Pro API v1");
    options.RoutePrefix = "swagger";
    options.DocumentTitle = "Work Tracker Pro - Swagger API Documentation";
    options.DisplayRequestDuration();
});

app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.UseSession();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
