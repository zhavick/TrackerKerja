using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TrackerKerja.Data;
using TrackerKerja.Models;

namespace TrackerKerja.Controllers
{
    [Authorize]
    public class ProjectController : Controller
    {
        private readonly AppDbContext _db;
        public ProjectController(AppDbContext db) { _db = db; }

        public async Task<IActionResult> Index()
        {
            var projects = await _db.Projects
                .Include(p => p.Tasks)
                .ThenInclude(t => t.Sessions)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();
            return View(projects);
        }

        public async Task<IActionResult> Detail(int id)
        {
            var project = await _db.Projects
                .Include(p => p.Tasks)
                .ThenInclude(t => t.Sessions)
                .Include(p => p.Tasks)
                .ThenInclude(t => t.Category)
                .FirstOrDefaultAsync(p => p.Id == id);
            if (project == null) return NotFound();
            return View(project);
        }

        [Authorize(Roles = "Admin")]
        public IActionResult Create() => View(new Project());

        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Project model)
        {
            if (!ModelState.IsValid) return View(model);
            model.CreatedAt = DateTime.Now;
            _db.Projects.Add(model);
            await _db.SaveChangesAsync();
            TempData["Success"] = "Proyek berhasil dibuat!";
            return RedirectToAction(nameof(Index));
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int id)
        {
            var project = await _db.Projects.FindAsync(id);
            if (project == null) return NotFound();
            return View(project);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Project model)
        {
            if (id != model.Id) return BadRequest();
            if (!ModelState.IsValid) return View(model);
            _db.Projects.Update(model);
            await _db.SaveChangesAsync();
            TempData["Success"] = "Proyek berhasil diperbarui!";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var project = await _db.Projects.FindAsync(id);
            if (project != null)
            {
                _db.Projects.Remove(project);
                await _db.SaveChangesAsync();
                TempData["Success"] = "Proyek berhasil dihapus!";
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
