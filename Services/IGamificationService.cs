using TrackerKerja.Models;
using TrackerKerja.ViewModels;

namespace TrackerKerja.Services
{
    public interface IGamificationService
    {
        Task<List<MasterBadge>> EvaluateAndAwardBadgesAsync(string userId);
        Task<GamificationProfileDto> GetGamificationStatsAsync(string userId);
        Task<bool> AwardManualBadgeAsync(string userId, int badgeId, string awardedBy);
        Task<bool> RevokeBadgeAsync(string userId, int badgeId);
        Task<bool> ToggleFeatureBadgeAsync(string userId, int userBadgeId);
        Task<GamificationSettingsDto> GetGamificationSettingsAsync();
        Task<bool> SaveGamificationSettingsAsync(GamificationSettingsDto settings);
        Task<GamificationUserPointsDto> GetUserPointsSummaryAsync(string userId);
        Task<DailyCheckInStatusDto> GetDailyCheckInStatusAsync(string userId);
        Task<DailyCheckInResultDto> PerformDailyCheckInAsync(string userId, string? notes = null);
        Task<RewardClaimResultDto> ClaimRewardAsync(string userId, int rewardItemId, string? userNotes);
        Task<List<RewardItem>> GetActiveRewardsAsync();
        Task<List<RewardClaim>> GetUserClaimsAsync(string userId);

        // Reset Database & Snapshot Recovery Methods
        Task<GamificationResetResultDto> ResetGamificationDataAsync(string mode, string initiatedBy);
        Task<List<BadgeSnapshotRecoveryDto>> GetPendingBadgeSnapshotsAsync(string? userId = null, string? batchId = null);
        Task<bool> RestoreSnapshotBadgeAsync(int snapshotId, string restoredByAdmin);
        Task<int> RestoreAllSnapshotBadgesAsync(string? userId, bool onlyIfConditionMet, string restoredByAdmin);
        Task<bool> DismissSnapshotBadgeAsync(int snapshotId);
        Task<int> GetPendingBadgeSnapshotsCountAsync();
    }
}
