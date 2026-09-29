using Microsoft.EntityFrameworkCore;
using TrackerKerja.Data;
using TrackerKerja.Models;
using TrackerKerja.ViewModels;

namespace TrackerKerja.Services
{
    public class GamificationService : IGamificationService
    {
        private readonly AppDbContext _db;

        public GamificationService(AppDbContext db)
        {
            _db = db;
        }

        public async Task<List<MasterBadge>> EvaluateAndAwardBadgesAsync(string userId)
        {
            if (string.IsNullOrEmpty(userId))
                return new List<MasterBadge>();

            var user = await _db.Users
                .Include(u => u.UserBadges)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
                return new List<MasterBadge>();

            // Calculate current metrics for the user
            var userEmail = user.Email;
            var doneTasksCount = await _db.Tasks.CountAsync(t => t.AssignedToUserId == userId && t.Status == Models.TaskStatus.Done);
            var totalTasksCount = await _db.Tasks.CountAsync(t => t.AssignedToUserId == userId);
            
            var totalWorkSeconds = await _db.Sessions
                .Where(s => s.UserId == userId)
                .SumAsync(s => (long?)s.Duration) ?? 0;
            var totalHours = totalWorkSeconds / 3600.0;

            var totalNotesCount = await _db.Notes.CountAsync(n => n.AuthorUserId == userId);
            var totalAttendanceCount = await _db.Attendances.CountAsync(a => a.UserId == userId);
            var totalTimesheetCount = await _db.Sessions.CountAsync(s => s.UserId == userId);
            var totalJsonCount = await _db.JsonHistories.CountAsync(j => j.UserId == userId);
            var totalSqlCount = await _db.SqlHistories.CountAsync(s => s.UserId == userId);
            var totalLoginCount = await _db.AuditLogs.CountAsync(a => (a.UserId == userId || (userEmail != null && a.UserEmail == userEmail)) && ((a.ControllerName == "Account" && a.ActionName == "Login") || (a.Path != null && a.Path.ToLower().Contains("/login"))));
            var totalLogoutCount = await _db.AuditLogs.CountAsync(a => (a.UserId == userId || (userEmail != null && a.UserEmail == userEmail)) && ((a.ControllerName == "Account" && a.ActionName == "Logout") || (a.Path != null && a.Path.ToLower().Contains("/logout"))));
            var totalDailyCheckInCount = await _db.DailyCheckIns.CountAsync(c => c.UserId == userId);
            var maxDailyStreak = await _db.DailyCheckIns.Where(c => c.UserId == userId).Select(c => (int?)c.StreakDay).MaxAsync() ?? 0;

            var isProfileComplete = !string.IsNullOrWhiteSpace(user.FullName) &&
                                    !string.IsNullOrWhiteSpace(user.JobTitle) &&
                                    !string.IsNullOrWhiteSpace(user.ProfilePictureUrl);

            var activeBadges = await _db.MasterBadges
                .Where(b => b.IsActive && b.TriggerType != BadgeTriggerType.Manual)
                .ToListAsync();

            var userBadgeIds = user.UserBadges.Select(ub => ub.BadgeId).ToHashSet();
            var newlyUnlockedBadges = new List<MasterBadge>();

            foreach (var badge in activeBadges)
            {
                if (userBadgeIds.Contains(badge.Id))
                    continue;

                bool shouldUnlock = false;

                switch (badge.TriggerType)
                {
                    case BadgeTriggerType.Auto_DoneTasks:
                        shouldUnlock = doneTasksCount >= badge.TriggerThreshold;
                        break;

                    case BadgeTriggerType.Auto_TotalTasks:
                        shouldUnlock = totalTasksCount >= badge.TriggerThreshold;
                        break;

                    case BadgeTriggerType.Auto_TotalHours:
                        shouldUnlock = totalHours >= badge.TriggerThreshold;
                        break;

                    case BadgeTriggerType.Auto_NotesCount:
                        shouldUnlock = totalNotesCount >= badge.TriggerThreshold;
                        break;

                    case BadgeTriggerType.Auto_ProfileComplete:
                        shouldUnlock = isProfileComplete;
                        break;

                    case BadgeTriggerType.Auto_AttendanceCount:
                        shouldUnlock = totalAttendanceCount >= badge.TriggerThreshold;
                        break;

                    case BadgeTriggerType.Auto_TimesheetCount:
                        shouldUnlock = totalTimesheetCount >= badge.TriggerThreshold;
                        break;

                    case BadgeTriggerType.Auto_JsonCount:
                        shouldUnlock = totalJsonCount >= badge.TriggerThreshold;
                        break;

                    case BadgeTriggerType.Auto_SqlCount:
                        shouldUnlock = totalSqlCount >= badge.TriggerThreshold;
                        break;

                    case BadgeTriggerType.Auto_LoginCount:
                        shouldUnlock = totalLoginCount >= badge.TriggerThreshold;
                        break;

                    case BadgeTriggerType.Auto_LogoutCount:
                        shouldUnlock = totalLogoutCount >= badge.TriggerThreshold;
                        break;

                    case BadgeTriggerType.Auto_DailyCheckInCount:
                        shouldUnlock = totalDailyCheckInCount >= badge.TriggerThreshold;
                        break;

                    case BadgeTriggerType.Auto_DailyCheckInStreak:
                        shouldUnlock = maxDailyStreak >= badge.TriggerThreshold;
                        break;
                }

                if (shouldUnlock)
                {
                    var userBadge = new UserBadge
                    {
                        UserId = userId,
                        BadgeId = badge.Id,
                        UnlockedAt = DateTime.UtcNow,
                        IsFeatured = user.UserBadges.Count == 0 // Set as featured if it's the very first badge
                    };

                    _db.UserBadges.Add(userBadge);
                    newlyUnlockedBadges.Add(badge);
                }
            }

            if (newlyUnlockedBadges.Any())
            {
                await _db.SaveChangesAsync();
            }

            return newlyUnlockedBadges;
        }

        public async Task<GamificationProfileDto> GetGamificationStatsAsync(string userId)
        {
            var user = await _db.Users.FindAsync(userId);
            var userEmail = user?.Email;

            var allBadges = await _db.MasterBadges
                .Where(b => b.IsActive)
                .OrderBy(b => b.OrderIndex)
                .ThenBy(b => b.Rarity)
                .ToListAsync();

            var userBadges = await _db.UserBadges
                .Include(ub => ub.Badge)
                .Where(ub => ub.UserId == userId)
                .ToListAsync();

            var userBadgeMap = userBadges.ToDictionary(ub => ub.BadgeId, ub => ub);

            // Fetch user metrics for progress bar
            var doneTasksCount = await _db.Tasks.CountAsync(t => t.AssignedToUserId == userId && t.Status == Models.TaskStatus.Done);
            var totalTasksCount = await _db.Tasks.CountAsync(t => t.AssignedToUserId == userId);
            var totalWorkSeconds = await _db.Sessions
                .Where(s => s.UserId == userId)
                .SumAsync(s => (long?)s.Duration) ?? 0;
            var totalHours = (int)Math.Floor(totalWorkSeconds / 3600.0);
            var totalNotesCount = await _db.Notes.CountAsync(n => n.AuthorUserId == userId);
            var totalAttendanceCount = await _db.Attendances.CountAsync(a => a.UserId == userId);
            var totalTimesheetCount = await _db.Sessions.CountAsync(s => s.UserId == userId);
            var totalJsonCount = await _db.JsonHistories.CountAsync(j => j.UserId == userId);
            var totalSqlCount = await _db.SqlHistories.CountAsync(s => s.UserId == userId);
            var totalLoginCount = await _db.AuditLogs.CountAsync(a => (a.UserId == userId || (userEmail != null && a.UserEmail == userEmail)) && ((a.ControllerName == "Account" && a.ActionName == "Login") || (a.Path != null && a.Path.ToLower().Contains("/login"))));
            var totalLogoutCount = await _db.AuditLogs.CountAsync(a => (a.UserId == userId || (userEmail != null && a.UserEmail == userEmail)) && ((a.ControllerName == "Account" && a.ActionName == "Logout") || (a.Path != null && a.Path.ToLower().Contains("/logout"))));
            var totalDailyCheckInCount = await _db.DailyCheckIns.CountAsync(c => c.UserId == userId);
            var maxDailyStreak = await _db.DailyCheckIns.Where(c => c.UserId == userId).Select(c => (int?)c.StreakDay).MaxAsync() ?? 0;

            var badgeItemDtos = new List<BadgeItemDto>();
            int totalExp = 0;

            foreach (var badge in allBadges)
            {
                var isUnlocked = userBadgeMap.TryGetValue(badge.Id, out var userBadge);
                int currentProgress = 0;
                int progressPercent = 0;

                if (isUnlocked)
                {
                    totalExp += badge.Points;
                    currentProgress = badge.TriggerThreshold;
                    progressPercent = 100;
                }
                else
                {
                    switch (badge.TriggerType)
                    {
                        case BadgeTriggerType.Auto_DoneTasks:
                            currentProgress = doneTasksCount;
                            break;
                        case BadgeTriggerType.Auto_TotalTasks:
                            currentProgress = totalTasksCount;
                            break;
                        case BadgeTriggerType.Auto_TotalHours:
                            currentProgress = totalHours;
                            break;
                        case BadgeTriggerType.Auto_NotesCount:
                            currentProgress = totalNotesCount;
                            break;
                        case BadgeTriggerType.Auto_AttendanceCount:
                            currentProgress = totalAttendanceCount;
                            break;
                        case BadgeTriggerType.Auto_TimesheetCount:
                            currentProgress = totalTimesheetCount;
                            break;
                        case BadgeTriggerType.Auto_JsonCount:
                            currentProgress = totalJsonCount;
                            break;
                        case BadgeTriggerType.Auto_SqlCount:
                            currentProgress = totalSqlCount;
                            break;
                        case BadgeTriggerType.Auto_LoginCount:
                            currentProgress = totalLoginCount;
                            break;
                        case BadgeTriggerType.Auto_LogoutCount:
                            currentProgress = totalLogoutCount;
                            break;
                        case BadgeTriggerType.Auto_DailyCheckInCount:
                            currentProgress = totalDailyCheckInCount;
                            break;
                        case BadgeTriggerType.Auto_DailyCheckInStreak:
                            currentProgress = maxDailyStreak;
                            break;
                        default:
                            currentProgress = 0;
                            break;
                    }

                    if (badge.TriggerThreshold > 0)
                    {
                        progressPercent = Math.Min(100, (int)Math.Round((double)currentProgress / badge.TriggerThreshold * 100));
                    }
                }

                badgeItemDtos.Add(new BadgeItemDto
                {
                    Id = badge.Id,
                    UserBadgeId = userBadge?.Id,
                    Code = badge.Code,
                    Name = badge.Name,
                    Description = badge.Description,
                    Category = badge.Category,
                    Icon = badge.Icon,
                    Color = badge.Color,
                    Points = badge.Points,
                    Rarity = badge.Rarity,
                    TriggerType = badge.TriggerType,
                    TriggerThreshold = badge.TriggerThreshold,
                    IsActive = badge.IsActive,
                    OrderIndex = badge.OrderIndex,
                    IsUnlocked = isUnlocked,
                    IsFeatured = userBadge?.IsFeatured ?? false,
                    UnlockedAt = userBadge?.UnlockedAt,
                    AwardedBy = userBadge?.AwardedBy,
                    CurrentProgress = currentProgress,
                    ProgressPercent = progressPercent
                });
            }

            // Level & EXP computation
            // Each level takes 200 EXP points
            int expPerLevel = 200;
            int level = 1 + (totalExp / expPerLevel);
            int currentLevelExp = totalExp % expPerLevel;
            int expProgressPercent = (int)Math.Round((double)currentLevelExp / expPerLevel * 100);

            string levelTitle = level switch
            {
                1 => "🌱 Novice Tracker",
                2 => "⚡ Task Apprentice",
                3 => "🛡️ Work Specialist",
                4 => "⚔️ Productivity Master",
                5 => "💎 Elite Organizer",
                _ => "👑 Work Tracker Legend"
            };

            var featuredBadgeDto = badgeItemDtos.FirstOrDefault(b => b.IsFeatured && b.IsUnlocked);

            return new GamificationProfileDto
            {
                TotalExp = totalExp,
                Level = level,
                LevelTitle = levelTitle,
                CurrentLevelExp = currentLevelExp,
                NextLevelExp = expPerLevel,
                ExpProgressPercent = expProgressPercent,
                UnlockedBadgesCount = userBadges.Count,
                TotalBadgesCount = allBadges.Count,
                FeaturedBadge = featuredBadgeDto,
                Badges = badgeItemDtos
            };
        }

        public async Task<bool> AwardManualBadgeAsync(string userId, int badgeId, string awardedBy)
        {
            var alreadyAwarded = await _db.UserBadges.AnyAsync(ub => ub.UserId == userId && ub.BadgeId == badgeId);
            if (alreadyAwarded)
                return false;

            var userBadge = new UserBadge
            {
                UserId = userId,
                BadgeId = badgeId,
                UnlockedAt = DateTime.UtcNow,
                AwardedBy = awardedBy
            };

            _db.UserBadges.Add(userBadge);
            await _db.SaveChangesAsync();
            return true;
        }

        public async Task<bool> RevokeBadgeAsync(string userId, int badgeId)
        {
            var userBadge = await _db.UserBadges.FirstOrDefaultAsync(ub => ub.UserId == userId && ub.BadgeId == badgeId);
            if (userBadge == null)
                return false;

            _db.UserBadges.Remove(userBadge);
            await _db.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ToggleFeatureBadgeAsync(string userId, int userBadgeId)
        {
            var userBadges = await _db.UserBadges.Where(ub => ub.UserId == userId).ToListAsync();
            var target = userBadges.FirstOrDefault(ub => ub.Id == userBadgeId);
            if (target == null)
                return false;

            bool makeFeatured = !target.IsFeatured;

            // Clear all other featured badges for this user
            foreach (var ub in userBadges)
            {
                ub.IsFeatured = false;
            }

            target.IsFeatured = makeFeatured;
            await _db.SaveChangesAsync();
            return true;
        }

        public async Task<GamificationSettingsDto> GetGamificationSettingsAsync()
        {
            var settings = new GamificationSettingsDto();

            var checkInPointsSetting = await _db.SystemSettings.FirstOrDefaultAsync(s => s.Key == "Gamification_DailyCheckInPoints");
            if (checkInPointsSetting != null && int.TryParse(checkInPointsSetting.Value, out var cip))
                settings.DailyCheckInPoints = Math.Max(1, cip);

            var pointValueSetting = await _db.SystemSettings.FirstOrDefaultAsync(s => s.Key == "Gamification_PointValueRupiah");
            if (pointValueSetting != null && int.TryParse(pointValueSetting.Value, out var pv))
                settings.PointValueRupiah = Math.Max(1, pv);

            var streakDaysSetting = await _db.SystemSettings.FirstOrDefaultAsync(s => s.Key == "Gamification_MonthlyStreakDays");
            if (streakDaysSetting != null && int.TryParse(streakDaysSetting.Value, out var sd))
                settings.MonthlyStreakDays = Math.Max(5, sd);

            var bonusPointsSetting = await _db.SystemSettings.FirstOrDefaultAsync(s => s.Key == "Gamification_MonthlyStreakBonusPoints");
            if (bonusPointsSetting != null && int.TryParse(bonusPointsSetting.Value, out var bp))
                settings.MonthlyStreakBonusPoints = Math.Max(0, bp);

            var resetDaysSetting = await _db.SystemSettings.FirstOrDefaultAsync(s => s.Key == "Gamification_MissedDaysReset");
            if (resetDaysSetting != null && int.TryParse(resetDaysSetting.Value, out var rd))
                settings.MissedDaysReset = Math.Max(2, rd);

            return settings;
        }

        public async Task<bool> SaveGamificationSettingsAsync(GamificationSettingsDto settings)
        {
            async Task UpsertSetting(string key, string value, string description)
            {
                var item = await _db.SystemSettings.FirstOrDefaultAsync(s => s.Key == key);
                if (item == null)
                {
                    _db.SystemSettings.Add(new SystemSetting
                    {
                        Key = key,
                        Value = value,
                        Description = description,
                        UpdatedAt = DateTime.Now
                    });
                }
                else
                {
                    item.Value = value;
                    item.Description = description;
                    item.UpdatedAt = DateTime.Now;
                }
            }

            await UpsertSetting("Gamification_DailyCheckInPoints", settings.DailyCheckInPoints.ToString(), "Poin reward per daily check-in");
            await UpsertSetting("Gamification_PointValueRupiah", settings.PointValueRupiah.ToString(), "Nilai konversi 1 poin dalam rupiah");
            await UpsertSetting("Gamification_MonthlyStreakDays", settings.MonthlyStreakDays.ToString(), "Jumlah hari streak untuk reward klaim 1 bulan");
            await UpsertSetting("Gamification_MonthlyStreakBonusPoints", settings.MonthlyStreakBonusPoints.ToString(), "Bonus poin streak 1 bulan penuh");
            await UpsertSetting("Gamification_MissedDaysReset", settings.MissedDaysReset.ToString(), "Batas hari absen sebelum streak reset ke awal (default 2 hari)");

            await _db.SaveChangesAsync();
            return true;
        }

        public async Task<GamificationUserPointsDto> GetUserPointsSummaryAsync(string userId)
        {
            var settings = await GetGamificationSettingsAsync();

            // 1. Poin dari Badge: Setiap perolehan badge memberikan poin instan
            var badgePoints = await _db.UserBadges
                .Where(ub => ub.UserId == userId)
                .SumAsync(ub => (int?)ub.Badge!.Points) ?? 0;

            // 2. Total akumulasi poin check-in 1 bulan (30 hari target)
            int monthlyTargetDays = settings.MonthlyStreakDays > 0 ? settings.MonthlyStreakDays : 30;
            int totalMonthlyCheckInPoints = (monthlyTargetDays * settings.DailyCheckInPoints) + settings.MonthlyStreakBonusPoints;
            int halfMonthlyCheckInPoints = totalMonthlyCheckInPoints / 2;

            // 3. Hitung hari check-in aktif yang ditempuh user
            var today = DateTime.Today;
            var lastCheckIn = await _db.DailyCheckIns
                .Where(c => c.UserId == userId)
                .OrderByDescending(c => c.CheckInDate)
                .FirstOrDefaultAsync();

            int currentStreak = 0;
            if (lastCheckIn != null)
            {
                var daysDiff = (int)(today - lastCheckIn.CheckInDate.Date).TotalDays;
                if (daysDiff <= 1 || daysDiff < settings.MissedDaysReset)
                {
                    currentStreak = lastCheckIn.StreakDay;
                }
            }

            var startOfMonth = new DateTime(today.Year, today.Month, 1);
            var checkInsThisMonth = await _db.DailyCheckIns
                .Where(c => c.UserId == userId && c.CheckInDate >= startOfMonth)
                .Select(c => c.CheckInDate.Date)
                .Distinct()
                .CountAsync();

            int effectiveCheckInDays = Math.Max(currentStreak, checkInsThisMonth);

            // 4. Logika Poin Daily Check-In:
            // - Selalu dimulai dari 0
            // - Jika menempuh 15 hari -> 1/2 dari total poin
            // - Jika menempuh 30 hari -> 1x dari total poin
            int currentCycleCheckInPoints = 0;
            string checkInTierStatus = "Terkunci (Belum mencapai 15 hari)";

            if (effectiveCheckInDays >= monthlyTargetDays)
            {
                currentCycleCheckInPoints = totalMonthlyCheckInPoints;
                checkInTierStatus = $"Target 30 Hari Tercapai: 1x Penuh (+{totalMonthlyCheckInPoints:N0} pts)";
            }
            else if (effectiveCheckInDays >= 15)
            {
                currentCycleCheckInPoints = halfMonthlyCheckInPoints;
                checkInTierStatus = $"Target 15 Hari Tercapai: 1/2 Poin (+{halfMonthlyCheckInPoints:N0} pts)";
            }
            else
            {
                currentCycleCheckInPoints = 0;
                checkInTierStatus = $"Hari ke-{effectiveCheckInDays}/{monthlyTargetDays}: 0 Poin (Mulai 1/2 Poin di Hari ke-15)";
            }

            // 5. Akumulasi milestone 30-hari dari siklus sebelumnya
            var pastMilestonesCount = await _db.DailyCheckIns
                .CountAsync(c => c.UserId == userId && c.IsMonthlyMilestone);

            int priorMilestones = effectiveCheckInDays >= monthlyTargetDays
                ? Math.Max(0, pastMilestonesCount - 1)
                : pastMilestonesCount;
            int pastMilestoneBonus = priorMilestones * totalMonthlyCheckInPoints;

            int totalCheckInPoints = currentCycleCheckInPoints + pastMilestoneBonus;

            // 6. Poin terpakai untuk klaim hadiah
            var spentPoints = await _db.RewardClaims
                .Where(r => r.UserId == userId && r.Status != ClaimStatus.Rejected)
                .SumAsync(r => (int?)r.PointsSpent) ?? 0;

            return new GamificationUserPointsDto
            {
                BadgePoints = badgePoints,
                CheckInPoints = totalCheckInPoints,
                PotentialMonthlyCheckInPoints = totalMonthlyCheckInPoints,
                EffectiveCheckInDays = effectiveCheckInDays,
                CheckInTierStatus = checkInTierStatus,
                SpentPoints = spentPoints,
                PointValueRupiah = settings.PointValueRupiah
            };
        }

        public async Task<DailyCheckInStatusDto> GetDailyCheckInStatusAsync(string userId)
        {
            var settings = await GetGamificationSettingsAsync();
            var today = DateTime.Today;

            var todayCheckIn = await _db.DailyCheckIns
                .FirstOrDefaultAsync(c => c.UserId == userId && c.CheckInDate.Date == today);

            var recentCheckIns = await _db.DailyCheckIns
                .Where(c => c.UserId == userId)
                .OrderByDescending(c => c.CheckInDate)
                .Take(30)
                .ToListAsync();

            var totalCheckIns = await _db.DailyCheckIns
                .CountAsync(c => c.UserId == userId);

            var lastCheckIn = recentCheckIns.FirstOrDefault();
            int currentStreak = 0;

            if (todayCheckIn != null)
            {
                currentStreak = todayCheckIn.StreakDay;
            }
            else if (lastCheckIn != null)
            {
                var daysDiff = (int)(today - lastCheckIn.CheckInDate.Date).TotalDays;
                if (daysDiff == 1)
                {
                    // Yesterday was checked in, streak is waiting for today's checkin
                    currentStreak = lastCheckIn.StreakDay;
                }
                else if (daysDiff >= settings.MissedDaysReset)
                {
                    // Missed 2 or more days, streak resets back to 0
                    currentStreak = 0;
                }
                else
                {
                    currentStreak = lastCheckIn.StreakDay;
                }
            }

            var hasMonthlyMilestone = await _db.DailyCheckIns
                .AnyAsync(c => c.UserId == userId && c.IsMonthlyMilestone);

            return new DailyCheckInStatusDto
            {
                HasCheckedInToday = todayCheckIn != null,
                CurrentStreak = currentStreak,
                LastCheckInDate = lastCheckIn?.CheckInDate,
                TotalCheckIns = totalCheckIns,
                MonthlyTargetDays = settings.MonthlyStreakDays,
                PointsPerCheckIn = settings.DailyCheckInPoints,
                PointValueRupiah = settings.PointValueRupiah,
                MonthlyBonusPoints = settings.MonthlyStreakBonusPoints,
                MissedDaysReset = settings.MissedDaysReset,
                RecentCheckIns = recentCheckIns
            };
        }

        public async Task<DailyCheckInResultDto> PerformDailyCheckInAsync(string userId, string? notes = null)
        {
            var today = DateTime.Today;
            var alreadyCheckedIn = await _db.DailyCheckIns
                .AnyAsync(c => c.UserId == userId && c.CheckInDate.Date == today);

            if (alreadyCheckedIn)
            {
                var curPoints = await GetUserPointsSummaryAsync(userId);
                return new DailyCheckInResultDto
                {
                    Success = false,
                    Message = "Anda sudah melakukan daily check-in hari ini! Kembali lagi besok untuk melanjutkan streak.",
                    AvailablePoints = curPoints.AvailablePoints
                };
            }

            var settings = await GetGamificationSettingsAsync();
            var lastCheckIn = await _db.DailyCheckIns
                .Where(c => c.UserId == userId)
                .OrderByDescending(c => c.CheckInDate)
                .FirstOrDefaultAsync();

            int newStreak = 1;
            if (lastCheckIn != null)
            {
                var daysDiff = (int)(today - lastCheckIn.CheckInDate.Date).TotalDays;
                if (daysDiff == 1)
                {
                    newStreak = lastCheckIn.StreakDay + 1;
                }
                else if (daysDiff >= settings.MissedDaysReset)
                {
                    // Reset to beginning if missed for 2 days or more
                    newStreak = 1;
                }
                else
                {
                    newStreak = lastCheckIn.StreakDay + 1;
                }
            }

            int pointsEarned = settings.DailyCheckInPoints;
            bool isMonthlyMilestone = false;

            // Check if streak reaches monthly target (e.g. 30 days)
            if (newStreak >= settings.MonthlyStreakDays && (newStreak % settings.MonthlyStreakDays == 0))
            {
                pointsEarned += settings.MonthlyStreakBonusPoints;
                isMonthlyMilestone = true;
            }

            var checkIn = new DailyCheckIn
            {
                UserId = userId,
                CheckInDate = today,
                CheckInTime = DateTime.Now,
                PointsEarned = pointsEarned,
                StreakDay = newStreak,
                IsMonthlyMilestone = isMonthlyMilestone,
                Notes = notes
            };

            _db.DailyCheckIns.Add(checkIn);
            await _db.SaveChangesAsync();

            // Evaluate badge unlocks (daily check-in count and streak badges)
            await EvaluateAndAwardBadgesAsync(userId);

            var updatedPoints = await GetUserPointsSummaryAsync(userId);

            string msg;
            if (isMonthlyMilestone || newStreak >= settings.MonthlyStreakDays)
            {
                msg = $"Luar biasa! Anda berhasil check-in hari ke-{newStreak} (30 Hari Penuh)! Akumulasi saldo poin check-in 1x penuh ({updatedPoints.CheckInPoints} Poin) telah aktif di saldo hadiah Anda. Hadiah bulanan kini siap diklaim!";
            }
            else if (newStreak == 15)
            {
                msg = $"Selamat! Anda telah mencapai 15 hari check-in berturut-turut! 1/2 dari total poin bulanan ({updatedPoints.CheckInPoints} Poin) telah berhasil dicairkan ke saldo hadiah Anda!";
            }
            else if (newStreak < 15)
            {
                int remainingToHalf = 15 - newStreak;
                msg = $"Daily check-in hari ke-{newStreak} berhasil dicatat! Saldo hadiah check-in dimulai dari 0 dan akan mencairkan 1/2 total poin saat mencapai 15 hari ({remainingToHalf} hari lagi).";
            }
            else
            {
                int remainingToFull = Math.Max(0, settings.MonthlyStreakDays - newStreak);
                msg = $"Daily check-in hari ke-{newStreak} berhasil dicatat! Anda telah membuka 1/2 poin bulanan. Lanjutkan hingga hari ke-30 ({remainingToFull} hari lagi) untuk mendapatkan 1x total akumulasi poin penuh!";
            }

            return new DailyCheckInResultDto
            {
                Success = true,
                Message = msg,
                PointsEarned = pointsEarned,
                StreakDay = newStreak,
                IsMonthlyMilestone = isMonthlyMilestone,
                AvailablePoints = updatedPoints.AvailablePoints
            };
        }

        public async Task<RewardClaimResultDto> ClaimRewardAsync(string userId, int rewardItemId, string? userNotes)
        {
            var reward = await _db.RewardItems.FindAsync(rewardItemId);
            if (reward == null)
            {
                return new RewardClaimResultDto { Success = false, Message = "Hadiah yang dipilih tidak ditemukan." };
            }

            if (!reward.IsActive)
            {
                return new RewardClaimResultDto { Success = false, Message = "Hadiah ini sedang tidak aktif atau tidak dapat ditukarkan saat ini." };
            }

            if (reward.Stock <= 0)
            {
                return new RewardClaimResultDto { Success = false, Message = "Maaf, stok hadiah ini sudah habis." };
            }

            if (reward.IsMonthlyMilestoneReward)
            {
                var hasMilestone = await _db.DailyCheckIns.AnyAsync(c => c.UserId == userId && c.IsMonthlyMilestone);
                if (!hasMilestone)
                {
                    return new RewardClaimResultDto
                    {
                        Success = false,
                        Message = "Hadiah ini khusus untuk pengguna yang telah menyelesaikan daily check-in selama 1 bulan berturut-turut (30 hari)."
                    };
                }
            }

            var pointsSummary = await GetUserPointsSummaryAsync(userId);
            if (pointsSummary.AvailablePoints < reward.PointCost)
            {
                return new RewardClaimResultDto
                {
                    Success = false,
                    Message = $"Poin Anda tidak mencukupi. Anda membutuhkan {reward.PointCost} poin, saat ini Anda memiliki {pointsSummary.AvailablePoints} poin (Badge: {pointsSummary.BadgePoints} pts, Check-in: {pointsSummary.CheckInPoints} pts)."
                };
            }

            // Deduct stock
            reward.Stock = Math.Max(0, reward.Stock - 1);

            var claim = new RewardClaim
            {
                UserId = userId,
                RewardItemId = reward.Id,
                PointsSpent = reward.PointCost,
                PointValueSnapshot = pointsSummary.PointValueRupiah,
                RupiahEquivalent = reward.PointCost * pointsSummary.PointValueRupiah,
                Status = ClaimStatus.Pending,
                UserNotes = userNotes,
                ClaimedAt = DateTime.Now
            };

            _db.RewardClaims.Add(claim);
            await _db.SaveChangesAsync();

            var updatedPoints = await GetUserPointsSummaryAsync(userId);

            return new RewardClaimResultDto
            {
                Success = true,
                Message = $"Permintaan klaim hadiah '{reward.Name}' berhasil diajukan! Poin terpakai: {reward.PointCost} poin (Setara Rp {claim.RupiahEquivalent:N0}). Tim admin akan segera memproses penukaran Anda.",
                ClaimId = claim.Id,
                RemainingPoints = updatedPoints.AvailablePoints
            };
        }

        public async Task<List<RewardItem>> GetActiveRewardsAsync()
        {
            return await _db.RewardItems
                .Where(r => r.IsActive)
                .OrderBy(r => r.OrderIndex)
                .ThenBy(r => r.PointCost)
                .ToListAsync();
        }

        public async Task<List<RewardClaim>> GetUserClaimsAsync(string userId)
        {
            return await _db.RewardClaims
                .Include(c => c.RewardItem)
                .Where(c => c.UserId == userId)
                .OrderByDescending(c => c.ClaimedAt)
                .ToListAsync();
        }
    }
}
