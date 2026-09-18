using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TrackerKerja.Data;
using TrackerKerja.Models;
using TrackerKerja.Services;

namespace TrackerKerja.Controllers
{
    [Authorize]
    public class ProjectController : Controller
    {
        private readonly AppDbContext _db;
        private readonly UserManager<AppUser> _userManager;

        public ProjectController(AppDbContext db, UserManager<AppUser> userManager)
        {
            _db = db;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var isAdmin = User.IsInRole("Admin");
            var userCompanyId = currentUser?.CompanyId;

            var query = _db.Projects
                .Include(p => p.Company)
                .Include(p => p.Tasks)
                .ThenInclude(t => t.Sessions)
                .AsQueryable();

            if (!isAdmin)
            {
                query = query.Where(p => p.CompanyId == userCompanyId);
            }

            var projects = await query.OrderByDescending(p => p.CreatedAt).ToListAsync();
            return View(projects);
        }

        public async Task<IActionResult> Detail(int id)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var isAdmin = User.IsInRole("Admin");

            var project = await _db.Projects
                .Include(p => p.Company)
                .Include(p => p.Tasks)
                .ThenInclude(t => t.Sessions)
                .Include(p => p.Tasks)
                .ThenInclude(t => t.Category)
                .Include(p => p.Tasks)
                .ThenInclude(t => t.AssignedToUser)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (project == null) return NotFound();

            if (!TaskPermissionHelper.CanViewProject(currentUser, isAdmin, project))
            {
                TempData["Error"] = "Anda tidak memiliki hak akses untuk melihat proyek dari tim / perusahaan lain.";
                return RedirectToAction(nameof(Index));
            }

            return View(project);
        }

        public IActionResult Create() => View(new Project());

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Project model)
        {
            if (!ModelState.IsValid) return View(model);

            var currentUser = await _userManager.GetUserAsync(User);
            model.CompanyId = currentUser?.CompanyId ?? 1;
            model.CreatedAt = DateTime.Now;

            _db.Projects.Add(model);
            await _db.SaveChangesAsync();
            TempData["Success"] = "Proyek berhasil dibuat!";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int id)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var isAdmin = User.IsInRole("Admin");

            var project = await _db.Projects.FindAsync(id);
            if (project == null) return NotFound();

            if (!TaskPermissionHelper.CanViewProject(currentUser, isAdmin, project))
            {
                TempData["Error"] = "Anda tidak memiliki izin untuk mengedit proyek ini.";
                return RedirectToAction(nameof(Index));
            }

            return View(project);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Project model)
        {
            if (id != model.Id) return BadRequest();
            if (!ModelState.IsValid) return View(model);

            var currentUser = await _userManager.GetUserAsync(User);
            var isAdmin = User.IsInRole("Admin");

            var existing = await _db.Projects.FindAsync(id);
            if (existing == null) return NotFound();

            if (!TaskPermissionHelper.CanViewProject(currentUser, isAdmin, existing))
            {
                TempData["Error"] = "Anda tidak memiliki izin untuk mengedit proyek ini.";
                return RedirectToAction(nameof(Index));
            }

            existing.Name = model.Name;
            existing.Description = model.Description;
            existing.Color = model.Color;
            existing.Deadline = model.Deadline;
            existing.Status = model.Status;

            _db.Projects.Update(existing);
            await _db.SaveChangesAsync();
            TempData["Success"] = "Proyek berhasil diperbarui!";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var isAdmin = User.IsInRole("Admin");

            var project = await _db.Projects.FindAsync(id);
            if (project != null)
            {
                if (!TaskPermissionHelper.CanViewProject(currentUser, isAdmin, project))
                {
                    TempData["Error"] = "Anda tidak memiliki izin untuk menghapus proyek ini.";
                    return RedirectToAction(nameof(Index));
                }

                _db.Projects.Remove(project);
                await _db.SaveChangesAsync();
                TempData["Success"] = "Proyek berhasil dihapus!";
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
