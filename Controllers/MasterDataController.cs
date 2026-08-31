using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TrackerKerja.Data;
using TrackerKerja.Models;
using TrackerKerja.ViewModels;

namespace TrackerKerja.Controllers
{
    [Authorize(Roles = "Admin")]
    public class MasterDataController : Controller
    {
        private readonly AppDbContext _db;

        public MasterDataController(AppDbContext db)
        {
            _db = db;
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
                    .ToListAsync()
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
    }
}
