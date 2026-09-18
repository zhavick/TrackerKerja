using Microsoft.AspNetCore.Identity;
using TrackerKerja.Data;
using TrackerKerja.Models;
using TrackerKerja.Filters;
using TrackerKerja.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Security.Claims;

var builder = WebApplication.CreateBuilder(args);

// Register Audit Filter
builder.Services.AddScoped<AuditLogActionFilter>();

// Add services to the container with global Audit Filter
builder.Services.AddControllersWithViews(options =>
{
    options.Filters.Add<AuditLogActionFilter>();
});

// Configure upload & request size limits for sync packages and attachments (up to 200MB)
builder.Services.Configure<Microsoft.AspNetCore.Http.Features.FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 200 * 1024 * 1024; // 200 MB
    options.ValueLengthLimit = int.MaxValue;
    options.MultipartHeadersLengthLimit = int.MaxValue;
});
builder.WebHost.ConfigureKestrel(serverOptions =>
{
    serverOptions.Limits.MaxRequestBodySize = 200 * 1024 * 1024; // 200 MB
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

    // JWT Bearer Authentication definition in Swagger UI
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Description = "Masukkan token JWT dengan format: Bearer {token}",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT"
    });
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
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

// Add HttpClient Factory
builder.Services.AddHttpClient();

// Add Services
builder.Services.AddScoped<IGamificationService, GamificationService>();
builder.Services.AddScoped<IDatabaseExportService, DatabaseExportService>();
builder.Services.AddScoped<IDatabaseSyncService, DatabaseSyncService>();
builder.Services.AddScoped<IExcelSyncService, ExcelSyncService>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IJwtService, JwtService>();

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

// JWT Configuration & Dual-Scheme Authentication
var jwtKey = builder.Configuration["Jwt:Key"] ?? "TrackerKerja-SuperSecretKey-MustBeAtLeast32CharsLong-2026-SecureJWT!";
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "TrackerKerja";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "TrackerKerjaClient";
var keyBytes = Encoding.UTF8.GetBytes(jwtKey);

builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = "JWT_OR_COOKIE";
    options.DefaultAuthenticateScheme = "JWT_OR_COOKIE";
    options.DefaultChallengeScheme = "JWT_OR_COOKIE";
})
.AddPolicyScheme("JWT_OR_COOKIE", "JWT_OR_COOKIE", options =>
{
    options.ForwardDefaultSelector = context =>
    {
        // 1. If Authorization header with Bearer is present -> use JwtBearer
        string? authHeader = context.Request.Headers["Authorization"].FirstOrDefault();
        if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            return JwtBearerDefaults.AuthenticationScheme;
        }

        // 2. If it's an API route or from Swagger -> MUST use JwtBearer (challenges with 401 Unauthorized if missing token)
        if (context.Request.Path.StartsWithSegments("/api") ||
            context.Request.Headers["Referer"].ToString().Contains("/swagger", StringComparison.OrdinalIgnoreCase))
        {
            return JwtBearerDefaults.AuthenticationScheme;
        }

        // 3. For Web MVC routes, if jwt_token cookie is present -> use JwtBearer
        if (context.Request.Cookies.ContainsKey("jwt_token"))
        {
            return JwtBearerDefaults.AuthenticationScheme;
        }

        // 4. Otherwise use Identity Application Cookie
        return IdentityConstants.ApplicationScheme;
    };
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false;
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtIssuer,
        ValidAudience = jwtAudience,
        IssuerSigningKey = new SymmetricSecurityKey(keyBytes),
        ClockSkew = TimeSpan.Zero,
        NameClaimType = ClaimTypes.Name,
        RoleClaimType = ClaimTypes.Role
    };

    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            var isSwagger = context.Request.Headers["Referer"].ToString().Contains("/swagger", StringComparison.OrdinalIgnoreCase);
            var isApi = context.Request.Path.StartsWithSegments("/api");

            var authHeader = context.Request.Headers["Authorization"].FirstOrDefault();
            if (!string.IsNullOrEmpty(authHeader))
            {
                // Handle "Bearer {token}", "Bearer Bearer {token}", or raw token
                if (authHeader.StartsWith("Bearer Bearer ", StringComparison.OrdinalIgnoreCase))
                {
                    context.Token = authHeader.Substring("Bearer Bearer ".Length).Trim();
                }
                else if (authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                {
                    context.Token = authHeader.Substring("Bearer ".Length).Trim();
                }
                else
                {
                    context.Token = authHeader.Trim();
                }
            }
            else
            {
                // For Swagger UI: STRICTLY REQUIRE Authorization Header!
                // Users testing on Swagger UI must click 'Authorize' and input their JWT Bearer token.
                // For regular Web UI calls (AJAX/Fetch from app pages), fall back to jwt_token cookie if present.
                if (!isSwagger)
                {
                    if (context.Request.Cookies.TryGetValue("jwt_token", out var cookieToken) && !string.IsNullOrEmpty(cookieToken))
                    {
                        context.Token = cookieToken;
                    }
                }
            }
            return Task.CompletedTask;
        },
        OnChallenge = async context =>
        {
            var isApi = context.Request.Path.StartsWithSegments("/api");
            var isSwagger = context.Request.Headers["Referer"].ToString().Contains("/swagger", StringComparison.OrdinalIgnoreCase);

            if (isApi || isSwagger || context.Request.Headers.Accept.ToString().Contains("application/json") ||
                context.Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                context.HandleResponse();
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                context.Response.ContentType = "application/json";

                var errorResponse = new
                {
                    success = false,
                    message = "Akses Ditolak: Anda belum melakukan Authorize dengan token JWT. " +
                              "Pada Swagger UI, klik tombol 'Authorize' di kanan atas dan masukkan token JWT Bearer Anda terlebih dahulu.",
                    statusCode = 401
                };

                await context.Response.WriteAsync(System.Text.Json.JsonSerializer.Serialize(errorResponse));
            }
            else
            {
                context.HandleResponse();
                context.Response.Redirect($"/Account/Login?ReturnUrl={Uri.EscapeDataString(context.Request.Path + context.Request.QueryString)}");
            }
        }
    };
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

    // Multi-Tenancy Companies Table & Columns
    try
    {
        db.Database.ExecuteSqlRaw(@"
            CREATE TABLE IF NOT EXISTS Companies (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Name TEXT NOT NULL,
                Code TEXT NULL,
                Description TEXT NULL,
                CreatedAt TEXT NOT NULL
            );");
    } catch { }

    try { db.Database.ExecuteSqlRaw("ALTER TABLE AspNetUsers ADD COLUMN CompanyId INTEGER;"); } catch { }
    try { db.Database.ExecuteSqlRaw("ALTER TABLE AspNetUsers ADD COLUMN IsApproved INTEGER NOT NULL DEFAULT 1;"); } catch { }
    try { db.Database.ExecuteSqlRaw("ALTER TABLE AspNetUsers ADD COLUMN ApprovedAt TEXT NULL;"); } catch { }
    try { db.Database.ExecuteSqlRaw("ALTER TABLE AspNetUsers ADD COLUMN ApprovedByUserId TEXT NULL;"); } catch { }
    try { db.Database.ExecuteSqlRaw("ALTER TABLE AspNetUsers ADD COLUMN RejectionReason TEXT NULL;"); } catch { }
    try { db.Database.ExecuteSqlRaw("ALTER TABLE AspNetUsers ADD COLUMN CoverPictureUrl TEXT NULL;"); } catch { }
    try { db.Database.ExecuteSqlRaw("ALTER TABLE Projects ADD COLUMN CompanyId INTEGER;"); } catch { }
    try { db.Database.ExecuteSqlRaw("ALTER TABLE Tasks ADD COLUMN CompanyId INTEGER;"); } catch { }
    try { db.Database.ExecuteSqlRaw("ALTER TABLE Notes ADD COLUMN CompanyId INTEGER;"); } catch { }

    if (!db.Companies.Any())
    {
        db.Companies.Add(new Company
        {
            Name = "PT Elistec Teknologi",
            Code = "ELISTEC",
            Description = "Tim Inti Pengembangan Sistem TrackerKerja",
            CreatedAt = DateTime.Now
        });
        db.SaveChanges();
    }

    try { db.Database.ExecuteSqlRaw("UPDATE AspNetUsers SET CompanyId = 1 WHERE CompanyId IS NULL;"); } catch { }
    try { db.Database.ExecuteSqlRaw("UPDATE Projects SET CompanyId = 1 WHERE CompanyId IS NULL;"); } catch { }
    try { db.Database.ExecuteSqlRaw("UPDATE Tasks SET CompanyId = 1 WHERE CompanyId IS NULL;"); } catch { }
    try { db.Database.ExecuteSqlRaw("UPDATE Notes SET CompanyId = 1 WHERE CompanyId IS NULL;"); } catch { }

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

    try
    {
        db.Database.ExecuteSqlRaw(@"
            CREATE TABLE IF NOT EXISTS Attendances (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                UserId TEXT NOT NULL,
                Date TEXT NOT NULL,
                Type INTEGER NOT NULL DEFAULT 1,
                WorkLocation INTEGER NOT NULL DEFAULT 1,
                ClockIn TEXT NULL,
                ClockOut TEXT NULL,
                TotalHours REAL NOT NULL DEFAULT 0,
                LeaveReason TEXT NULL,
                Notes TEXT NULL,
                Status INTEGER NOT NULL DEFAULT 1,
                ApprovedByUserId TEXT NULL,
                CreatedAt TEXT NOT NULL,
                UpdatedAt TEXT NOT NULL,
                FOREIGN KEY (UserId) REFERENCES AspNetUsers(Id) ON DELETE CASCADE,
                FOREIGN KEY (ApprovedByUserId) REFERENCES AspNetUsers(Id) ON DELETE SET NULL
            );
            CREATE INDEX IF NOT EXISTS IX_Attendances_UserId_Date ON Attendances (UserId, Date);");
    } catch { }

    // EmailTemplates Table
    try
    {
        db.Database.ExecuteSqlRaw(@"
            CREATE TABLE IF NOT EXISTS EmailTemplates (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                EventCode TEXT NOT NULL,
                EventName TEXT NOT NULL,
                Category TEXT NULL,
                Subject TEXT NOT NULL,
                BodyHtml TEXT NOT NULL,
                AvailableVariables TEXT NULL,
                IsActive INTEGER NOT NULL DEFAULT 1,
                CreatedAt TEXT NOT NULL,
                UpdatedAt TEXT NOT NULL,
                UpdatedByUserId TEXT NULL
            );
            CREATE UNIQUE INDEX IF NOT EXISTS IX_EmailTemplates_EventCode ON EmailTemplates (EventCode);");
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

    if (!db.SystemSettings.Any(s => s.Key == "HostSync_ApiKey"))
    {
        db.SystemSettings.Add(new SystemSetting
        {
            Key = "HostSync_ApiKey",
            Value = "TK-SYNC-SECRET-" + Guid.NewGuid().ToString("N").Substring(0, 12).ToUpper(),
            Description = "Secret API Key untuk autentikasi sinkronisasi antar instance",
            UpdatedAt = DateTime.Now
        });
        db.SaveChanges();
    }

    if (!db.SystemSettings.Any(s => s.Key == "HostSync_TargetUrl"))
    {
        db.SystemSettings.Add(new SystemSetting
        {
            Key = "HostSync_TargetUrl",
            Value = "http://localhost:5000",
            Description = "Target URL Host Induk untuk sinkronisasi API",
            UpdatedAt = DateTime.Now
        });
        db.SaveChanges();
    }

    if (!db.SystemSettings.Any(s => s.Key == "HostSync_Role"))
    {
        db.SystemSettings.Add(new SystemSetting
        {
            Key = "HostSync_Role",
            Value = "child",
            Description = "Peran instance: child (pengisian) atau host (induk)",
            UpdatedAt = DateTime.Now
        });
        db.SaveChanges();
    }

    // Default Email SMTP System Settings
    if (!db.SystemSettings.Any(s => s.Key == "Email_SmtpHost"))
    {
        db.SystemSettings.AddRange(
            new SystemSetting { Key = "Email_SmtpHost", Value = "smtp.gmail.com", Description = "Host / server SMTP untuk pengiriman email", UpdatedAt = DateTime.Now },
            new SystemSetting { Key = "Email_SmtpPort", Value = "587", Description = "Port SMTP (contoh: 587 untuk STARTTLS, 465 untuk SSL)", UpdatedAt = DateTime.Now },
            new SystemSetting { Key = "Email_SenderEmail", Value = "notifications@trackerkerja.com", Description = "Alamat email pengirim default", UpdatedAt = DateTime.Now },
            new SystemSetting { Key = "Email_SenderName", Value = "Work Tracker Pro", Description = "Nama tampilan pengirim (Display Name)", UpdatedAt = DateTime.Now },
            new SystemSetting { Key = "Email_SenderPassword", Value = "", Description = "Password / App Password email pengirim", UpdatedAt = DateTime.Now },
            new SystemSetting { Key = "Email_EnableSsl", Value = "true", Description = "Aktifkan enkripsi SSL / TLS", UpdatedAt = DateTime.Now },
            new SystemSetting { Key = "Email_RequireAuth", Value = "true", Description = "Memerlukan autentikasi username & password", UpdatedAt = DateTime.Now },
            new SystemSetting { Key = "Email_IsEnabled", Value = "true", Description = "Status aktif integrasi pengiriman email", UpdatedAt = DateTime.Now }
        );
        db.SaveChanges();
    }

    // Seed Default Email Templates
    if (!db.EmailTemplates.Any())
    {
        db.EmailTemplates.AddRange(
            new EmailTemplate
            {
                EventCode = "USER_REGISTERED",
                EventName = "Pendaftaran Akun Baru (Menunggu Approval)",
                Category = "Account",
                Subject = "[{AppName}] Pendaftaran Akun Berhasil — Menunggu Persetujuan Administrator",
                BodyHtml = @"<div style=""font-family:'Inter',sans-serif;max-width:600px;margin:0 auto;padding:24px;background:#ffffff;border-radius:16px;border:1px solid #e2e8f0;color:#1e293b;"">
    <div style=""text-align:center;margin-bottom:24px;"">
        <div style=""display:inline-block;background:linear-gradient(135deg,#6366F1,#8B5CF6);color:#fff;padding:10px 18px;border-radius:12px;font-weight:900;font-size:15px;"">🚀 {AppName}</div>
        <h2 style=""font-size:20px;font-weight:800;color:#0f172a;margin-top:16px;margin-bottom:4px;"">Pendaftaran Akun Berhasil!</h2>
        <p style=""font-size:13px;color:#64748b;margin:0;"">Halo <strong>{FullName}</strong>, terima kasih telah mendaftar di sistem.</p>
    </div>
    <div style=""background:#fffbeb;border:1px solid #fef3c7;border-radius:12px;padding:16px;margin-bottom:20px;"">
        <h4 style=""font-size:13px;font-weight:700;color:#92400e;margin-top:0;margin-bottom:8px;"">⏳ Menunggu Persetujuan Administrator</h4>
        <p style=""font-size:12px;color:#b45309;line-height:1.6;margin:0;"">Akun Anda saat ini sedang dalam antrean persetujuan (Admin Approval). Anda akan menerima email konfirmasi begitu Administrator menyetujui akun Anda.</p>
    </div>
    <div style=""background:#f8fafc;border:1px solid #e2e8f0;border-radius:12px;padding:16px;margin-bottom:20px;font-size:12px;"">
        <p style=""margin:4px 0;""><strong>Email Terdaftar:</strong> {Email}</p>
        <p style=""margin:4px 0;""><strong>Jabatan:</strong> {JobTitle}</p>
        <p style=""margin:4px 0;""><strong>Afiliasi Tim / Perusahaan:</strong> {CompanyName}</p>
        <p style=""margin:4px 0;""><strong>Tanggal Pendaftaran:</strong> {CurrentDate} {CurrentTime}</p>
    </div>
    <div style=""text-align:center;margin-top:24px;padding-top:16px;border-top:1px solid #e2e8f0;font-size:11px;color:#94a3b8;"">
        <p style=""margin:0;"">&copy; {CurrentYear} {AppName} &bull; <a href=""{AppUrl}"" style=""color:#6366f1;text-decoration:none;"">{AppUrl}</a></p>
    </div>
</div>",
                AvailableVariables = "{FullName}, {Email}, {JobTitle}, {CompanyName}, {AppName}, {AppUrl}, {ActionUrl}, {CurrentDate}, {CurrentTime}, {CurrentYear}",
                IsActive = true,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            },
            new EmailTemplate
            {
                EventCode = "USER_APPROVED",
                EventName = "Persetujuan Akun (Approval Success)",
                Category = "Account",
                Subject = "[{AppName}] Selamat! Akun Anda Telah Disetujui & Siap Digunakan",
                BodyHtml = @"<div style=""font-family:'Inter',sans-serif;max-width:600px;margin:0 auto;padding:24px;background:#ffffff;border-radius:16px;border:1px solid #e2e8f0;color:#1e293b;"">
    <div style=""text-align:center;margin-bottom:24px;"">
        <div style=""display:inline-block;background:linear-gradient(135deg,#10B981,#059669);color:#fff;padding:10px 18px;border-radius:12px;font-weight:900;font-size:15px;"">🎉 Akun Telah Aktif</div>
        <h2 style=""font-size:20px;font-weight:800;color:#0f172a;margin-top:16px;margin-bottom:4px;"">Selamat Datang di {AppName}!</h2>
        <p style=""font-size:13px;color:#64748b;margin:0;"">Halo <strong>{FullName}</strong>, akun Anda telah disetujui oleh Administrator.</p>
    </div>
    <div style=""background:#ecfdf5;border:1px solid #d1fae5;border-radius:12px;padding:16px;margin-bottom:20px;font-size:12px;color:#065f46;line-height:1.6;"">
        Akun login Anda untuk tim <strong>{CompanyName}</strong> kini telah aktif. Anda dapat masuk dan mulai mengelola tugas, proyek, presensi harian, dan timesheet.
    </div>
    <div style=""text-align:center;margin:24px 0;"">
        <a href=""{ActionUrl}"" style=""display:inline-block;background:linear-gradient(135deg,#6366F1,#4F46E5);color:#ffffff;text-decoration:none;font-size:13px;font-weight:800;padding:12px 28px;border-radius:12px;box-shadow:0 4px 12px rgba(99,102,241,0.3);"">🚀 Masuk ke Portal Sekarang</a>
    </div>
    <div style=""text-align:center;margin-top:24px;padding-top:16px;border-top:1px solid #e2e8f0;font-size:11px;color:#94a3b8;"">
        <p style=""margin:0;"">&copy; {CurrentYear} {AppName} &bull; <a href=""{AppUrl}"" style=""color:#6366f1;text-decoration:none;"">{AppUrl}</a></p>
    </div>
</div>",
                AvailableVariables = "{FullName}, {Email}, {CompanyName}, {AppName}, {AppUrl}, {ActionUrl}, {CurrentDate}, {CurrentYear}",
                IsActive = true,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            },
            new EmailTemplate
            {
                EventCode = "USER_REJECTED",
                EventName = "Penolakan Pendaftaran Akun",
                Category = "Account",
                Subject = "[{AppName}] Pemberitahuan Status Pendaftaran Akun",
                BodyHtml = @"<div style=""font-family:'Inter',sans-serif;max-width:600px;margin:0 auto;padding:24px;background:#ffffff;border-radius:16px;border:1px solid #e2e8f0;color:#1e293b;"">
    <div style=""text-align:center;margin-bottom:24px;"">
        <div style=""display:inline-block;background:#fee2e2;color:#b91c1c;padding:10px 18px;border-radius:12px;font-weight:900;font-size:15px;"">Pemberitahuan Pendaftaran</div>
        <h2 style=""font-size:18px;font-weight:800;color:#0f172a;margin-top:16px;margin-bottom:4px;"">Status Pendaftaran Akun</h2>
        <p style=""font-size:13px;color:#64748b;margin:0;"">Halo <strong>{FullName}</strong> ({Email})</p>
    </div>
    <div style=""background:#fef2f2;border:1px solid #fee2e2;border-radius:12px;padding:16px;margin-bottom:20px;font-size:12px;color:#991b1b;line-height:1.6;"">
        Mohon maaf, pendaftaran akun Anda di <strong>{AppName}</strong> belum dapat disetujui oleh Administrator saat ini.<br/><br/>
        <strong>Catatan Administrator:</strong><br/>
        <em>{RejectionReason}</em>
    </div>
    <div style=""text-align:center;margin-top:24px;padding-top:16px;border-top:1px solid #e2e8f0;font-size:11px;color:#94a3b8;"">
        <p style=""margin:0;"">&copy; {CurrentYear} {AppName} &bull; <a href=""{AppUrl}"" style=""color:#6366f1;text-decoration:none;"">{AppUrl}</a></p>
    </div>
</div>",
                AvailableVariables = "{FullName}, {Email}, {RejectionReason}, {AppName}, {AppUrl}, {CurrentDate}, {CurrentYear}",
                IsActive = true,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            },
            new EmailTemplate
            {
                EventCode = "ADMIN_NEW_USER_ALERT",
                EventName = "Alert Administrator: Pendaftar Baru",
                Category = "Account",
                Subject = "[Admin Alert] Pendaftar Pengguna Baru Menunggu Approval: {FullName}",
                BodyHtml = @"<div style=""font-family:'Inter',sans-serif;max-width:600px;margin:0 auto;padding:24px;background:#ffffff;border-radius:16px;border:1px solid #e2e8f0;color:#1e293b;"">
    <div style=""text-align:center;margin-bottom:20px;"">
        <div style=""display:inline-block;background:linear-gradient(135deg,#F59E0B,#D97706);color:#fff;padding:8px 16px;border-radius:10px;font-weight:900;font-size:14px;"">🛡️ Admin Notification</div>
        <h2 style=""font-size:18px;font-weight:800;color:#0f172a;margin-top:14px;margin-bottom:4px;"">Pendaftar Baru Memerlukan Persetujuan</h2>
        <p style=""font-size:12px;color:#64748b;margin:0;"">Terdapat akun pengguna baru yang baru saja mendaftar secara mandiri.</p>
    </div>
    <div style=""background:#f8fafc;border:1px solid #e2e8f0;border-radius:12px;padding:16px;margin-bottom:20px;font-size:12px;"">
        <p style=""margin:4px 0;""><strong>Nama Lengkap:</strong> {FullName}</p>
        <p style=""margin:4px 0;""><strong>Email:</strong> {Email}</p>
        <p style=""margin:4px 0;""><strong>Jabatan:</strong> {JobTitle}</p>
        <p style=""margin:4px 0;""><strong>Afiliasi Tim:</strong> {CompanyName}</p>
        <p style=""margin:4px 0;""><strong>Waktu:</strong> {CurrentDate} {CurrentTime}</p>
    </div>
    <div style=""text-align:center;margin:20px 0;"">
        <a href=""{ActionUrl}"" style=""display:inline-block;background:#4F46E5;color:#ffffff;text-decoration:none;font-size:12px;font-weight:800;padding:10px 24px;border-radius:10px;"">Tinjau &amp; Setujui Member di Direktori &rarr;</a>
    </div>
    <div style=""text-align:center;margin-top:20px;padding-top:14px;border-top:1px solid #e2e8f0;font-size:11px;color:#94a3b8;"">
        <p style=""margin:0;"">&copy; {CurrentYear} {AppName} Administrator Security Hub</p>
    </div>
</div>",
                AvailableVariables = "{FullName}, {Email}, {JobTitle}, {CompanyName}, {AppName}, {AppUrl}, {ActionUrl}, {CurrentDate}, {CurrentTime}, {CurrentYear}",
                IsActive = true,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            },
            new EmailTemplate
            {
                EventCode = "TASK_ASSIGNED",
                EventName = "Penugasan Tugas Baru (Task Assigned)",
                Category = "Tasks",
                Subject = "[Tugas Baru] {TaskCode}: {TaskTitle}",
                BodyHtml = @"<div style=""font-family:'Inter',sans-serif;max-width:600px;margin:0 auto;padding:24px;background:#ffffff;border-radius:16px;border:1px solid #e2e8f0;color:#1e293b;"">
    <div style=""text-align:center;margin-bottom:20px;"">
        <div style=""display:inline-block;background:linear-gradient(135deg,#6366F1,#8B5CF6);color:#fff;padding:8px 16px;border-radius:10px;font-weight:900;font-size:14px;"">📋 Penugasan Tugas Baru</div>
        <h2 style=""font-size:18px;font-weight:800;color:#0f172a;margin-top:14px;margin-bottom:4px;"">{TaskTitle}</h2>
        <p style=""font-size:12px;color:#64748b;margin:0;"">Halo <strong>{AssigneeName}</strong>, Anda telah ditugaskan pada tugas berikut.</p>
    </div>
    <div style=""background:#f8fafc;border:1px solid #e2e8f0;border-radius:12px;padding:16px;margin-bottom:20px;font-size:12px;"">
        <p style=""margin:4px 0;""><strong>Kode Tugas:</strong> <span style=""font-family:monospace;background:#e0e7ff;color:#3730a3;padding:2px 6px;border-radius:4px;"">{TaskCode}</span></p>
        <p style=""margin:4px 0;""><strong>Proyek:</strong> {ProjectName}</p>
        <p style=""margin:4px 0;""><strong>Prioritas:</strong> {Priority}</p>
        <p style=""margin:4px 0;""><strong>Milestone SDLC:</strong> {Milestone}</p>
        <p style=""margin:4px 0;""><strong>Batas Waktu (Deadline):</strong> {DueDate}</p>
        <p style=""margin:4px 0;""><strong>Diberikan Oleh:</strong> {CreatedByName}</p>
    </div>
    <div style=""text-align:center;margin:20px 0;"">
        <a href=""{ActionUrl}"" style=""display:inline-block;background:#4F46E5;color:#ffffff;text-decoration:none;font-size:12px;font-weight:800;padding:10px 24px;border-radius:10px;"">Lihat Detail Tugas &amp; Mulai Timer &rarr;</a>
    </div>
    <div style=""text-align:center;margin-top:20px;padding-top:14px;border-top:1px solid #e2e8f0;font-size:11px;color:#94a3b8;"">
        <p style=""margin:0;"">&copy; {CurrentYear} {AppName} &bull; <a href=""{AppUrl}"" style=""color:#6366f1;text-decoration:none;"">{AppUrl}</a></p>
    </div>
</div>",
                AvailableVariables = "{TaskCode}, {TaskTitle}, {ProjectName}, {Priority}, {Milestone}, {DueDate}, {AssigneeName}, {CreatedByName}, {AppName}, {AppUrl}, {ActionUrl}, {CurrentDate}, {CurrentYear}",
                IsActive = true,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            },
            new EmailTemplate
            {
                EventCode = "TASK_STATUS_CHANGED",
                EventName = "Perubahan Status Tugas",
                Category = "Tasks",
                Subject = "[Update Tugas] {TaskCode} Diperbarui Menjadi {Status}",
                BodyHtml = @"<div style=""font-family:'Inter',sans-serif;max-width:600px;margin:0 auto;padding:24px;background:#ffffff;border-radius:16px;border:1px solid #e2e8f0;color:#1e293b;"">
    <div style=""text-align:center;margin-bottom:20px;"">
        <div style=""display:inline-block;background:#e0e7ff;color:#3730a3;padding:8px 16px;border-radius:10px;font-weight:900;font-size:14px;"">🔄 Perubahan Status Tugas</div>
        <h2 style=""font-size:18px;font-weight:800;color:#0f172a;margin-top:14px;margin-bottom:4px;"">{TaskTitle}</h2>
        <p style=""font-size:12px;color:#64748b;margin:0;"">Status tugas <strong style=""font-family:monospace;"">{TaskCode}</strong> telah diperbarui menjadi <strong>{Status}</strong>.</p>
    </div>
    <div style=""text-align:center;margin:20px 0;"">
        <a href=""{ActionUrl}"" style=""display:inline-block;background:#4F46E5;color:#ffffff;text-decoration:none;font-size:12px;font-weight:800;padding:10px 24px;border-radius:10px;"">Buka Detail Tugas &rarr;</a>
    </div>
    <div style=""text-align:center;margin-top:20px;padding-top:14px;border-top:1px solid #e2e8f0;font-size:11px;color:#94a3b8;"">
        <p style=""margin:0;"">&copy; {CurrentYear} {AppName} &bull; <a href=""{AppUrl}"" style=""color:#6366f1;text-decoration:none;"">{AppUrl}</a></p>
    </div>
</div>",
                AvailableVariables = "{TaskCode}, {TaskTitle}, {Status}, {ProjectName}, {AssigneeName}, {AppName}, {AppUrl}, {ActionUrl}, {CurrentDate}, {CurrentYear}",
                IsActive = true,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            },
            new EmailTemplate
            {
                EventCode = "PASSWORD_RESET_NOTIFICATION",
                EventName = "Pemberitahuan Reset Password Akun",
                Category = "Account",
                Subject = "[{AppName}] Kata Sandi Akun Anda Berhasil Diperbarui",
                BodyHtml = @"<div style=""font-family:'Inter',sans-serif;max-width:600px;margin:0 auto;padding:24px;background:#ffffff;border-radius:16px;border:1px solid #e2e8f0;color:#1e293b;"">
    <div style=""text-align:center;margin-bottom:20px;"">
        <div style=""display:inline-block;background:#fef3c7;color:#92400e;padding:8px 16px;border-radius:10px;font-weight:900;font-size:14px;"">🔑 Keamanan Akun</div>
        <h2 style=""font-size:18px;font-weight:800;color:#0f172a;margin-top:14px;margin-bottom:4px;"">Kata Sandi Baru Telah Disetel</h2>
        <p style=""font-size:12px;color:#64748b;margin:0;"">Halo <strong>{FullName}</strong>, kata sandi login Anda telah diperbarui oleh Administrator.</p>
    </div>
    <div style=""background:#f8fafc;border:1px solid #e2e8f0;border-radius:12px;padding:16px;margin-bottom:20px;font-size:12px;"">
        <p style=""margin:4px 0;""><strong>Email Login:</strong> {Email}</p>
        <p style=""margin:4px 0;""><strong>Kata Sandi Baru:</strong> <span style=""font-family:monospace;font-weight:bold;color:#4f46e5;background:#eef2ff;padding:2px 8px;border-radius:6px;"">{NewPassword}</span></p>
    </div>
    <div style=""text-align:center;margin:20px 0;"">
        <a href=""{ActionUrl}"" style=""display:inline-block;background:#4F46E5;color:#ffffff;text-decoration:none;font-size:12px;font-weight:800;padding:10px 24px;border-radius:10px;"">Masuk dan Ubah Kata Sandi &rarr;</a>
    </div>
    <div style=""text-align:center;margin-top:20px;padding-top:14px;border-top:1px solid #e2e8f0;font-size:11px;color:#94a3b8;"">
        <p style=""margin:0;"">&copy; {CurrentYear} {AppName} &bull; <a href=""{AppUrl}"" style=""color:#6366f1;text-decoration:none;"">{AppUrl}</a></p>
    </div>
</div>",
                AvailableVariables = "{FullName}, {Email}, {NewPassword}, {AppName}, {AppUrl}, {ActionUrl}, {CurrentDate}, {CurrentYear}",
                IsActive = true,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            }
        );
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
            EmailConfirmed = true,
            IsApproved = true,
            ApprovedAt = DateTime.Now
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
                CompanyId = 1,
                CreatedAt = DateTime.Now,
                EmailConfirmed = true,
                IsApproved = true,
                ApprovedAt = DateTime.Now
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

    // 5. Seed sample Attendances & Leaves
    if (!await db.Attendances.AnyAsync())
    {
        var glennUser = await userManager.FindByEmailAsync("glenn.hakim@elistec.com");
        var adminUser = await userManager.FindByEmailAsync("admin@trackerkerja.com");
        var targetUser = glennUser ?? adminUser;

        if (targetUser != null)
        {
            var today = DateTime.Today;
            // Monday of this week
            int diff = (7 + (today.DayOfWeek - DayOfWeek.Monday)) % 7;
            var mon = today.AddDays(-1 * diff).Date;

            db.Attendances.AddRange(
                new AttendanceRecord
                {
                    UserId = targetUser.Id,
                    Date = mon,
                    Type = AttendanceType.Present,
                    WorkLocation = WorkLocationType.WFO,
                    ClockIn = mon.AddHours(8).AddMinutes(15),
                    ClockOut = mon.AddHours(17).AddMinutes(20),
                    TotalHours = 9.08,
                    Notes = "Hadir tepat waktu di kantor pusat",
                    Status = AttendanceApprovalStatus.Approved,
                    CreatedAt = mon,
                    UpdatedAt = mon
                },
                new AttendanceRecord
                {
                    UserId = targetUser.Id,
                    Date = mon.AddDays(1),
                    Type = AttendanceType.Present,
                    WorkLocation = WorkLocationType.WFH,
                    ClockIn = mon.AddDays(1).AddHours(8).AddMinutes(30),
                    ClockOut = mon.AddDays(1).AddHours(17).AddMinutes(30),
                    TotalHours = 9.0,
                    Notes = "Work From Home - Sprint backlog refinement",
                    Status = AttendanceApprovalStatus.Approved,
                    CreatedAt = mon.AddDays(1),
                    UpdatedAt = mon.AddDays(1)
                },
                new AttendanceRecord
                {
                    UserId = targetUser.Id,
                    Date = mon.AddDays(2),
                    Type = AttendanceType.Present,
                    WorkLocation = WorkLocationType.WFO,
                    ClockIn = mon.AddDays(2).AddHours(8).AddMinutes(20),
                    ClockOut = mon.AddDays(2).AddHours(17).AddMinutes(35),
                    TotalHours = 9.25,
                    Notes = "Meeting koordinasi arsitektur database",
                    Status = AttendanceApprovalStatus.Approved,
                    CreatedAt = mon.AddDays(2),
                    UpdatedAt = mon.AddDays(2)
                },
                new AttendanceRecord
                {
                    UserId = targetUser.Id,
                    Date = mon.AddDays(3),
                    Type = AttendanceType.Leave,
                    WorkLocation = WorkLocationType.WFO,
                    ClockIn = null,
                    ClockOut = null,
                    TotalHours = 0,
                    LeaveReason = "Cuti Tahunan",
                    Notes = "Keperluan keluarga di luar kota (disetujui)",
                    Status = AttendanceApprovalStatus.Approved,
                    CreatedAt = mon.AddDays(3),
                    UpdatedAt = mon.AddDays(3)
                },
                new AttendanceRecord
                {
                    UserId = targetUser.Id,
                    Date = mon.AddDays(4),
                    Type = AttendanceType.Present,
                    WorkLocation = WorkLocationType.WFO,
                    ClockIn = mon.AddDays(4).AddHours(8).AddMinutes(25),
                    ClockOut = null, // sedang berlangsung
                    TotalHours = 0,
                    Notes = "Hari ini di kantor",
                    Status = AttendanceApprovalStatus.Approved,
                    CreatedAt = mon.AddDays(4),
                    UpdatedAt = mon.AddDays(4)
                }
            );
            await db.SaveChangesAsync();
        }
    }
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

var staticFileContentTypeProvider = new Microsoft.AspNetCore.StaticFiles.FileExtensionContentTypeProvider();
staticFileContentTypeProvider.Mappings[".lottie"] = "application/zip";
app.UseStaticFiles(new StaticFileOptions
{
    ContentTypeProvider = staticFileContentTypeProvider
});

// Enable Swagger & Swagger UI
app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "Work Tracker Pro API v1");
    options.RoutePrefix = "swagger";
    options.DocumentTitle = "Work Tracker Pro - Swagger API Documentation";
    options.DisplayRequestDuration();
    options.EnablePersistAuthorization();
});

app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.UseSession();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

if (args.Contains("--run-sync-tests"))
{
    var exitCode = await TrackerKerja.Tests.SyncTestRunner.RunAllTestsAsync();
    Environment.Exit(exitCode);
}

app.Run();
