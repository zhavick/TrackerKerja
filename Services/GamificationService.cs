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
            var doneTasksCount = await _db.Tasks.CountAsync(t => t.AssignedToUserId == userId && t.Status == Models.TaskStatus.Done);
            var totalTasksCount = await _db.Tasks.CountAsync(t => t.AssignedToUserId == userId);
            
            var totalWorkSeconds = await _db.Sessions
                .Where(s => s.UserId == userId)
                .SumAsync(s => (long?)s.Duration) ?? 0;
            var totalHours = totalWorkSeconds / 3600.0;

            var totalNotesCount = await _db.Notes.CountAsync(n => n.AuthorUserId == userId);

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
    }
}
