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
        public int ReviewTasks { get; set; }
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
        public int MyReviewTasks { get; set; }
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

        // Daily Check-In & Gamification Info for Dashboard Card
        public DailyCheckInStatusDto CheckInStatus { get; set; } = new();
        public GamificationUserPointsDto PointsSummary { get; set; } = new();

        // Chart data
        public List<string> WeekLabels { get; set; } = new();
        public List<long> WeekHours { get; set; } = new();

        // Status Distribution Chart
        public List<string> StatusChartLabels { get; set; } = new();
        public List<int> StatusChartCounts { get; set; } = new();
        public List<string> StatusChartColors { get; set; } = new();
        public List<StatusMetricDto> StatusMetrics { get; set; } = new();

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

    public class StatusMetricDto
    {
        public int Id { get; set; }
        public string Key { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string Color { get; set; } = "#6366F1";
        public string Icon { get; set; } = "fa-clipboard-list";
        public int Count { get; set; }
        public bool IsDoneState { get; set; }
        public int OrderIndex { get; set; }
    }

    // ── Dashboard Period-Filtered Analytics DTO ───────────────────
    public class DashboardAnalyticsDto
    {
        // KPI Counts
        public int TotalTasks { get; set; }
        public int DoneTasks { get; set; }
        public int InProgressTasks { get; set; }
        public int TodoTasks { get; set; }
        public int ReviewTasks { get; set; }
        public int OverdueTasks { get; set; }
        public int CompletionRatePercent => TotalTasks > 0 ? (int)Math.Round((double)DoneTasks / TotalTasks * 100) : 0;

        // Personal Stats
        public int MyTotalTasks { get; set; }
        public int MyTodoTasks { get; set; }
        public int MyInProgressTasks { get; set; }
        public int MyReviewTasks { get; set; }
        public int MyDoneTasks { get; set; }
        public string MyTodayWorkFormatted { get; set; } = "0j 0m";

        // Work hours
        public double TotalWorkHours { get; set; }
        public double AvgDailyWorkHours { get; set; }

        // Trend Chart: labels + hours per day
        public List<string> TrendLabels { get; set; } = new();
        public List<double> TrendHours { get; set; } = new();

        // Trend Chart: tasks done per day
        public List<int> TrendDoneTasks { get; set; } = new();

        // Status chart (Dynamic)
        public List<string> StatusLabels { get; set; } = new();
        public List<int> StatusCounts { get; set; } = new();
        public List<string> StatusColors { get; set; } = new();
        public List<StatusMetricDto> StatusMetrics { get; set; } = new();

        // Productivity by Member (name, done, hours)
        public List<MemberProductivityItemDto> MemberProductivity { get; set; } = new();

        // Drill-down: task list (for KPI click modal)
        public List<DrillDownTaskDto> DrillDownTasks { get; set; } = new();

        // Period info
        public string Period { get; set; } = "week";
        public string PeriodLabel { get; set; } = "7 Hari Terakhir";
        public string DateFrom { get; set; } = string.Empty;
        public string DateTo { get; set; } = string.Empty;
    }

    public class MemberProductivityItemDto
    {
        public string MemberId { get; set; } = string.Empty;
        public string MemberName { get; set; } = string.Empty;
        public string MemberColor { get; set; } = "#6366F1";
        public string Initials { get; set; } = string.Empty;
        public int DoneTasks { get; set; }
        public int TotalTasks { get; set; }
        public double WorkHours { get; set; }
        public int CompletionRatePercent => TotalTasks > 0 ? (int)Math.Round((double)DoneTasks / TotalTasks * 100) : 0;
    }

    public class DrillDownTaskDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string Priority { get; set; } = string.Empty;
        public string? ProjectName { get; set; }
        public string? AssigneeName { get; set; }
        public string? DueDate { get; set; }
        public bool IsOverdue { get; set; }
        public double WorkHours { get; set; }
        public string EditUrl => $"/Task/Edit/{Id}";
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
        public int? CompanyId { get; set; }
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
        public string? AssigneeName { get; set; }
        public string? AssigneeAvatar { get; set; }
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
        public List<RewardItem> Rewards { get; set; } = new();
        public List<RewardClaim> Claims { get; set; } = new();
        public GamificationSettingsDto GamificationSettings { get; set; } = new();
    }

    // ── Gamification ViewModels & DTOs ─────────────────────────
    public class GamificationSettingsDto
    {
        public int DailyCheckInPoints { get; set; } = 10;
        public int PointValueRupiah { get; set; } = 100;
        public int MonthlyStreakDays { get; set; } = 30;
        public int MonthlyStreakBonusPoints { get; set; } = 500;
        public int MissedDaysReset { get; set; } = 2;
    }

    public class GamificationUserPointsDto
    {
        public int BadgePoints { get; set; }
        public int CheckInPoints { get; set; }
        public int PotentialMonthlyCheckInPoints { get; set; }
        public int EffectiveCheckInDays { get; set; }
        public string CheckInTierStatus { get; set; } = string.Empty;
        public int TotalEarnedPoints => BadgePoints + CheckInPoints;
        public int SpentPoints { get; set; }
        public int AvailablePoints => Math.Max(0, TotalEarnedPoints - SpentPoints);
        public int PointValueRupiah { get; set; } = 100;
        public decimal RupiahEquivalent => AvailablePoints * PointValueRupiah;
    }

    public class DailyCheckInStatusDto
    {
        public bool HasCheckedInToday { get; set; }
        public int CurrentStreak { get; set; }
        public DateTime? LastCheckInDate { get; set; }
        public int TotalCheckIns { get; set; }
        public int MonthlyTargetDays { get; set; } = 30;
        public int DaysRemainingForMonthlyReward => Math.Max(0, MonthlyTargetDays - CurrentStreak);
        public int DaysRemainingForHalfReward => Math.Max(0, 15 - CurrentStreak);
        public bool IsEligibleForHalfReward => CurrentStreak >= 15;
        public bool IsEligibleForMonthlyReward => CurrentStreak >= MonthlyTargetDays;
        public int PointsPerCheckIn { get; set; } = 10;
        public int PointValueRupiah { get; set; } = 100;
        public int MonthlyBonusPoints { get; set; } = 500;
        public int TotalMonthlyCheckInPoints => (MonthlyTargetDays * PointsPerCheckIn) + MonthlyBonusPoints;
        public int HalfMonthlyCheckInPoints => TotalMonthlyCheckInPoints / 2;
        public int MissedDaysReset { get; set; } = 2;
        public List<DailyCheckIn> RecentCheckIns { get; set; } = new();
    }

    public class DailyCheckInResultDto
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public int PointsEarned { get; set; }
        public int StreakDay { get; set; }
        public bool IsMonthlyMilestone { get; set; }
        public int AvailablePoints { get; set; }
    }

    public class RewardClaimResultDto
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public int ClaimId { get; set; }
        public int RemainingPoints { get; set; }
    }

    public class GamificationPageViewModel
    {
        public GamificationProfileDto Gamification { get; set; } = new();
        public GamificationUserPointsDto PointsSummary { get; set; } = new();
        public DailyCheckInStatusDto CheckInStatus { get; set; } = new();
        public List<RewardItem> Rewards { get; set; } = new();
        public List<RewardClaim> MyClaims { get; set; } = new();
        public string ActiveTab { get; set; } = "checkin";
    }

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

    public class QuickCreateTaskDto
    {
        public string Title { get; set; } = string.Empty;
        public int? ProjectId { get; set; }
        public string? AssignedToUserId { get; set; }
        public string? Priority { get; set; } = "Medium";
        public string? DueDate { get; set; }
        public string? Description { get; set; }
    }
}
