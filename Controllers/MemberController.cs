using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TrackerKerja.Data;
using TrackerKerja.Models;
using TrackerKerja.ViewModels;
using TrackerKerja.Services;

namespace TrackerKerja.Controllers
{
    [Authorize]
    public class MemberController : Controller
    {
        private readonly AppDbContext _db;
        private readonly UserManager<AppUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IGamificationService _gamificationService;
        private readonly IEmailService _emailService;

        public MemberController(
            AppDbContext db,
            UserManager<AppUser> userManager,
            RoleManager<IdentityRole> roleManager,
            IGamificationService gamificationService,
            IEmailService emailService)
        {
            _db = db;
            _userManager = userManager;
            _roleManager = roleManager;
            _gamificationService = gamificationService;
            _emailService = emailService;
        }

        // ── 1. INDEX: LIST ALL TEAM MEMBERS ──────────────────────
        public async Task<IActionResult> Index(string? search, string? role, int? companyId, string? approvalStatus)
        {
            ViewData["Title"] = "Anggota Tim & Kontribusi";

            var currentUser = await _userManager.GetUserAsync(User);
            var isAdmin = User.IsInRole("Admin");
            var userCompanyId = currentUser?.CompanyId;

            var usersQuery = _db.Users.Include(u => u.Company).AsQueryable();

            // Multi-tenant isolation: non-admins only see users in their own company
            if (!isAdmin)
            {
                usersQuery = usersQuery.Where(u => u.CompanyId == userCompanyId);
            }
            else if (companyId.HasValue)
            {
                usersQuery = usersQuery.Where(u => u.CompanyId == companyId.Value);
            }

            // Approval status filter
            if (!string.IsNullOrWhiteSpace(approvalStatus))
            {
                if (approvalStatus.Equals("pending", StringComparison.OrdinalIgnoreCase))
                {
                    usersQuery = usersQuery.Where(u => !u.IsApproved);
                }
                else if (approvalStatus.Equals("approved", StringComparison.OrdinalIgnoreCase))
                {
                    usersQuery = usersQuery.Where(u => u.IsApproved);
                }
            }

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
            var allUserBadges = await _db.UserBadges.Include(ub => ub.Badge).ToListAsync();

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

                var userBadges = allUserBadges.Where(ub => ub.UserId == user.Id).ToList();
                var totalExp = userBadges.Sum(ub => ub.Badge?.Points ?? 0);
                var level = 1 + (totalExp / 200);
                var featured = userBadges.FirstOrDefault(ub => ub.IsFeatured)?.Badge ?? userBadges.FirstOrDefault()?.Badge;

                memberList.Add(new MemberListItemViewModel
                {
                    User = user,
                    Role = userRole,
                    TotalTasks = userTasks.Count,
                    ActiveTasks = userTasks.Count(t => t.Status != Models.TaskStatus.Done),
                    DoneTasks = userTasks.Count(t => t.Status == Models.TaskStatus.Done),
                    TotalHours = Math.Round(totalSecs / 3600.0, 1),
                    NotesContributedCount = userNotesCount,
                    UserLevel = level,
                    FeaturedBadgeIcon = featured?.Icon,
                    FeaturedBadgeColor = featured?.Color,
                    FeaturedBadgeName = featured?.Name
                });
            }

            var pendingCountQuery = _db.Users.Where(u => !u.IsApproved);
            if (!isAdmin && currentUser != null)
            {
                pendingCountQuery = pendingCountQuery.Where(u => u.CompanyId == userCompanyId);
            }
            var pendingCount = await pendingCountQuery.CountAsync();

            ViewBag.Search = search;
            ViewBag.RoleFilter = role;
            ViewBag.CompanyFilter = companyId;
            ViewBag.ApprovalStatus = approvalStatus ?? "all";
            ViewBag.PendingCount = pendingCount;
            ViewBag.Companies = await _db.Companies.OrderBy(c => c.Name).ToListAsync();
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

            var currentUser = await _userManager.GetUserAsync(User);
            var isAdmin = User.IsInRole("Admin");

            // Multi-tenant check: non-admin cannot view members from other companies
            if (!TaskPermissionHelper.CanAccessCompany(currentUser, isAdmin, user.CompanyId))
            {
                TempData["Error"] = "Anda tidak memiliki akses untuk melihat profil anggota dari tim / perusahaan lain.";
                return RedirectToAction(nameof(Index));
            }

            // Auto-evaluate badges for this member
            await _gamificationService.EvaluateAndAwardBadgesAsync(id);

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

            var gamification = await _gamificationService.GetGamificationStatsAsync(id);
            var availableManualBadges = await _db.MasterBadges
                .Where(b => b.IsActive)
                .OrderBy(b => b.Name)
                .ToListAsync();

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
                WorkSessions = userSessions,
                Gamification = gamification,
                AvailableManualBadges = availableManualBadges
            };

            ViewData["Title"] = $"Profil & Kontribusi - {user.FullName}";
            return View(vm);
        }

        // ── 3. CREATE: ADD NEW MEMBER ───────────────────────────
        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            ViewData["Title"] = "Tambah Anggota Tim Baru";
            ViewBag.Companies = await _db.Companies.OrderBy(c => c.Name).ToListAsync();
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
                ViewBag.Companies = await _db.Companies.OrderBy(c => c.Name).ToListAsync();
                return View(model);
            }

            var existing = await _userManager.FindByEmailAsync(model.Email);
            if (existing != null)
            {
                ModelState.AddModelError("Email", "Email sudah digunakan oleh anggota lain.");
                ViewBag.Companies = await _db.Companies.OrderBy(c => c.Name).ToListAsync();
                return View(model);
            }

            var currentUser = await _userManager.GetUserAsync(User);
            var password = !string.IsNullOrWhiteSpace(model.Password) ? model.Password : "Password123!";

            var user = new AppUser
            {
                UserName = model.Email,
                Email = model.Email,
                FullName = model.FullName,
                JobTitle = model.JobTitle ?? "Team Member",
                PhoneNumber = model.PhoneNumber,
                AvatarColor = model.AvatarColor ?? "#6366F1",
                CompanyId = model.CompanyId ?? currentUser?.CompanyId ?? 1,
                CreatedAt = DateTime.Now,
                EmailConfirmed = true,
                IsApproved = true,
                ApprovedAt = DateTime.Now,
                ApprovedByUserId = currentUser?.Id
            };

            var res = await _userManager.CreateAsync(user, password);
            if (!res.Succeeded)
            {
                foreach (var err in res.Errors)
                    ModelState.AddModelError("", err.Description);
                ViewBag.Companies = await _db.Companies.OrderBy(c => c.Name).ToListAsync();
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
                CompanyId = user.CompanyId,
                Role = roles.FirstOrDefault() ?? "User"
            };

            ViewBag.Companies = await _db.Companies.OrderBy(c => c.Name).ToListAsync();
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
            if (model.CompanyId.HasValue)
            {
                user.CompanyId = model.CompanyId.Value;
            }

            var res = await _userManager.UpdateAsync(user);
            if (!res.Succeeded)
            {
                foreach (var err in res.Errors)
                    ModelState.AddModelError("", err.Description);
                ViewBag.Companies = await _db.Companies.OrderBy(c => c.Name).ToListAsync();
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
                // Send Password Reset Notification Email (Background Safe)
                if (!string.IsNullOrWhiteSpace(user.Email))
                {
                    var resetVars = new Dictionary<string, string>
                    {
                        { "FullName", user.FullName },
                        { "Email", user.Email },
                        { "NewPassword", newPassword },
                        { "LoginUrl", "/Account/Login" },
                        { "CurrentDate", DateTime.Now.ToString("dd MMM yyyy HH:mm") }
                    };
                    _ = Task.Run(async () => await _emailService.SendEventEmailAsync("PASSWORD_RESET_NOTIFICATION", user.Email, resetVars));
                }

                TempData["Success"] = $"Password untuk anggota '{user.FullName}' berhasil diubah secara langsung!";
            }

            if (!string.IsNullOrEmpty(returnUrl)) return Redirect(returnUrl);
            return RedirectToAction(nameof(Index));
        }

        // ── 7. ADMIN AWARD / REVOKE BADGE ───────────────────────
        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AwardBadge(string userId, int badgeId)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var awardedByName = currentUser?.FullName ?? "Admin";

            var success = await _gamificationService.AwardManualBadgeAsync(userId, badgeId, awardedByName);
            if (!success)
            {
                TempData["Error"] = "Badge ini sudah pernah diberikan kepada anggota atau tidak valid.";
            }
            else
            {
                TempData["Success"] = "Badge penghargaan berhasil diberikan kepada anggota!";
            }

            return RedirectToAction(nameof(Details), new { id = userId });
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RevokeBadge(string userId, int badgeId)
        {
            var success = await _gamificationService.RevokeBadgeAsync(userId, badgeId);
            if (!success)
            {
                TempData["Error"] = "Gagal mencabut badge atau badge tidak ditemukan.";
            }
            else
            {
                TempData["Success"] = "Badge penghargaan berhasil dicabut.";
            }

            return RedirectToAction(nameof(Details), new { id = userId });
        }

        // ── 8. ADMIN DELETE MEMBER & LOGIN ACCOUNT ──────────────
        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                TempData["Error"] = "ID Anggota tidak valid.";
                return RedirectToAction(nameof(Index));
            }

            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                TempData["Error"] = "Anggota tim tidak ditemukan.";
                return RedirectToAction(nameof(Index));
            }

            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser != null && currentUser.Id == user.Id)
            {
                TempData["Error"] = "Anda tidak dapat menghapus akun Anda sendiri yang sedang aktif digunakan.";
                return RedirectToAction(nameof(Index));
            }

            // Protect root admin account
            if (string.Equals(user.Email, "admin@trackerkerja.com", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(user.UserName, "admin@trackerkerja.com", StringComparison.OrdinalIgnoreCase))
            {
                TempData["Error"] = "Akun Administrator Utama Sistem (admin@trackerkerja.com) dilindungi dan tidak dapat dihapus.";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                // 1. Unassign tasks assigned to this user (keep project tasks intact)
                var userTasks = await _db.Tasks.Where(t => t.AssignedToUserId == id).ToListAsync();
                foreach (var t in userTasks)
                {
                    t.AssignedToUserId = null;
                }

                // 2. Unassign notes created by this user (keep project notes intact)
                var userNotes = await _db.Notes.Where(n => n.AuthorUserId == id).ToListAsync();
                foreach (var n in userNotes)
                {
                    n.AuthorUserId = null;
                }

                // 3. Clear user work sessions user ID
                var userSessions = await _db.Sessions.Where(s => s.UserId == id).ToListAsync();
                foreach (var s in userSessions)
                {
                    s.UserId = null;
                }

                // 4. Remove user badges
                var userBadges = await _db.UserBadges.Where(ub => ub.UserId == id).ToListAsync();
                _db.UserBadges.RemoveRange(userBadges);

                // 5. Remove attendance records
                var userAttendances = await _db.Attendances.Where(a => a.UserId == id).ToListAsync();
                _db.Attendances.RemoveRange(userAttendances);

                // Save relational updates before deleting Identity user
                await _db.SaveChangesAsync();

                // 6. Delete ASP.NET Core Identity user login account
                var result = await _userManager.DeleteAsync(user);
                if (!result.Succeeded)
                {
                    var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                    TempData["Error"] = $"Gagal menghapus akun login member: {errors}";
                    return RedirectToAction(nameof(Index));
                }

                TempData["Success"] = $"Akun dan data member '{user.FullName}' ({user.Email}) berhasil dihapus permanen dari sistem dan akun login.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Terjadi kesalahan saat menghapus member: {ex.Message}";
            }

            return RedirectToAction(nameof(Index));
        }

        // ── 9. ADMIN APPROVE MEMBER REGISTRATION ────────────────
        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Approve(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                    return Json(new { success = false, message = "ID Anggota tidak valid." });
                TempData["Error"] = "ID Anggota tidak valid.";
                return RedirectToAction(nameof(Index));
            }

            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                    return Json(new { success = false, message = "Anggota tidak ditemukan." });
                TempData["Error"] = "Anggota tidak ditemukan.";
                return RedirectToAction(nameof(Index));
            }

            var currentAdmin = await _userManager.GetUserAsync(User);
            user.IsApproved = true;
            user.ApprovedAt = DateTime.Now;
            user.ApprovedByUserId = currentAdmin?.Id;
            user.RejectionReason = null;

            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                    return Json(new { success = false, message = $"Gagal menyetujui akun: {errors}" });
                TempData["Error"] = $"Gagal menyetujui akun: {errors}";
                return RedirectToAction(nameof(Index));
            }

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                return Json(new { success = true, message = $"Pendaftaran akun '{user.FullName}' ({user.Email}) berhasil disetujui." });

            // Send USER_APPROVED Email Notification (Background Safe)
            if (!string.IsNullOrWhiteSpace(user.Email))
            {
                var approveVars = new Dictionary<string, string>
                {
                    { "FullName", user.FullName },
                    { "Email", user.Email },
                    { "LoginUrl", "/Account/Login" },
                    { "CurrentDate", DateTime.Now.ToString("dd MMM yyyy HH:mm") }
                };
                _ = Task.Run(async () => await _emailService.SendEventEmailAsync("USER_APPROVED", user.Email, approveVars));
            }

            TempData["Success"] = $"Pendaftaran akun '{user.FullName}' ({user.Email}) berhasil disetujui! Akun sekarang dapat digunakan untuk login.";
            return RedirectToAction(nameof(Index));
        }

        // ── 10. ADMIN REJECT MEMBER REGISTRATION ────────────────
        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reject(string id, string? reason)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                    return Json(new { success = false, message = "ID Anggota tidak valid." });
                TempData["Error"] = "ID Anggota tidak valid.";
                return RedirectToAction(nameof(Index));
            }

            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                    return Json(new { success = false, message = "Anggota tidak ditemukan." });
                TempData["Error"] = "Anggota tidak ditemukan.";
                return RedirectToAction(nameof(Index));
            }

            if (user.IsApproved)
            {
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                    return Json(new { success = false, message = "Akun ini sudah disetujui sebelumnya." });
                TempData["Error"] = "Akun ini sudah disetujui sebelumnya. Gunakan penonaktifan atau hapus member jika diperlukan.";
                return RedirectToAction(nameof(Index));
            }

            // Send USER_REJECTED Email Notification before deleting (Background Safe)
            if (!string.IsNullOrWhiteSpace(user.Email))
            {
                var rejectVars = new Dictionary<string, string>
                {
                    { "FullName", user.FullName },
                    { "Email", user.Email },
                    { "RejectionReason", string.IsNullOrWhiteSpace(reason) ? "Kriteria pendaftaran belum terpenuhi." : reason.Trim() },
                    { "CurrentDate", DateTime.Now.ToString("dd MMM yyyy HH:mm") }
                };
                _ = Task.Run(async () => await _emailService.SendEventEmailAsync("USER_REJECTED", user.Email, rejectVars));
            }

            var result = await _userManager.DeleteAsync(user);
            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                    return Json(new { success = false, message = $"Gagal menolak pendaftaran: {errors}" });
                TempData["Error"] = $"Gagal menolak pendaftaran: {errors}";
                return RedirectToAction(nameof(Index));
            }

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                return Json(new { success = true, message = $"Pendaftaran akun '{user.FullName}' ({user.Email}) telah ditolak dan dibatalkan." });

            TempData["Success"] = $"Pendaftaran akun '{user.FullName}' ({user.Email}) telah ditolak dan dibatalkan.";
            return RedirectToAction(nameof(Index));
        }
    }
}
