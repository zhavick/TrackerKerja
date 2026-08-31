using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TrackerKerja.Data;
using TrackerKerja.Models;
using TrackerKerja.ViewModels;

namespace TrackerKerja.Controllers
{
    [Authorize]
    public class MemberController : Controller
    {
        private readonly AppDbContext _db;
        private readonly UserManager<AppUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public MemberController(AppDbContext db, UserManager<AppUser> userManager, RoleManager<IdentityRole> roleManager)
        {
            _db = db;
            _userManager = userManager;
            _roleManager = roleManager;
        }

        // ── 1. INDEX: LIST ALL TEAM MEMBERS ──────────────────────
        public async Task<IActionResult> Index(string? search, string? role)
        {
            ViewData["Title"] = "Anggota Tim & Kontribusi";

            var usersQuery = _db.Users.AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim().ToLower();
                usersQuery = usersQuery.Where(u =>
                    u.FullName.ToLower().Contains(s) ||
                    (u.Email != null && u.Email.ToLower().Contains(s)) ||
                    (u.JobTitle != null && u.JobTitle.ToLower().Contains(s))
                );
            }

            var users = await usersQuery.OrderBy(u => u.FullName).ToListAsync();
            var allTasks = await _db.Tasks.Include(t => t.Sessions).ToListAsync();
            var allNotes = await _db.Notes.ToListAsync();

            var memberList = new List<MemberListItemViewModel>();

            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                var userRole = roles.FirstOrDefault() ?? "User";

                if (!string.IsNullOrWhiteSpace(role) && !userRole.Equals(role, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var userTasks = allTasks.Where(t => t.AssignedToUserId == user.Id).ToList();
                var userNotesCount = allNotes.Count(n => n.AuthorUserId == user.Id);

                var totalSecs = userTasks.SelectMany(t => t.Sessions).Sum(s => s.DurationSeconds);
                var totalHours = Math.Round(totalSecs / 3600.0, 1);

                memberList.Add(new MemberListItemViewModel
                {
                    User = user,
                    Role = userRole,
                    TotalTasks = userTasks.Count,
                    ActiveTasks = userTasks.Count(t => t.Status != Models.TaskStatus.Done),
                    DoneTasks = userTasks.Count(t => t.Status == Models.TaskStatus.Done),
                    TotalHours = totalHours,
                    NotesContributedCount = userNotesCount
                });
            }

            ViewBag.Search = search;
            ViewBag.RoleFilter = role;
            ViewBag.TotalMembers = memberList.Count;

            return View(memberList);
        }

        // ── 2. DETAILS: MEMBER PROFILE & CONTRIBUTIONS ──────────
        public async Task<IActionResult> Details(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                TempData["Error"] = "Anggota tim tidak ditemukan.";
                return RedirectToAction(nameof(Index));
            }

            var roles = await _userManager.GetRolesAsync(user);
            var userRole = roles.FirstOrDefault() ?? "User";

            var assignedTasks = await _db.Tasks
                .Include(t => t.Project)
                .Include(t => t.Category)
                .Include(t => t.Sessions)
                .Where(t => t.AssignedToUserId == id)
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync();

            var contributedNotes = await _db.Notes
                .Include(n => n.Task)
                .Where(n => n.AuthorUserId == id)
                .OrderByDescending(n => n.UpdatedAt)
                .ToListAsync();

            var userSessions = await _db.Sessions
                .Include(s => s.Task)
                    .ThenInclude(t => t != null ? t.Project : null)
                .Where(s => s.Task != null && s.Task.AssignedToUserId == id)
                .OrderByDescending(s => s.StartTime)
                .Take(20)
                .ToListAsync();

            var totalSecs = assignedTasks.SelectMany(t => t.Sessions).Sum(s => s.DurationSeconds);
            var totalHours = Math.Round(totalSecs / 3600.0, 1);

            var vm = new MemberDetailsViewModel
            {
                User = user,
                Role = userRole,
                TotalTasks = assignedTasks.Count,
                TodoTasks = assignedTasks.Count(t => t.Status == Models.TaskStatus.Todo),
                InProgressTasks = assignedTasks.Count(t => t.Status == Models.TaskStatus.InProgress),
                DoneTasks = assignedTasks.Count(t => t.Status == Models.TaskStatus.Done),
                OverdueTasks = assignedTasks.Count(t => t.DueDate.HasValue && t.DueDate.Value < DateTime.Now && t.Status != Models.TaskStatus.Done),
                TotalHours = totalHours,
                NotesContributedCount = contributedNotes.Count,
                AssignedTasks = assignedTasks,
                ContributedNotes = contributedNotes,
                WorkSessions = userSessions
            };

            ViewData["Title"] = $"Profil & Kontribusi - {user.FullName}";
            return View(vm);
        }

        // ── 3. CREATE: ADD NEW MEMBER ───────────────────────────
        [Authorize(Roles = "Admin")]
        [HttpGet]
        public IActionResult Create()
        {
            ViewData["Title"] = "Tambah Anggota Tim Baru";
            return View(new MemberFormViewModel());
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(MemberFormViewModel model)
        {
            if (string.IsNullOrWhiteSpace(model.FullName) || string.IsNullOrWhiteSpace(model.Email))
            {
                ModelState.AddModelError("", "Nama lengkap dan Email wajib diisi.");
                return View(model);
            }

            var existing = await _userManager.FindByEmailAsync(model.Email);
            if (existing != null)
            {
                ModelState.AddModelError("Email", "Email sudah digunakan oleh anggota lain.");
                return View(model);
            }

            var password = !string.IsNullOrWhiteSpace(model.Password) ? model.Password : "Password123!";

            var user = new AppUser
            {
                UserName = model.Email,
                Email = model.Email,
                FullName = model.FullName,
                JobTitle = model.JobTitle ?? "Team Member",
                PhoneNumber = model.PhoneNumber,
                AvatarColor = model.AvatarColor ?? "#6366F1",
                CreatedAt = DateTime.Now,
                EmailConfirmed = true
            };

            var res = await _userManager.CreateAsync(user, password);
            if (!res.Succeeded)
            {
                foreach (var err in res.Errors)
                    ModelState.AddModelError("", err.Description);
                return View(model);
            }

            // Assign role
            var roleName = model.Role == "Admin" ? "Admin" : "User";
            if (!await _roleManager.RoleExistsAsync(roleName))
            {
                await _roleManager.CreateAsync(new IdentityRole(roleName));
            }
            await _userManager.AddToRoleAsync(user, roleName);

            TempData["Success"] = $"Anggota tim '{user.FullName}' berhasil ditambahkan!";
            return RedirectToAction(nameof(Index));
        }

        // ── 4. EDIT: UPDATE MEMBER ──────────────────────────────
        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<IActionResult> Edit(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            var roles = await _userManager.GetRolesAsync(user);

            var model = new MemberFormViewModel
            {
                Id = user.Id,
                FullName = user.FullName,
                Email = user.Email ?? "",
                JobTitle = user.JobTitle,
                PhoneNumber = user.PhoneNumber,
                AvatarColor = user.AvatarColor,
                Role = roles.FirstOrDefault() ?? "User"
            };

            ViewData["Title"] = $"Edit Anggota - {user.FullName}";
            return View(model);
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, MemberFormViewModel model)
        {
            if (id != model.Id) return BadRequest();

            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            user.FullName = model.FullName;
            user.JobTitle = model.JobTitle ?? "Team Member";
            user.PhoneNumber = model.PhoneNumber;
            user.AvatarColor = model.AvatarColor ?? "#6366F1";

            var res = await _userManager.UpdateAsync(user);
            if (!res.Succeeded)
            {
                foreach (var err in res.Errors)
                    ModelState.AddModelError("", err.Description);
                return View(model);
            }

            // Update role
            var currentRoles = await _userManager.GetRolesAsync(user);
            await _userManager.RemoveFromRolesAsync(user, currentRoles);
            var newRole = model.Role == "Admin" ? "Admin" : "User";
            if (!await _roleManager.RoleExistsAsync(newRole))
            {
                await _roleManager.CreateAsync(new IdentityRole(newRole));
            }
            await _userManager.AddToRoleAsync(user, newRole);

            TempData["Success"] = $"Data anggota '{user.FullName}' berhasil diperbarui!";
            return RedirectToAction(nameof(Index));
        }

        // ── 5. TOGGLE STATUS / LOCKOUT ──────────────────────────
        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleStatus(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser != null && currentUser.Id == user.Id)
            {
                TempData["Error"] = "Anda tidak dapat menonaktifkan akun sendiri.";
                return RedirectToAction(nameof(Index));
            }

            bool isCurrentlyLocked = user.LockoutEnd.HasValue && user.LockoutEnd.Value > DateTimeOffset.Now;

            if (isCurrentlyLocked)
            {
                user.LockoutEnd = null;
                TempData["Success"] = $"Akun '{user.FullName}' telah diaktifkan kembali.";
            }
            else
            {
                user.LockoutEnd = DateTimeOffset.Now.AddYears(100);
                TempData["Success"] = $"Akun '{user.FullName}' telah dinonaktifkan.";
            }

            await _userManager.UpdateAsync(user);
            return RedirectToAction(nameof(Index));
        }

        // ── 6. ADMIN DIRECT PASSWORD RESET ──────────────────────
        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(string id, string newPassword, string? returnUrl)
        {
            if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Length < 6)
            {
                TempData["Error"] = "Password baru minimal 6 karakter.";
                if (!string.IsNullOrEmpty(returnUrl)) return Redirect(returnUrl);
                return RedirectToAction(nameof(Index));
            }

            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                TempData["Error"] = "Anggota tim tidak ditemukan.";
                return RedirectToAction(nameof(Index));
            }

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var result = await _userManager.ResetPasswordAsync(user, token, newPassword);

            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                TempData["Error"] = $"Gagal mengubah password: {errors}";
            }
            else
            {
                TempData["Success"] = $"Password untuk anggota '{user.FullName}' berhasil diubah secara langsung!";
            }

            if (!string.IsNullOrEmpty(returnUrl)) return Redirect(returnUrl);
            return RedirectToAction(nameof(Index));
        }
    }
}
