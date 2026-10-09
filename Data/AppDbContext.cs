using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using TrackerKerja.Models;

namespace TrackerKerja.Data
{
    public class AppDbContext : IdentityDbContext<AppUser>
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<Company> Companies { get; set; }
        public DbSet<Project> Projects { get; set; }
        public DbSet<WorkTask> Tasks { get; set; }
        public DbSet<WorkSession> Sessions { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<JsonHistory> JsonHistories { get; set; }
        public DbSet<SqlHistory> SqlHistories { get; set; }
        public DbSet<ImportLog> ImportLogs { get; set; }
        public DbSet<AuditLog> AuditLogs { get; set; }
        public DbSet<WorkNote> Notes { get; set; }
        public DbSet<NoteAttachment> NoteAttachments { get; set; }
        public DbSet<MasterPriority> MasterPriorities { get; set; }
        public DbSet<MasterStatus> MasterStatuses { get; set; }
        public DbSet<MasterMilestone> MasterMilestones { get; set; }
        public DbSet<SystemSetting> SystemSettings { get; set; }
        public DbSet<MasterBadge> MasterBadges { get; set; }
        public DbSet<UserBadge> UserBadges { get; set; }
        public DbSet<AttendanceRecord> Attendances { get; set; }
        public DbSet<EmailTemplate> EmailTemplates { get; set; }
        public DbSet<DailyCheckIn> DailyCheckIns { get; set; }
        public DbSet<RewardItem> RewardItems { get; set; }
        public DbSet<RewardClaim> RewardClaims { get; set; }
        public DbSet<Announcement> Announcements { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Company Relationships
            modelBuilder.Entity<Company>(b =>
            {
                b.HasMany(c => c.Members)
                 .WithOne(u => u.Company)
                 .HasForeignKey(u => u.CompanyId)
                 .OnDelete(DeleteBehavior.SetNull);

                b.HasMany(c => c.Projects)
                 .WithOne(p => p.Company)
                 .HasForeignKey(p => p.CompanyId)
                 .OnDelete(DeleteBehavior.SetNull);

                b.HasMany(c => c.Tasks)
                 .WithOne(t => t.Company)
                 .HasForeignKey(t => t.CompanyId)
                 .OnDelete(DeleteBehavior.SetNull);

                b.HasMany(c => c.Notes)
                 .WithOne(n => n.Company)
                 .HasForeignKey(n => n.CompanyId)
                 .OnDelete(DeleteBehavior.SetNull);
            });

            // Seed Default Company
            modelBuilder.Entity<Company>().HasData(
                new Company
                {
                    Id = 1,
                    Name = "PT Elistec Teknologi",
                    Code = "ELISTEC",
                    Description = "Tim Inti Pengembangan Sistem TrackerKerja",
                    CreatedAt = new DateTime(2026, 8, 19)
                }
            );

            // Seed Categories
            modelBuilder.Entity<Category>().HasData(
                new Category { Id = 1, Name = "Backend", Color = "#6366F1" },
                new Category { Id = 2, Name = "Frontend", Color = "#06B6D4" },
                new Category { Id = 3, Name = "API / REST", Color = "#10B981" },
                new Category { Id = 4, Name = "Database", Color = "#F59E0B" },
                new Category { Id = 5, Name = "DevOps", Color = "#EF4444" },
                new Category { Id = 6, Name = "Testing", Color = "#8B5CF6" }
            );

            // Seed Projects (static dates to avoid migration drift)
            modelBuilder.Entity<Project>().HasData(
                new Project
                {
                    Id = 1,
                    Name = "Work Tracker Pro",
                    Description = "Aplikasi tracker kerja all-in-one",
                    Color = "#6366F1",
                    Status = ProjectStatus.Active,
                    Deadline = new DateTime(2026, 10, 19),
                    CreatedAt = new DateTime(2026, 8, 19)
                },
                new Project
                {
                    Id = 2,
                    Name = "REST API Integration",
                    Description = "Integrasi REST API dengan sistem eksternal",
                    Color = "#10B981",
                    Status = ProjectStatus.Active,
                    Deadline = new DateTime(2026, 9, 19),
                    CreatedAt = new DateTime(2026, 8, 19)
                }
            );

            // Seed Tasks
            modelBuilder.Entity<WorkTask>().HasData(
                new WorkTask
                {
                    Id = 1, ProjectId = 1, CategoryId = 1,
                    Title = "Setup Project ASP.NET Core MVC",
                    Description = "Inisialisasi project dengan EF Core dan SQLite",
                    Priority = TaskPriority.High, Status = Models.TaskStatus.Done,
                    DueDate = new DateTime(2026, 8, 20),
                    CreatedAt = new DateTime(2026, 8, 19), UpdatedAt = new DateTime(2026, 8, 19)
                },
                new WorkTask
                {
                    Id = 2, ProjectId = 1, CategoryId = 2,
                    Title = "Design UI Dashboard",
                    Description = "Membuat tampilan dashboard dengan Tailwind CSS",
                    Priority = TaskPriority.High, Status = Models.TaskStatus.InProgress,
                    DueDate = new DateTime(2026, 8, 22),
                    CreatedAt = new DateTime(2026, 8, 19), UpdatedAt = new DateTime(2026, 8, 19)
                },
                new WorkTask
                {
                    Id = 3, ProjectId = 2, CategoryId = 3,
                    Title = "Analisis endpoint REST API",
                    Description = "Mempelajari dan mendokumentasikan endpoint REST",
                    Priority = TaskPriority.Medium, Status = Models.TaskStatus.Todo,
                    DueDate = new DateTime(2026, 8, 24),
                    CreatedAt = new DateTime(2026, 8, 19), UpdatedAt = new DateTime(2026, 8, 19)
                },
                new WorkTask
                {
                    Id = 4, ProjectId = 2, CategoryId = 3,
                    Title = "Testing JSON Response Format",
                    Description = "Memverifikasi format response JSON dari API",
                    Priority = TaskPriority.Medium, Status = Models.TaskStatus.Todo,
                    DueDate = new DateTime(2026, 8, 26),
                    CreatedAt = new DateTime(2026, 8, 19), UpdatedAt = new DateTime(2026, 8, 19)
                }
            );

            // WorkNote Relationships
            modelBuilder.Entity<WorkNote>(b =>
            {
                b.HasOne(n => n.AuthorUser)
                 .WithMany()
                 .HasForeignKey(n => n.AuthorUserId)
                 .OnDelete(DeleteBehavior.SetNull);

                b.HasOne(n => n.Task)
                 .WithMany(t => t.Notes)
                 .HasForeignKey(n => n.TaskId)
                 .OnDelete(DeleteBehavior.SetNull);
            });

            // NoteAttachment Relationships
            modelBuilder.Entity<NoteAttachment>(b =>
            {
                b.HasOne(a => a.Note)
                 .WithMany(n => n.Attachments)
                 .HasForeignKey(a => a.NoteId)
                 .OnDelete(DeleteBehavior.Cascade);

                b.HasOne(a => a.UploadedByUser)
                 .WithMany()
                 .HasForeignKey(a => a.UploadedByUserId)
                 .OnDelete(DeleteBehavior.SetNull);
            });

            // WorkTask Parent/Child Relationship
            modelBuilder.Entity<WorkTask>(b =>
            {
                b.HasOne(t => t.ParentTask)
                 .WithMany(t => t.ChildTasks)
                 .HasForeignKey(t => t.ParentTaskId)
                 .OnDelete(DeleteBehavior.SetNull);
            });

            // UserBadge Relationships
            modelBuilder.Entity<UserBadge>(b =>
            {
                b.HasOne(ub => ub.User)
                 .WithMany(u => u.UserBadges)
                 .HasForeignKey(ub => ub.UserId)
                 .OnDelete(DeleteBehavior.Cascade);

                b.HasOne(ub => ub.Badge)
                 .WithMany(mb => mb.UserBadges)
                 .HasForeignKey(ub => ub.BadgeId)
                 .OnDelete(DeleteBehavior.Cascade);
            });

            // AttendanceRecord Configuration
            modelBuilder.Entity<AttendanceRecord>(b =>
            {
                b.HasOne(a => a.User)
                 .WithMany()
                 .HasForeignKey(a => a.UserId)
                 .OnDelete(DeleteBehavior.Cascade);

                b.HasOne(a => a.ApprovedByUser)
                 .WithMany()
                 .HasForeignKey(a => a.ApprovedByUserId)
                 .OnDelete(DeleteBehavior.SetNull);

                b.HasIndex(a => new { a.UserId, a.Date });
            });

            // Announcement Configuration
            modelBuilder.Entity<Announcement>(b =>
            {
                b.HasOne(a => a.CreatedByUser)
                 .WithMany()
                 .HasForeignKey(a => a.CreatedByUserId)
                 .OnDelete(DeleteBehavior.SetNull);

                b.HasIndex(a => new { a.IsActive, a.AnnouncementDate, a.EndDate });
            });

            // Seed Master Badges (Gamification & Achievements)
            modelBuilder.Entity<MasterBadge>().HasData(
                new MasterBadge
                {
                    Id = 1,
                    Code = "TASK_FIRST",
                    Name = "Langkah Pertama 🐾",
                    Description = "Selesaikan tugas pertamamu di sistem",
                    Category = "Tasks",
                    Icon = "fa-solid fa-paw",
                    Color = "#10B981",
                    Points = 50,
                    Rarity = BadgeRarity.Common,
                    TriggerType = BadgeTriggerType.Auto_DoneTasks,
                    TriggerThreshold = 1,
                    IsActive = true,
                    OrderIndex = 1,
                    CreatedAt = new DateTime(2026, 8, 19)
                },
                new MasterBadge
                {
                    Id = 2,
                    Code = "TASK_10",
                    Name = "Task Crusher ⚡",
                    Description = "Selesaikan 10 tugas dengan sukses",
                    Category = "Tasks",
                    Icon = "fa-solid fa-bolt",
                    Color = "#F59E0B",
                    Points = 150,
                    Rarity = BadgeRarity.Rare,
                    TriggerType = BadgeTriggerType.Auto_DoneTasks,
                    TriggerThreshold = 10,
                    IsActive = true,
                    OrderIndex = 2,
                    CreatedAt = new DateTime(2026, 8, 19)
                },
                new MasterBadge
                {
                    Id = 3,
                    Code = "TASK_50",
                    Name = "Master Executor ⚔️",
                    Description = "Selesaikan 50 tugas secara produktif",
                    Category = "Tasks",
                    Icon = "fa-solid fa-shield-halved",
                    Color = "#8B5CF6",
                    Points = 400,
                    Rarity = BadgeRarity.Epic,
                    TriggerType = BadgeTriggerType.Auto_DoneTasks,
                    TriggerThreshold = 50,
                    IsActive = true,
                    OrderIndex = 3,
                    CreatedAt = new DateTime(2026, 8, 19)
                },
                new MasterBadge
                {
                    Id = 4,
                    Code = "TASK_100",
                    Name = "Century Hero 🏆",
                    Description = "Menembus pencapaian 100 tugas terselesaikan!",
                    Category = "Tasks",
                    Icon = "fa-solid fa-trophy",
                    Color = "#EAB308",
                    Points = 1000,
                    Rarity = BadgeRarity.Legendary,
                    TriggerType = BadgeTriggerType.Auto_DoneTasks,
                    TriggerThreshold = 100,
                    IsActive = true,
                    OrderIndex = 4,
                    CreatedAt = new DateTime(2026, 8, 19)
                },
                new MasterBadge
                {
                    Id = 5,
                    Code = "WORK_10H",
                    Name = "Fokus Membara 🔥",
                    Description = "Kumpulkan total 10 jam kerja produktif",
                    Category = "Timesheets",
                    Icon = "fa-solid fa-fire-flame-curved",
                    Color = "#F97316",
                    Points = 100,
                    Rarity = BadgeRarity.Common,
                    TriggerType = BadgeTriggerType.Auto_TotalHours,
                    TriggerThreshold = 10,
                    IsActive = true,
                    OrderIndex = 5,
                    CreatedAt = new DateTime(2026, 8, 19)
                },
                new MasterBadge
                {
                    Id = 6,
                    Code = "WORK_50H",
                    Name = "Coffee Fuelled ☕",
                    Description = "Tembus 50 jam dedikasi kerja keras",
                    Category = "Timesheets",
                    Icon = "fa-solid fa-mug-hot",
                    Color = "#EC4899",
                    Points = 300,
                    Rarity = BadgeRarity.Rare,
                    TriggerType = BadgeTriggerType.Auto_TotalHours,
                    TriggerThreshold = 50,
                    IsActive = true,
                    OrderIndex = 6,
                    CreatedAt = new DateTime(2026, 8, 19)
                },
                new MasterBadge
                {
                    Id = 7,
                    Code = "NOTE_FIRST",
                    Name = "Juru Tulis 📜",
                    Description = "Buat catatan kerja/dev log pertama",
                    Category = "Notes",
                    Icon = "fa-solid fa-scroll",
                    Color = "#06B6D4",
                    Points = 50,
                    Rarity = BadgeRarity.Common,
                    TriggerType = BadgeTriggerType.Auto_NotesCount,
                    TriggerThreshold = 1,
                    IsActive = true,
                    OrderIndex = 7,
                    CreatedAt = new DateTime(2026, 8, 19)
                },
                new MasterBadge
                {
                    Id = 8,
                    Code = "NOTE_10",
                    Name = "Knowledge Keeper 🧠",
                    Description = "Bagikan 10 catatan & dokumentasi kerja",
                    Category = "Notes",
                    Icon = "fa-solid fa-brain",
                    Color = "#6366F1",
                    Points = 200,
                    Rarity = BadgeRarity.Rare,
                    TriggerType = BadgeTriggerType.Auto_NotesCount,
                    TriggerThreshold = 10,
                    IsActive = true,
                    OrderIndex = 8,
                    CreatedAt = new DateTime(2026, 8, 19)
                },
                new MasterBadge
                {
                    Id = 9,
                    Code = "ROCKSTAR_DEV",
                    Name = "Rockstar of The Month 🌟",
                    Description = "Penghargaan khusus atas kinerja luar biasa dari Admin",
                    Category = "Special",
                    Icon = "fa-solid fa-star",
                    Color = "#E11D48",
                    Points = 500,
                    Rarity = BadgeRarity.Legendary,
                    TriggerType = BadgeTriggerType.Manual,
                    TriggerThreshold = 1,
                    IsActive = true,
                    OrderIndex = 9,
                    CreatedAt = new DateTime(2026, 8, 19)
                }
            );
        }
    }
}
