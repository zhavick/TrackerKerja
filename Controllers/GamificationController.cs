using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using TrackerKerja.Data;
using TrackerKerja.Models;
using TrackerKerja.Services;
using TrackerKerja.ViewModels;

namespace TrackerKerja.Controllers
{
    [Authorize]
    public class GamificationController : Controller
    {
        private readonly AppDbContext _db;
        private readonly IGamificationService _gamificationService;
        private readonly UserManager<AppUser> _userManager;

        public GamificationController(
            AppDbContext db,
            IGamificationService gamificationService,
            UserManager<AppUser> userManager)
        {
            _db = db;
            _gamificationService = gamificationService;
            _userManager = userManager;
        }

        // GET: /Gamification
        public async Task<IActionResult> Index(string tab = "checkin")
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return RedirectToAction("Login", "Account");

            var profile = await _gamificationService.GetGamificationStatsAsync(userId);
            var pointsSummary = await _gamificationService.GetUserPointsSummaryAsync(userId);
            var checkInStatus = await _gamificationService.GetDailyCheckInStatusAsync(userId);
            var rewards = await _gamificationService.GetActiveRewardsAsync();
            var myClaims = await _gamificationService.GetUserClaimsAsync(userId);

            var model = new GamificationPageViewModel
            {
                Gamification = profile,
                PointsSummary = pointsSummary,
                CheckInStatus = checkInStatus,
                Rewards = rewards,
                MyClaims = myClaims,
                ActiveTab = string.IsNullOrWhiteSpace(tab) ? "checkin" : tab.ToLowerInvariant()
            };

            return View(model);
        }

        // POST: /Gamification/CheckIn
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CheckIn(string? notes)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Json(new { success = false, message = "Sesi telah berakhir, silakan login kembali." });
            }

            var result = await _gamificationService.PerformDailyCheckInAsync(userId, notes);

            if (result.Success)
            {
                // Record AuditLog
                var user = await _userManager.GetUserAsync(User);
                _db.AuditLogs.Add(new AuditLog
                {
                    UserId = userId,
                    UserEmail = user?.Email ?? "user@trackerkerja.com",
                    UserName = user?.FullName ?? user?.UserName ?? "User",
                    ControllerName = "Gamification",
                    ActionName = "DailyCheckIn",
                    HttpMethod = "POST",
                    StatusCode = 200,
                    Path = "/Gamification/CheckIn",
                    Details = $"Daily check-in berhasil. Hari ke-{result.StreakDay}, +{result.PointsEarned} poin diperoleh.",
                    IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1",
                    Timestamp = DateTime.Now
                });
                await _db.SaveChangesAsync();
            }

            return Json(result);
        }

        // POST: /Gamification/ClaimReward
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ClaimReward(int rewardItemId, string? userNotes)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Json(new { success = false, message = "Sesi telah berakhir, silakan login kembali." });
            }

            var result = await _gamificationService.ClaimRewardAsync(userId, rewardItemId, userNotes);

            if (result.Success)
            {
                var user = await _userManager.GetUserAsync(User);
                var reward = await _db.RewardItems.FindAsync(rewardItemId);
                _db.AuditLogs.Add(new AuditLog
                {
                    UserId = userId,
                    UserEmail = user?.Email ?? "user@trackerkerja.com",
                    UserName = user?.FullName ?? user?.UserName ?? "User",
                    ControllerName = "Gamification",
                    ActionName = "ClaimReward",
                    HttpMethod = "POST",
                    StatusCode = 200,
                    Path = "/Gamification/ClaimReward",
                    Details = $"Pengajuan klaim hadiah '{reward?.Name}' (ID {rewardItemId}). Catatan: {userNotes}",
                    IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1",
                    Timestamp = DateTime.Now
                });
                await _db.SaveChangesAsync();
            }

            return Json(result);
        }

        // GET: /Gamification/GetPointsSummary
        [HttpGet]
        public async Task<IActionResult> GetPointsSummary()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var summary = await _gamificationService.GetUserPointsSummaryAsync(userId);
            var checkInStatus = await _gamificationService.GetDailyCheckInStatusAsync(userId);

            return Json(new
            {
                success = true,
                points = summary,
                checkIn = checkInStatus
            });
        }
    }
}
