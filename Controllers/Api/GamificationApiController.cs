using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using TrackerKerja.Models;
using TrackerKerja.Services;
using TrackerKerja.ViewModels;

namespace TrackerKerja.Controllers.Api
{
    [ApiController]
    [Route("api/gamification")]
    [Produces("application/json")]
    [Authorize]
    public class GamificationApiController : ControllerBase
    {
        private readonly IGamificationService _gamificationService;
        private readonly UserManager<AppUser> _userManager;

        public GamificationApiController(
            IGamificationService gamificationService,
            UserManager<AppUser> userManager)
        {
            _gamificationService = gamificationService;
            _userManager = userManager;
        }

        /// <summary>
        /// Mengambil ringkasan statistik gamifikasi, level, EXP, dan seluruh badge untuk user saat ini.
        /// </summary>
        [HttpGet("my-badges")]
        public async Task<IActionResult> GetMyBadges()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                // Fallback to first user / admin if unauthenticated in local testing
                user = await _userManager.FindByEmailAsync("admin@trackerkerja.com");
                if (user == null)
                    return Unauthorized(new { success = false, message = "User belum login" });
            }

            var stats = await _gamificationService.GetGamificationStatsAsync(user.Id);
            return Ok(new
            {
                success = true,
                message = "Berhasil memuat data gamifikasi",
                data = stats
            });
        }

        /// <summary>
        /// Mengambil ringkasan gamifikasi untuk spesifik user (berdasarkan userId).
        /// </summary>
        [HttpGet("user/{userId}")]
        public async Task<IActionResult> GetUserBadges(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return NotFound(new { success = false, message = "Pengguna tidak ditemukan" });

            var stats = await _gamificationService.GetGamificationStatsAsync(userId);
            return Ok(new
            {
                success = true,
                message = "Berhasil memuat data gamifikasi pengguna",
                data = stats
            });
        }

        /// <summary>
        /// Memicu evaluasi otomatis dan memberikan badge baru jika kriteria tercapai.
        /// </summary>
        [HttpPost("evaluate")]
        public async Task<IActionResult> EvaluateBadges()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                user = await _userManager.FindByEmailAsync("admin@trackerkerja.com");
                if (user == null)
                    return Unauthorized(new { success = false, message = "User belum login" });
            }

            var newUnlocks = await _gamificationService.EvaluateAndAwardBadgesAsync(user.Id);
            return Ok(new
            {
                success = true,
                newlyUnlockedCount = newUnlocks.Count,
                newBadges = newUnlocks.Select(b => new
                {
                    b.Id,
                    b.Code,
                    b.Name,
                    b.Description,
                    b.Icon,
                    b.Color,
                    b.Points,
                    Rarity = b.Rarity.ToString()
                })
            });
        }

        /// <summary>
        /// Menyematkan (Pin / Feature) badge favorit pada profil pengguna.
        /// </summary>
        [HttpPost("feature/{userBadgeId}")]
        public async Task<IActionResult> ToggleFeature(int userBadgeId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                user = await _userManager.FindByEmailAsync("admin@trackerkerja.com");
                if (user == null)
                    return Unauthorized(new { success = false, message = "User belum login" });
            }

            var success = await _gamificationService.ToggleFeatureBadgeAsync(user.Id, userBadgeId);
            if (!success)
                return BadRequest(new { success = false, message = "Gagal memperbarui status badge utama" });

            return Ok(new { success = true, message = "Status badge favorit berhasil diperbarui" });
        }

        /// <summary>
        /// Memberikan badge secara manual kepada pengguna (Hanya Admin).
        /// </summary>
        [HttpPost("award-manual")]
        public async Task<IActionResult> AwardManual([FromBody] AwardManualBadgeDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.UserId) || dto.BadgeId <= 0)
                return BadRequest(new { success = false, message = "UserId dan BadgeId wajib diisi" });

            var currentUser = await _userManager.GetUserAsync(User);
            var awardedByName = currentUser?.FullName ?? "Admin";

            var success = await _gamificationService.AwardManualBadgeAsync(dto.UserId, dto.BadgeId, awardedByName);
            if (!success)
                return BadRequest(new { success = false, message = "Badge sudah pernah diberikan atau tidak valid" });

            return Ok(new { success = true, message = "Badge penghargaan berhasil diberikan kepada anggota!" });
        }

        /// <summary>
        /// Mengambil daftar snapshot badge gamifikasi yang tersimpan pasca reset database (Hanya Admin).
        /// </summary>
        [HttpGet("snapshots")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetSnapshots([FromQuery] string? userId = null, [FromQuery] string? batchId = null)
        {
            var snapshots = await _gamificationService.GetPendingBadgeSnapshotsAsync(userId, batchId);
            return Ok(new
            {
                success = true,
                total = snapshots.Count,
                data = snapshots
            });
        }

        /// <summary>
        /// Memulihkan badge tunggal dari snapshot kepada pengguna (Hanya Admin).
        /// </summary>
        [HttpPost("snapshots/{id}/restore")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> RestoreSnapshot(int id)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var adminName = currentUser?.FullName ?? "Admin";

            var success = await _gamificationService.RestoreSnapshotBadgeAsync(id, adminName);
            if (!success)
                return BadRequest(new { success = false, message = "Gagal memulihkan badge atau badge telah dipulihkan sebelumnya." });

            return Ok(new { success = true, message = "Badge berhasil dipulihkan kepada pengguna!" });
        }

        /// <summary>
        /// Memulihkan seluruh badge dari snapshot secara massal (Hanya Admin).
        /// </summary>
        [HttpPost("snapshots/restore-all")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> RestoreAllSnapshots([FromBody] RestoreSnapshotBadgeRequestDto dto)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var adminName = currentUser?.FullName ?? "Admin";

            var count = await _gamificationService.RestoreAllSnapshotBadgesAsync(dto.UserId, dto.OnlyIfConditionMet, adminName);
            return Ok(new
            {
                success = true,
                restoredCount = count,
                message = $"Sebanyak {count} badge berhasil dipulihkan kepada pengguna!"
            });
        }

        /// <summary>
        /// Menghapus/mengabaikan catatan snapshot badge tertentu (Hanya Admin).
        /// </summary>
        [HttpDelete("snapshots/{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DismissSnapshot(int id)
        {
            var success = await _gamificationService.DismissSnapshotBadgeAsync(id);
            if (!success)
                return NotFound(new { success = false, message = "Catatan snapshot tidak ditemukan." });

            return Ok(new { success = true, message = "Catatan snapshot berhasil dihapus." });
        }
    }
}
