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
    }
}
