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

        public async Task<IActionResult> Index(ProjectStatus? status = null, string? search = null, int? companyId = null)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var isAdmin = User.IsInRole("Admin");
            var userCompanyId = currentUser?.CompanyId;

            var query = _db.Projects
                .Include(p => p.Company)
                .Include(p => p.ProjectManager)
                .Include(p => p.Tasks)
                    .ThenInclude(t => t.Sessions)
                .AsQueryable();

            if (!isAdmin)
            {
                query = query.Where(p => p.CompanyId == userCompanyId);
            }
            else if (companyId.HasValue)
            {
                query = query.Where(p => p.CompanyId == companyId.Value);
            }

            if (isAdmin)
            {
                ViewBag.Companies = await _db.Companies.OrderBy(c => c.Name).ToListAsync();
                ViewBag.SelectedCompanyId = companyId;
            }

            if (status.HasValue)
            {
                query = query.Where(p => p.Status == status.Value);
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim().ToLower();
                query = query.Where(p => p.Name.ToLower().Contains(term) ||
                                         (p.Description != null && p.Description.ToLower().Contains(term)) ||
                                         (p.ClientName != null && p.ClientName.ToLower().Contains(term)) ||
                                         (p.Tags != null && p.Tags.ToLower().Contains(term)));
            }

            var projects = await query.OrderByDescending(p => p.CreatedAt).ToListAsync();

            // Financial & Progress Aggregates
            ViewBag.SelectedStatus = status;
            ViewBag.SearchTerm = search;
            ViewBag.TotalProjects = projects.Count;
            ViewBag.ActiveProjects = projects.Count(p => p.Status == ProjectStatus.Active);
            ViewBag.CompletedProjects = projects.Count(p => p.Status == ProjectStatus.Completed);
            ViewBag.TotalBudget = projects.Sum(p => p.Budget ?? 0);
            ViewBag.TotalActualCost = projects.Sum(p => p.ActualCost ?? 0);
            ViewBag.TotalRemainingBudget = ViewBag.TotalBudget - ViewBag.TotalActualCost;
            ViewBag.AverageProgress = projects.Any() ? (int)Math.Round(projects.Average(p => p.ProgressPercent)) : 0;

            return View(projects);
        }

        public async Task<IActionResult> Detail(int id)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var isAdmin = User.IsInRole("Admin");

            var project = await _db.Projects
                .Include(p => p.Company)
                .Include(p => p.ProjectManager)
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

            // Company team members for task allocation
            var companyId = project.CompanyId ?? currentUser?.CompanyId ?? 1;
            ViewBag.CompanyMembers = await _db.Users
                .Where(u => u.CompanyId == companyId)
                .OrderBy(u => u.FullName)
                .ToListAsync();

            return View(project);
        }

        public async Task<IActionResult> Create()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var isAdmin = User.IsInRole("Admin");
            var companyId = currentUser?.CompanyId ?? 1;

            var managersQuery = _db.Users.AsQueryable();
            if (!isAdmin)
            {
                managersQuery = managersQuery.Where(u => u.CompanyId == companyId);
            }

            ViewBag.Managers = await managersQuery
                .OrderBy(u => u.FullName)
                .ToListAsync();

            if (isAdmin)
            {
                ViewBag.Companies = await _db.Companies.OrderBy(c => c.Name).ToListAsync();
            }

            return View(new Project { CompanyId = companyId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Project model)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var isAdmin = User.IsInRole("Admin");
            var companyId = currentUser?.CompanyId ?? 1;

            ModelState.Remove("Company");
            ModelState.Remove("ProjectManager");
            ModelState.Remove("Tasks");

            if (!ModelState.IsValid)
            {
                var managersQuery = _db.Users.AsQueryable();
                if (!isAdmin)
                {
                    managersQuery = managersQuery.Where(u => u.CompanyId == companyId);
                }

                ViewBag.Managers = await managersQuery
                    .OrderBy(u => u.FullName)
                    .ToListAsync();

                if (isAdmin)
                {
                    ViewBag.Companies = await _db.Companies.OrderBy(c => c.Name).ToListAsync();
                }

                return View(model);
            }

            try
            {
                if (!isAdmin || !model.CompanyId.HasValue)
                {
                    model.CompanyId = companyId;
                }
                model.Name = model.Name.Trim();
                model.Description = model.Description?.Trim();
                model.Color = string.IsNullOrWhiteSpace(model.Color) ? "#6366F1" : model.Color.Trim();
                model.ClientName = model.ClientName?.Trim();
                model.Tags = model.Tags?.Trim();
                model.ProjectManagerId = string.IsNullOrWhiteSpace(model.ProjectManagerId) ? null : model.ProjectManagerId.Trim();
                model.CreatedAt = DateTime.Now;

                _db.Projects.Add(model);
                await _db.SaveChangesAsync();
                TempData["Success"] = $"Proyek '{model.Name}' berhasil dibuat!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Gagal membuat proyek: {ex.Message}");

                var managersQuery = _db.Users.AsQueryable();
                if (!isAdmin)
                {
                    managersQuery = managersQuery.Where(u => u.CompanyId == companyId);
                }

                ViewBag.Managers = await managersQuery
                    .OrderBy(u => u.FullName)
                    .ToListAsync();

                if (isAdmin)
                {
                    ViewBag.Companies = await _db.Companies.OrderBy(c => c.Name).ToListAsync();
                }

                return View(model);
            }
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

            var companyId = project.CompanyId ?? currentUser?.CompanyId ?? 1;
            var managersQuery = _db.Users.AsQueryable();
            if (!isAdmin)
            {
                managersQuery = managersQuery.Where(u => u.CompanyId == companyId);
            }

            ViewBag.Managers = await managersQuery
                .OrderBy(u => u.FullName)
                .ToListAsync();

            if (isAdmin)
            {
                ViewBag.Companies = await _db.Companies.OrderBy(c => c.Name).ToListAsync();
            }

            return View(project);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Project model)
        {
            if (id != model.Id) return BadRequest();

            var currentUser = await _userManager.GetUserAsync(User);
            var isAdmin = User.IsInRole("Admin");

            var existing = await _db.Projects.FindAsync(id);
            if (existing == null) return NotFound();

            if (!TaskPermissionHelper.CanViewProject(currentUser, isAdmin, existing))
            {
                TempData["Error"] = "Anda tidak memiliki izin untuk mengedit proyek ini.";
                return RedirectToAction(nameof(Index));
            }

            ModelState.Remove("Company");
            ModelState.Remove("ProjectManager");
            ModelState.Remove("Tasks");

            if (!ModelState.IsValid)
            {
                var companyId = existing.CompanyId ?? currentUser?.CompanyId ?? 1;
                var managersQuery = _db.Users.AsQueryable();
                if (!isAdmin)
                {
                    managersQuery = managersQuery.Where(u => u.CompanyId == companyId);
                }

                ViewBag.Managers = await managersQuery
                    .OrderBy(u => u.FullName)
                    .ToListAsync();

                if (isAdmin)
                {
                    ViewBag.Companies = await _db.Companies.OrderBy(c => c.Name).ToListAsync();
                }

                return View(model);
            }

            try
            {
                existing.Name = model.Name.Trim();
                existing.Description = model.Description?.Trim();
                existing.Color = string.IsNullOrWhiteSpace(model.Color) ? "#6366F1" : model.Color.Trim();
                existing.Deadline = model.Deadline;
                existing.Status = model.Status;
                existing.ClientName = model.ClientName?.Trim();
                existing.Budget = model.Budget;
                existing.ActualCost = model.ActualCost;
                existing.ProjectManagerId = string.IsNullOrWhiteSpace(model.ProjectManagerId) ? null : model.ProjectManagerId.Trim();
                existing.Tags = model.Tags?.Trim();
                if (isAdmin && model.CompanyId.HasValue)
                {
                    existing.CompanyId = model.CompanyId;
                }

                _db.Projects.Update(existing);
                await _db.SaveChangesAsync();
                TempData["Success"] = $"Proyek '{existing.Name}' berhasil diperbarui!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Gagal memperbarui proyek: {ex.Message}");

                var companyId = existing.CompanyId ?? currentUser?.CompanyId ?? 1;
                var managersQuery = _db.Users.AsQueryable();
                if (!isAdmin)
                {
                    managersQuery = managersQuery.Where(u => u.CompanyId == companyId);
                }

                ViewBag.Managers = await managersQuery
                    .OrderBy(u => u.FullName)
                    .ToListAsync();

                if (isAdmin)
                {
                    ViewBag.Companies = await _db.Companies.OrderBy(c => c.Name).ToListAsync();
                }

                return View(model);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BulkAssignTasks(int projectId, string assigneeUserId)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var isAdmin = User.IsInRole("Admin");

            var project = await _db.Projects
                .Include(p => p.Tasks)
                .FirstOrDefaultAsync(p => p.Id == projectId);

            if (project == null) return NotFound();

            if (!TaskPermissionHelper.CanViewProject(currentUser, isAdmin, project))
            {
                TempData["Error"] = "Anda tidak memiliki izin untuk mengelola tugas proyek ini.";
                return RedirectToAction(nameof(Index));
            }

            var targetUser = await _db.Users.FindAsync(assigneeUserId);
            if (targetUser == null)
            {
                TempData["Error"] = "Anggota tim tujuan tidak ditemukan.";
                return RedirectToAction(nameof(Detail), new { id = projectId });
            }

            var activeTasks = project.Tasks.Where(t => t.Status != TrackerKerja.Models.TaskStatus.Done).ToList();
            if (!activeTasks.Any())
            {
                TempData["Error"] = "Tidak ada tugas aktif dalam proyek ini yang perlu dialokasikan.";
                return RedirectToAction(nameof(Detail), new { id = projectId });
            }

            foreach (var task in activeTasks)
            {
                task.AssignedToUserId = assigneeUserId;
                task.UpdatedAt = DateTime.Now;
            }

            await _db.SaveChangesAsync();
            TempData["Success"] = $"Berhasil mengalokasikan {activeTasks.Count} tugas aktif ke {targetUser.FullName}!";
            return RedirectToAction(nameof(Detail), new { id = projectId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var isAdmin = User.IsInRole("Admin");

            var project = await _db.Projects
                .Include(p => p.Tasks)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (project != null)
            {
                if (!TaskPermissionHelper.CanViewProject(currentUser, isAdmin, project))
                {
                    TempData["Error"] = "Anda tidak memiliki izin untuk menghapus proyek ini.";
                    return RedirectToAction(nameof(Index));
                }

                // Disconnect tasks from project
                foreach (var task in project.Tasks)
                {
                    task.ProjectId = null;
                }

                _db.Projects.Remove(project);
                await _db.SaveChangesAsync();
                TempData["Success"] = "Proyek berhasil dihapus!";
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
