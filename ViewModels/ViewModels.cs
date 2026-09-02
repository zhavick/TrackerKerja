using TrackerKerja.Models;

namespace TrackerKerja.ViewModels
{
    public class DashboardViewModel
    {
        public int TotalTasks { get; set; }
        public int DoneTasks { get; set; }
        public int PendingTasks { get; set; }
        public int OverdueTasks { get; set; }
        public int InProgressTasks { get; set; }
        public int TotalProjects { get; set; }
        public long TodayWorkSeconds { get; set; }

        public string TodayWorkFormatted
        {
            get
            {
                var h = TodayWorkSeconds / 3600;
                var m = (TodayWorkSeconds % 3600) / 60;
                return $"{h}j {m}m";
            }
        }

        public List<WorkTask> TodayTasks { get; set; } = new();
        public List<WorkTask> OverdueTaskList { get; set; } = new();
        public List<Project> ActiveProjects { get; set; } = new();
        public WorkSession? RunningSession { get; set; }

        // Personal Member Stats (for the current logged in user)
        public bool IsAdmin { get; set; }
        public string CurrentUserId { get; set; } = string.Empty;
        public string CurrentUserName { get; set; } = string.Empty;
        public string CurrentUserEmail { get; set; } = string.Empty;
        public int MyTotalTasks { get; set; }
        public int MyDoneTasks { get; set; }
        public int MyInProgressTasks { get; set; }
        public int MyTodoTasks { get; set; }
        public int MyOverdueTasks { get; set; }
        public long MyTodayWorkSeconds { get; set; }
        public string MyTodayWorkFormatted
        {
            get
            {
                var h = MyTodayWorkSeconds / 3600;
                var m = (MyTodayWorkSeconds % 3600) / 60;
                return $"{h}j {m}m";
            }
        }
        public List<WorkTask> MyTasks { get; set; } = new();
        public List<WorkNote> MyRecentNotes { get; set; } = new();

        // Chart data
        public List<string> WeekLabels { get; set; } = new();
        public List<long> WeekHours { get; set; } = new();

        // Status Distribution Chart
        public List<string> StatusChartLabels { get; set; } = new();
        public List<int> StatusChartCounts { get; set; } = new();

        // Project Task Distribution Chart
        public List<string> ProjectChartLabels { get; set; } = new();
        public List<int> ProjectChartTodo { get; set; } = new();
        public List<int> ProjectChartInProgress { get; set; } = new();
        public List<int> ProjectChartDone { get; set; } = new();

        // Member Workload Distribution Chart (Task per Member)
        public List<string> MemberChartLabels { get; set; } = new();
        public List<int> MemberChartTodo { get; set; } = new();
        public List<int> MemberChartInProgress { get; set; } = new();
        public List<int> MemberChartDone { get; set; } = new();
        public List<double> MemberChartHours { get; set; } = new();

        // Project-Member Matrix & Project List for Filter
        public List<Project> AllProjects { get; set; } = new();
        public List<ProjectMemberDistributionDto> ProjectMemberDistributions { get; set; } = new();
    }

    public class ProjectMemberDistributionDto
    {
        public int ProjectId { get; set; }
        public string ProjectName { get; set; } = string.Empty;
        public string MemberName { get; set; } = string.Empty;
        public string MemberAvatar { get; set; } = string.Empty;
        public string MemberColor { get; set; } = "#6366F1";
        public int TodoCount { get; set; }
        public int InProgressCount { get; set; }
        public int DoneCount { get; set; }
        public int TotalCount => TodoCount + InProgressCount + DoneCount;
        public double LoggedHours { get; set; }
    }

    public class MemberListItemViewModel
    {
        public AppUser User { get; set; } = new();
        public string Role { get; set; } = "User";
        public int TotalTasks { get; set; }
        public int ActiveTasks { get; set; }
        public int DoneTasks { get; set; }
        public double TotalHours { get; set; }
        public int NotesContributedCount { get; set; }
        public int CompletionRate => TotalTasks > 0 ? (int)Math.Round((double)DoneTasks / TotalTasks * 100) : 0;
        public int UserLevel { get; set; } = 1;
        public string? FeaturedBadgeIcon { get; set; }
        public string? FeaturedBadgeColor { get; set; }
        public string? FeaturedBadgeName { get; set; }
    }

    public class MemberDetailsViewModel
    {
        public AppUser User { get; set; } = new();
        public string Role { get; set; } = "User";
        public int TotalTasks { get; set; }
        public int InProgressTasks { get; set; }
        public int TodoTasks { get; set; }
        public int DoneTasks { get; set; }
        public int OverdueTasks { get; set; }
        public double TotalHours { get; set; }
        public int NotesContributedCount { get; set; }
        public int CompletionRate => TotalTasks > 0 ? (int)Math.Round((double)DoneTasks / TotalTasks * 100) : 0;

        public List<WorkTask> AssignedTasks { get; set; } = new();
        public List<WorkNote> ContributedNotes { get; set; } = new();
        public List<WorkSession> WorkSessions { get; set; } = new();

        // Gamification & Badges
        public GamificationProfileDto Gamification { get; set; } = new();
        public List<MasterBadge> AvailableManualBadges { get; set; } = new();
    }

    public class MemberFormViewModel
    {
        public string? Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? JobTitle { get; set; }
        public string? PhoneNumber { get; set; }
        public string? AvatarColor { get; set; } = "#6366F1";
        public string Role { get; set; } = "User";
        public string? Password { get; set; }
    }

    public class CalendarEventViewModel
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Start { get; set; }
        public string? End { get; set; }
        public string Color { get; set; } = "#6366F1";
        public string Status { get; set; } = string.Empty;
        public string Priority { get; set; } = string.Empty;
        public string? ProjectName { get; set; }
        public string Url { get; set; } = string.Empty;
    }

    public class TaskFormViewModel
    {
        public WorkTask Task { get; set; } = new();
        public List<Project> Projects { get; set; } = new();
        public List<Category> Categories { get; set; } = new();
        public List<AppUser> Users { get; set; } = new();
        public List<WorkTask> AvailableParentTasks { get; set; } = new();
        public List<MasterMilestone> Milestones { get; set; } = new();
    }

    public class AuditTrailChartDto
    {
        public List<string> Labels { get; set; } = new();
        public List<int> GetCounts { get; set; } = new();
        public List<int> CreateCounts { get; set; } = new();
        public List<int> EditCounts { get; set; } = new();
        public List<int> DeleteCounts { get; set; } = new();
        public List<int> LoginCounts { get; set; } = new();
        public List<int> LogoutCounts { get; set; } = new();

        public int TotalGet { get; set; }
        public int TotalCreate { get; set; }
        public int TotalEdit { get; set; }
        public int TotalDelete { get; set; }
        public int TotalLogin { get; set; }
        public int TotalLogout { get; set; }
        public int GrandTotal => TotalGet + TotalCreate + TotalEdit + TotalDelete + TotalLogin + TotalLogout;
    }

    public class MasterDataViewModel
    {
        public string ActiveTab { get; set; } = "categories";
        public List<Category> Categories { get; set; } = new();
        public List<MasterPriority> Priorities { get; set; } = new();
        public List<MasterStatus> Statuses { get; set; } = new();
        public List<MasterMilestone> Milestones { get; set; } = new();
        public List<MasterBadge> Badges { get; set; } = new();
    }

    // ── Gamification ViewModels & DTOs ─────────────────────────
    public class GamificationProfileDto
    {
        public int TotalExp { get; set; }
        public int Level { get; set; } = 1;
        public string LevelTitle { get; set; } = "🌱 Novice Tracker";
        public int CurrentLevelExp { get; set; }
        public int NextLevelExp { get; set; } = 200;
        public int ExpProgressPercent { get; set; }
        public int UnlockedBadgesCount { get; set; }
        public int TotalBadgesCount { get; set; }
        public BadgeItemDto? FeaturedBadge { get; set; }
        public List<BadgeItemDto> Badges { get; set; } = new();
    }

    public class BadgeItemDto
    {
        public int Id { get; set; }
        public int? UserBadgeId { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Category { get; set; } = "Tasks";
        public string Icon { get; set; } = "fa-solid fa-medal";
        public string Color { get; set; } = "#F59E0B";
        public int Points { get; set; } = 100;
        public BadgeRarity Rarity { get; set; }
        public string RarityName => Rarity.ToString();
        public BadgeTriggerType TriggerType { get; set; }
        public int TriggerThreshold { get; set; }
        public bool IsActive { get; set; }
        public int OrderIndex { get; set; }
        public bool IsUnlocked { get; set; }
        public bool IsFeatured { get; set; }
        public DateTime? UnlockedAt { get; set; }
        public string? AwardedBy { get; set; }
        public int CurrentProgress { get; set; }
        public int ProgressPercent { get; set; }
    }

    public class AwardManualBadgeDto
    {
        public string UserId { get; set; } = string.Empty;
        public int BadgeId { get; set; }
        public string? Note { get; set; }
    }
}
