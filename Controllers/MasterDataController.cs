using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using TrackerKerja.Data;
using TrackerKerja.Models;
using TrackerKerja.Services;
using TrackerKerja.ViewModels;

namespace TrackerKerja.Controllers
{
    [Authorize(Roles = "Admin")]
    public class MasterDataController : Controller
    {
        private readonly AppDbContext _db;
        private readonly IGamificationService _gamificationService;

        public MasterDataController(AppDbContext db, IGamificationService gamificationService)
        {
            _db = db;
            _gamificationService = gamificationService;
        }

        // ── INDEX / MAIN VIEW ──────────────────────────────────
        public async Task<IActionResult> Index(string tab = "categories")
        {
            var model = new MasterDataViewModel
            {
                ActiveTab = string.IsNullOrWhiteSpace(tab) ? "categories" : tab.ToLowerInvariant(),
                Categories = await _db.Categories
                    .Include(c => c.Tasks)
                    .OrderBy(c => c.Name)
                    .ToListAsync(),
                Priorities = await _db.MasterPriorities
                    .OrderBy(p => p.OrderIndex)
                    .ThenBy(p => p.Name)
                    .ToListAsync(),
                Statuses = await _db.MasterStatuses
                    .OrderBy(s => s.OrderIndex)
                    .ThenBy(s => s.Name)
                    .ToListAsync(),
                Milestones = await _db.MasterMilestones
                    .OrderBy(m => m.OrderIndex)
                    .ThenBy(m => m.Name)
                    .ToListAsync(),
                Badges = await _db.MasterBadges
                    .OrderBy(b => b.OrderIndex)
                    .ThenBy(b => b.Name)
                    .ToListAsync(),
                Rewards = await _db.RewardItems
                    .OrderBy(r => r.OrderIndex)
                    .ThenBy(r => r.PointCost)
                    .ToListAsync(),
                Claims = await _db.RewardClaims
                    .Include(c => c.RewardItem)
                    .Include(c => c.User)
                    .Include(c => c.ProcessedByUser)
                    .OrderByDescending(c => c.ClaimedAt)
                    .ToListAsync(),
                GamificationSettings = await _gamificationService.GetGamificationSettingsAsync()
            };

            return View(model);
        }

        // ── CATEGORY CRUD ──────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateCategory(Category model)
        {
            if (string.IsNullOrWhiteSpace(model.Name))
            {
                TempData["Error"] = "Nama kategori tidak boleh kosong.";
                return RedirectToAction(nameof(Index), new { tab = "categories" });
            }

            if (await _db.Categories.AnyAsync(c => c.Name.ToLower() == model.Name.Trim().ToLower()))
            {
                TempData["Error"] = $"Kategori '{model.Name}' sudah ada.";
                return RedirectToAction(nameof(Index), new { tab = "categories" });
            }

            model.Name = model.Name.Trim();
            model.Color = string.IsNullOrWhiteSpace(model.Color) ? "#6366F1" : model.Color.Trim();
            _db.Categories.Add(model);
            await _db.SaveChangesAsync();

            TempData["Success"] = $"Kategori '{model.Name}' berhasil ditambahkan!";
            return RedirectToAction(nameof(Index), new { tab = "categories" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditCategory(Category model)
        {
            var category = await _db.Categories.FindAsync(model.Id);
            if (category == null)
            {
                TempData["Error"] = "Kategori tidak ditemukan.";
                return RedirectToAction(nameof(Index), new { tab = "categories" });
            }

            if (string.IsNullOrWhiteSpace(model.Name))
            {
                TempData["Error"] = "Nama kategori tidak boleh kosong.";
                return RedirectToAction(nameof(Index), new { tab = "categories" });
            }

            category.Name = model.Name.Trim();
            category.Color = string.IsNullOrWhiteSpace(model.Color) ? "#6366F1" : model.Color.Trim();
            category.Description = model.Description?.Trim();

            await _db.SaveChangesAsync();
            TempData["Success"] = $"Kategori '{category.Name}' berhasil diperbarui!";
            return RedirectToAction(nameof(Index), new { tab = "categories" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteCategory(int id)
        {
            var category = await _db.Categories.Include(c => c.Tasks).FirstOrDefaultAsync(c => c.Id == id);
            if (category == null)
            {
                TempData["Error"] = "Kategori tidak ditemukan.";
                return RedirectToAction(nameof(Index), new { tab = "categories" });
            }

            // Unlink from tasks
            foreach (var task in category.Tasks)
            {
                task.CategoryId = null;
            }

            _db.Categories.Remove(category);
            await _db.SaveChangesAsync();

            TempData["Success"] = $"Kategori '{category.Name}' berhasil dihapus.";
            return RedirectToAction(nameof(Index), new { tab = "categories" });
        }

        // ── PRIORITY CRUD ──────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreatePriority(MasterPriority model)
        {
            if (string.IsNullOrWhiteSpace(model.Name))
            {
                TempData["Error"] = "Nama prioritas tidak boleh kosong.";
                return RedirectToAction(nameof(Index), new { tab = "priorities" });
            }

            if (await _db.MasterPriorities.AnyAsync(p => p.Name.ToLower() == model.Name.Trim().ToLower()))
            {
                TempData["Error"] = $"Prioritas '{model.Name}' sudah ada.";
                return RedirectToAction(nameof(Index), new { tab = "priorities" });
            }

            model.Name = model.Name.Trim();
            model.Color = string.IsNullOrWhiteSpace(model.Color) ? "#F59E0B" : model.Color.Trim();
            model.Icon = string.IsNullOrWhiteSpace(model.Icon) ? "fa-flag" : model.Icon.Trim();

            if (model.IsDefault)
            {
                var defaults = await _db.MasterPriorities.Where(p => p.IsDefault).ToListAsync();
                foreach (var d in defaults) d.IsDefault = false;
            }

            _db.MasterPriorities.Add(model);
            await _db.SaveChangesAsync();

            TempData["Success"] = $"Prioritas '{model.Name}' berhasil ditambahkan!";
            return RedirectToAction(nameof(Index), new { tab = "priorities" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditPriority(MasterPriority model)
        {
            var priority = await _db.MasterPriorities.FindAsync(model.Id);
            if (priority == null)
            {
                TempData["Error"] = "Data prioritas tidak ditemukan.";
                return RedirectToAction(nameof(Index), new { tab = "priorities" });
            }

            if (string.IsNullOrWhiteSpace(model.Name))
            {
                TempData["Error"] = "Nama prioritas tidak boleh kosong.";
                return RedirectToAction(nameof(Index), new { tab = "priorities" });
            }

            priority.Name = model.Name.Trim();
            priority.Color = string.IsNullOrWhiteSpace(model.Color) ? "#F59E0B" : model.Color.Trim();
            priority.Icon = string.IsNullOrWhiteSpace(model.Icon) ? "fa-flag" : model.Icon.Trim();
            priority.OrderIndex = model.OrderIndex;
            priority.Description = model.Description?.Trim();

            if (model.IsDefault)
            {
                var defaults = await _db.MasterPriorities.Where(p => p.IsDefault && p.Id != model.Id).ToListAsync();
                foreach (var d in defaults) d.IsDefault = false;
                priority.IsDefault = true;
            }
            else
            {
                priority.IsDefault = false;
            }

            await _db.SaveChangesAsync();
            TempData["Success"] = $"Prioritas '{priority.Name}' berhasil diperbarui!";
            return RedirectToAction(nameof(Index), new { tab = "priorities" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeletePriority(int id)
        {
            var priority = await _db.MasterPriorities.FindAsync(id);
            if (priority == null)
            {
                TempData["Error"] = "Prioritas tidak ditemukan.";
                return RedirectToAction(nameof(Index), new { tab = "priorities" });
            }

            if (await _db.MasterPriorities.CountAsync() <= 1)
            {
                TempData["Error"] = "Minimal harus ada 1 jenis prioritas dalam sistem.";
                return RedirectToAction(nameof(Index), new { tab = "priorities" });
            }

            _db.MasterPriorities.Remove(priority);
            await _db.SaveChangesAsync();

            TempData["Success"] = $"Prioritas '{priority.Name}' berhasil dihapus.";
            return RedirectToAction(nameof(Index), new { tab = "priorities" });
        }

        // ── STATUS CRUD ────────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateStatus(MasterStatus model)
        {
            if (string.IsNullOrWhiteSpace(model.Name))
            {
                TempData["Error"] = "Nama status tidak boleh kosong.";
                return RedirectToAction(nameof(Index), new { tab = "statuses" });
            }

            if (await _db.MasterStatuses.AnyAsync(s => s.Name.ToLower() == model.Name.Trim().ToLower()))
            {
                TempData["Error"] = $"Status '{model.Name}' sudah ada.";
                return RedirectToAction(nameof(Index), new { tab = "statuses" });
            }

            model.Name = model.Name.Trim();
            model.Color = string.IsNullOrWhiteSpace(model.Color) ? "#06B6D4" : model.Color.Trim();

            if (model.IsDefault)
            {
                var defaults = await _db.MasterStatuses.Where(s => s.IsDefault).ToListAsync();
                foreach (var d in defaults) d.IsDefault = false;
            }

            _db.MasterStatuses.Add(model);
            await _db.SaveChangesAsync();

            TempData["Success"] = $"Status '{model.Name}' berhasil ditambahkan!";
            return RedirectToAction(nameof(Index), new { tab = "statuses" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditStatus(MasterStatus model)
        {
            var status = await _db.MasterStatuses.FindAsync(model.Id);
            if (status == null)
            {
                TempData["Error"] = "Data status tidak ditemukan.";
                return RedirectToAction(nameof(Index), new { tab = "statuses" });
            }

            if (string.IsNullOrWhiteSpace(model.Name))
            {
                TempData["Error"] = "Nama status tidak boleh kosong.";
                return RedirectToAction(nameof(Index), new { tab = "statuses" });
            }

            status.Name = model.Name.Trim();
            status.Color = string.IsNullOrWhiteSpace(model.Color) ? "#06B6D4" : model.Color.Trim();
            status.IsDoneState = model.IsDoneState;
            status.OrderIndex = model.OrderIndex;
            status.Description = model.Description?.Trim();

            if (model.IsDefault)
            {
                var defaults = await _db.MasterStatuses.Where(s => s.IsDefault && s.Id != model.Id).ToListAsync();
                foreach (var d in defaults) d.IsDefault = false;
                status.IsDefault = true;
            }
            else
            {
                status.IsDefault = false;
            }

            await _db.SaveChangesAsync();
            TempData["Success"] = $"Status '{status.Name}' berhasil diperbarui!";
            return RedirectToAction(nameof(Index), new { tab = "statuses" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteStatus(int id)
        {
            var status = await _db.MasterStatuses.FindAsync(id);
            if (status == null)
            {
                TempData["Error"] = "Status tidak ditemukan.";
                return RedirectToAction(nameof(Index), new { tab = "statuses" });
            }

            if (await _db.MasterStatuses.CountAsync() <= 1)
            {
                TempData["Error"] = "Minimal harus ada 1 jenis status dalam sistem.";
                return RedirectToAction(nameof(Index), new { tab = "statuses" });
            }

            _db.MasterStatuses.Remove(status);
            await _db.SaveChangesAsync();

            TempData["Success"] = $"Status '{status.Name}' berhasil dihapus.";
            return RedirectToAction(nameof(Index), new { tab = "statuses" });
        }

        // ── MILESTONE (SDLC WATERFALL) CRUD ────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateMilestone(MasterMilestone model)
        {
            if (string.IsNullOrWhiteSpace(model.Name))
            {
                TempData["Error"] = "Nama milestone tidak boleh kosong.";
                return RedirectToAction(nameof(Index), new { tab = "milestones" });
            }

            if (await _db.MasterMilestones.AnyAsync(m => m.Name.ToLower() == model.Name.Trim().ToLower()))
            {
                TempData["Error"] = $"Milestone '{model.Name}' sudah ada.";
                return RedirectToAction(nameof(Index), new { tab = "milestones" });
            }

            model.Name = model.Name.Trim();
            model.Phase = string.IsNullOrWhiteSpace(model.Phase) ? model.Name : model.Phase.Trim();
            model.Color = string.IsNullOrWhiteSpace(model.Color) ? "#6366F1" : model.Color.Trim();
            model.Icon = string.IsNullOrWhiteSpace(model.Icon) ? "fa-flag" : model.Icon.Trim();
            model.Description = model.Description?.Trim();

            if (model.IsDefault)
            {
                var defaults = await _db.MasterMilestones.Where(m => m.IsDefault).ToListAsync();
                foreach (var d in defaults) d.IsDefault = false;
            }

            _db.MasterMilestones.Add(model);
            await _db.SaveChangesAsync();

            TempData["Success"] = $"Milestone '{model.Name}' berhasil ditambahkan!";
            return RedirectToAction(nameof(Index), new { tab = "milestones" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditMilestone(MasterMilestone model)
        {
            var milestone = await _db.MasterMilestones.FindAsync(model.Id);
            if (milestone == null)
            {
                TempData["Error"] = "Data milestone tidak ditemukan.";
                return RedirectToAction(nameof(Index), new { tab = "milestones" });
            }

            if (string.IsNullOrWhiteSpace(model.Name))
            {
                TempData["Error"] = "Nama milestone tidak boleh kosong.";
                return RedirectToAction(nameof(Index), new { tab = "milestones" });
            }

            milestone.Name = model.Name.Trim();
            milestone.Phase = string.IsNullOrWhiteSpace(model.Phase) ? model.Name : model.Phase.Trim();
            milestone.Color = string.IsNullOrWhiteSpace(model.Color) ? "#6366F1" : model.Color.Trim();
            milestone.Icon = string.IsNullOrWhiteSpace(model.Icon) ? "fa-flag" : model.Icon.Trim();
            milestone.OrderIndex = model.OrderIndex;
            milestone.Description = model.Description?.Trim();

            if (model.IsDefault)
            {
                var defaults = await _db.MasterMilestones.Where(m => m.IsDefault && m.Id != model.Id).ToListAsync();
                foreach (var d in defaults) d.IsDefault = false;
                milestone.IsDefault = true;
            }
            else
            {
                milestone.IsDefault = false;
            }

            await _db.SaveChangesAsync();
            TempData["Success"] = $"Milestone '{milestone.Name}' berhasil diperbarui!";
            return RedirectToAction(nameof(Index), new { tab = "milestones" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteMilestone(int id)
        {
            var milestone = await _db.MasterMilestones.FindAsync(id);
            if (milestone == null)
            {
                TempData["Error"] = "Milestone tidak ditemukan.";
                return RedirectToAction(nameof(Index), new { tab = "milestones" });
            }

            if (await _db.MasterMilestones.CountAsync() <= 1)
            {
                TempData["Error"] = "Minimal harus ada 1 jenis milestone dalam sistem.";
                return RedirectToAction(nameof(Index), new { tab = "milestones" });
            }

            _db.MasterMilestones.Remove(milestone);
            await _db.SaveChangesAsync();

            TempData["Success"] = $"Milestone '{milestone.Name}' berhasil dihapus.";
            return RedirectToAction(nameof(Index), new { tab = "milestones" });
        }

        // ── BADGES & ACHIEVEMENTS CRUD ─────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateBadge(MasterBadge model)
        {
            if (string.IsNullOrWhiteSpace(model.Name) || string.IsNullOrWhiteSpace(model.Code))
            {
                TempData["Error"] = "Kode dan Nama Badge tidak boleh kosong.";
                return RedirectToAction(nameof(Index), new { tab = "badges" });
            }

            model.Code = model.Code.Trim().ToUpperInvariant().Replace(" ", "_");

            if (await _db.MasterBadges.AnyAsync(b => b.Code == model.Code))
            {
                TempData["Error"] = $"Badge dengan kode '{model.Code}' sudah ada.";
                return RedirectToAction(nameof(Index), new { tab = "badges" });
            }

            model.Name = model.Name.Trim();
            model.Description = model.Description?.Trim() ?? string.Empty;
            model.Category = string.IsNullOrWhiteSpace(model.Category) ? "Tasks" : model.Category.Trim();
            model.Icon = string.IsNullOrWhiteSpace(model.Icon) ? "fa-solid fa-award" : model.Icon.Trim();
            model.Color = string.IsNullOrWhiteSpace(model.Color) ? "#F59E0B" : model.Color.Trim();
            model.Points = Math.Max(10, model.Points);
            model.TriggerThreshold = Math.Max(1, model.TriggerThreshold);
            model.CreatedAt = DateTime.UtcNow;

            _db.MasterBadges.Add(model);
            await _db.SaveChangesAsync();

            TempData["Success"] = $"Badge '{model.Name}' berhasil ditambahkan ke sistem gamifikasi!";
            return RedirectToAction(nameof(Index), new { tab = "badges" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditBadge(MasterBadge model)
        {
            var badge = await _db.MasterBadges.FindAsync(model.Id);
            if (badge == null)
            {
                TempData["Error"] = "Data badge tidak ditemukan.";
                return RedirectToAction(nameof(Index), new { tab = "badges" });
            }

            if (string.IsNullOrWhiteSpace(model.Name) || string.IsNullOrWhiteSpace(model.Code))
            {
                TempData["Error"] = "Kode dan Nama Badge tidak boleh kosong.";
                return RedirectToAction(nameof(Index), new { tab = "badges" });
            }

            model.Code = model.Code.Trim().ToUpperInvariant().Replace(" ", "_");

            if (await _db.MasterBadges.AnyAsync(b => b.Code == model.Code && b.Id != model.Id))
            {
                TempData["Error"] = $"Badge dengan kode '{model.Code}' sudah digunakan oleh badge lain.";
                return RedirectToAction(nameof(Index), new { tab = "badges" });
            }

            badge.Code = model.Code;
            badge.Name = model.Name.Trim();
            badge.Description = model.Description?.Trim() ?? string.Empty;
            badge.Category = string.IsNullOrWhiteSpace(model.Category) ? "Tasks" : model.Category.Trim();
            badge.Icon = string.IsNullOrWhiteSpace(model.Icon) ? "fa-solid fa-award" : model.Icon.Trim();
            badge.Color = string.IsNullOrWhiteSpace(model.Color) ? "#F59E0B" : model.Color.Trim();
            badge.Points = Math.Max(10, model.Points);
            badge.Rarity = model.Rarity;
            badge.TriggerType = model.TriggerType;
            badge.TriggerThreshold = Math.Max(1, model.TriggerThreshold);
            badge.IsActive = model.IsActive;
            badge.OrderIndex = model.OrderIndex;

            await _db.SaveChangesAsync();
            TempData["Success"] = $"Badge '{badge.Name}' berhasil diperbarui!";
            return RedirectToAction(nameof(Index), new { tab = "badges" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteBadge(int id)
        {
            var badge = await _db.MasterBadges.Include(b => b.UserBadges).FirstOrDefaultAsync(b => b.Id == id);
            if (badge == null)
            {
                TempData["Error"] = "Badge tidak ditemukan.";
                return RedirectToAction(nameof(Index), new { tab = "badges" });
            }

            _db.MasterBadges.Remove(badge);
            await _db.SaveChangesAsync();

            TempData["Success"] = $"Badge '{badge.Name}' berhasil dihapus.";
            return RedirectToAction(nameof(Index), new { tab = "badges" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleBadgeStatus(int id)
        {
            var badge = await _db.MasterBadges.FindAsync(id);
            if (badge == null)
            {
                TempData["Error"] = "Badge tidak ditemukan.";
                return RedirectToAction(nameof(Index), new { tab = "badges" });
            }

            badge.IsActive = !badge.IsActive;
            await _db.SaveChangesAsync();

            TempData["Success"] = $"Status badge '{badge.Name}' berhasil diubah menjadi {(badge.IsActive ? "Aktif" : "Nonaktif")}.";
            return RedirectToAction(nameof(Index), new { tab = "badges" });
        }

        // ── GAMIFICATION SETTINGS ─────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveGamificationSettings(GamificationSettingsDto model)
        {
            if (model.DailyCheckInPoints <= 0 || model.PointValueRupiah <= 0 || model.MonthlyStreakDays <= 0)
            {
                TempData["Error"] = "Nilai konfigurasi poin dan hari streak harus lebih dari 0.";
                return RedirectToAction(nameof(Index), new { tab = "rewards" });
            }

            await _gamificationService.SaveGamificationSettingsAsync(model);
            TempData["Success"] = "Konfigurasi Poin & Daily Check-In berhasil diperbarui!";
            return RedirectToAction(nameof(Index), new { tab = "rewards" });
        }

        // ── MASTER REWARDS CRUD ────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateReward(RewardItem model)
        {
            if (string.IsNullOrWhiteSpace(model.Name))
            {
                TempData["Error"] = "Nama Hadiah tidak boleh kosong.";
                return RedirectToAction(nameof(Index), new { tab = "rewards" });
            }

            model.Name = model.Name.Trim();
            model.Category = string.IsNullOrWhiteSpace(model.Category) ? "Voucher" : model.Category.Trim();
            model.PointCost = Math.Max(1, model.PointCost);
            model.Stock = Math.Max(0, model.Stock);
            model.Icon = string.IsNullOrWhiteSpace(model.Icon) ? "fa-solid fa-gift" : model.Icon.Trim();
            model.Color = string.IsNullOrWhiteSpace(model.Color) ? "#EC4899" : model.Color.Trim();
            model.CreatedAt = DateTime.Now;

            _db.RewardItems.Add(model);
            await _db.SaveChangesAsync();

            TempData["Success"] = $"Hadiah '{model.Name}' berhasil ditambahkan ke katalog!";
            return RedirectToAction(nameof(Index), new { tab = "rewards" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditReward(RewardItem model)
        {
            var reward = await _db.RewardItems.FindAsync(model.Id);
            if (reward == null)
            {
                TempData["Error"] = "Data hadiah tidak ditemukan.";
                return RedirectToAction(nameof(Index), new { tab = "rewards" });
            }

            if (string.IsNullOrWhiteSpace(model.Name))
            {
                TempData["Error"] = "Nama Hadiah tidak boleh kosong.";
                return RedirectToAction(nameof(Index), new { tab = "rewards" });
            }

            reward.Name = model.Name.Trim();
            reward.Description = model.Description?.Trim();
            reward.Category = string.IsNullOrWhiteSpace(model.Category) ? "Voucher" : model.Category.Trim();
            reward.PointCost = Math.Max(1, model.PointCost);
            reward.Stock = Math.Max(0, model.Stock);
            reward.ImageUrl = model.ImageUrl?.Trim();
            reward.Icon = string.IsNullOrWhiteSpace(model.Icon) ? "fa-solid fa-gift" : model.Icon.Trim();
            reward.Color = string.IsNullOrWhiteSpace(model.Color) ? "#EC4899" : model.Color.Trim();
            reward.IsMonthlyMilestoneReward = model.IsMonthlyMilestoneReward;
            reward.IsActive = model.IsActive;
            reward.OrderIndex = model.OrderIndex;

            await _db.SaveChangesAsync();
            TempData["Success"] = $"Hadiah '{reward.Name}' berhasil diperbarui!";
            return RedirectToAction(nameof(Index), new { tab = "rewards" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteReward(int id)
        {
            var reward = await _db.RewardItems.Include(r => r.Claims).FirstOrDefaultAsync(r => r.Id == id);
            if (reward == null)
            {
                TempData["Error"] = "Hadiah tidak ditemukan.";
                return RedirectToAction(nameof(Index), new { tab = "rewards" });
            }

            if (reward.Claims.Any())
            {
                // Soft disable if has claims history
                reward.IsActive = false;
                await _db.SaveChangesAsync();
                TempData["Success"] = $"Hadiah '{reward.Name}' memiliki riwayat klaim, status diubah menjadi Nonaktif.";
            }
            else
            {
                _db.RewardItems.Remove(reward);
                await _db.SaveChangesAsync();
                TempData["Success"] = $"Hadiah '{reward.Name}' berhasil dihapus.";
            }

            return RedirectToAction(nameof(Index), new { tab = "rewards" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleRewardStatus(int id)
        {
            var reward = await _db.RewardItems.FindAsync(id);
            if (reward == null)
            {
                TempData["Error"] = "Hadiah tidak ditemukan.";
                return RedirectToAction(nameof(Index), new { tab = "rewards" });
            }

            reward.IsActive = !reward.IsActive;
            await _db.SaveChangesAsync();

            TempData["Success"] = $"Status hadiah '{reward.Name}' berhasil diubah menjadi {(reward.IsActive ? "Aktif" : "Nonaktif")}.";
            return RedirectToAction(nameof(Index), new { tab = "rewards" });
        }

        // ── PROCESS REWARD CLAIM (APPROVAL / REJECTION / COMPLETION) ──
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ProcessRewardClaim(int id, ClaimStatus status, string? adminNotes)
        {
            var claim = await _db.RewardClaims.Include(c => c.RewardItem).Include(c => c.User).FirstOrDefaultAsync(c => c.Id == id);
            if (claim == null)
            {
                TempData["Error"] = "Data klaim tidak ditemukan.";
                return RedirectToAction(nameof(Index), new { tab = "rewards" });
            }

            var previousStatus = claim.Status;
            claim.Status = status;
            claim.AdminNotes = adminNotes?.Trim();
            claim.ProcessedAt = DateTime.Now;
            claim.ProcessedByUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // If changing to Rejected and wasn't previously rejected, refund stock
            if (status == ClaimStatus.Rejected && previousStatus != ClaimStatus.Rejected && claim.RewardItem != null)
            {
                claim.RewardItem.Stock += 1;
            }
            // If changing from Rejected back to Approved/Pending, re-decrement stock
            else if (previousStatus == ClaimStatus.Rejected && status != ClaimStatus.Rejected && claim.RewardItem != null && claim.RewardItem.Stock > 0)
            {
                claim.RewardItem.Stock -= 1;
            }

            await _db.SaveChangesAsync();

            string statusText = status switch
            {
                ClaimStatus.Approved => "Disetujui",
                ClaimStatus.Completed => "Selesai / Terkirim",
                ClaimStatus.Rejected => "Ditolak (Poin dikembalikan)",
                _ => "Diproses"
            };

            TempData["Success"] = $"Klaim hadiah dari '{claim.User?.FullName ?? "Pengguna"}' berhasil diubah menjadi '{statusText}'!";
            return RedirectToAction(nameof(Index), new { tab = "rewards" });
        }
    }
}
