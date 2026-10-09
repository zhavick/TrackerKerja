using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using TrackerKerja.Data;
using TrackerKerja.Models;
using TrackerKerja.ViewModels;

namespace TrackerKerja.Controllers
{
    [Authorize]
    public class AnnouncementController : Controller
    {
        private readonly AppDbContext _db;
        private readonly UserManager<AppUser> _userManager;

        public AnnouncementController(AppDbContext db, UserManager<AppUser> userManager)
        {
            _db = db;
            _userManager = userManager;
        }

        // ══════════════════════════════════════════════════════════════════════════
        // 1. ADMIN MANAGEMENT (INDEX, CREATE, EDIT, DELETE, TOGGLE)
        // ══════════════════════════════════════════════════════════════════════════

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Index(string? search, string? statusFilter, string? activeFilter)
        {
            ViewData["Title"] = "Kelola Pengumuman";

            var today = DateTime.Today;
            var query = _db.Announcements
                .Include(a => a.CreatedByUser)
                .AsQueryable();

            // Summary counts across all announcements
            var allAnnouncements = await _db.Announcements.AsNoTracking().ToListAsync();
            var totalCount = allAnnouncements.Count;
            var activeCount = allAnnouncements.Count(a => a.IsActive && a.AnnouncementDate.Date <= today && a.EndDate.Date >= today);
            var importantCount = allAnnouncements.Count(a => a.Status == "Informasi Penting");
            var generalCount = allAnnouncements.Count(a => a.Status == "Pengumuman Umum");
            var expiredCount = allAnnouncements.Count(a => a.EndDate.Date < today);

            // Filtering
            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim().ToLower();
                query = query.Where(a => a.Title.ToLower().Contains(s) || a.Content.ToLower().Contains(s));
            }

            if (!string.IsNullOrWhiteSpace(statusFilter))
            {
                query = query.Where(a => a.Status == statusFilter);
            }

            if (!string.IsNullOrWhiteSpace(activeFilter))
            {
                if (activeFilter == "active")
                {
                    query = query.Where(a => a.IsActive && a.AnnouncementDate.Date <= today && a.EndDate.Date >= today);
                }
                else if (activeFilter == "inactive")
                {
                    query = query.Where(a => !a.IsActive);
                }
                else if (activeFilter == "expired")
                {
                    query = query.Where(a => a.EndDate.Date < today);
                }
            }

            var list = await query
                .OrderByDescending(a => a.AnnouncementDate)
                .ThenByDescending(a => a.Id)
                .ToListAsync();

            var model = new AnnouncementIndexViewModel
            {
                Announcements = list,
                Search = search,
                StatusFilter = statusFilter,
                ActiveFilter = activeFilter,
                TotalCount = totalCount,
                ActiveCount = activeCount,
                ImportantCount = importantCount,
                GeneralCount = generalCount,
                ExpiredCount = expiredCount
            };

            return View(model);
        }

        [HttpGet]
        [Authorize(Roles = "Admin")]
        public IActionResult Create()
        {
            ViewData["Title"] = "Tambah Pengumuman Baru";
            var model = new AnnouncementFormViewModel
            {
                AnnouncementDate = DateTime.Today,
                EndDate = DateTime.Today.AddDays(7),
                Status = "Pengumuman Umum",
                IsActive = true
            };
            return View(model);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(AnnouncementFormViewModel model)
        {
            if (model.EndDate < model.AnnouncementDate)
            {
                ModelState.AddModelError("EndDate", "Tanggal berakhir tidak boleh sebelum tanggal pemberitahuan / pengumuman.");
            }

            if (!ModelState.IsValid)
            {
                ViewData["Title"] = "Tambah Pengumuman Baru";
                return View(model);
            }

            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var announcement = new Announcement
            {
                Title = model.Title.Trim(),
                Content = model.Content.Trim(),
                AnnouncementDate = model.AnnouncementDate.Date,
                EndDate = model.EndDate.Date,
                Status = model.Status == "Informasi Penting" ? "Informasi Penting" : "Pengumuman Umum",
                IsActive = model.IsActive,
                CreatedByUserId = currentUserId,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };

            _db.Announcements.Add(announcement);

            // Audit log
            _db.AuditLogs.Add(new AuditLog
            {
                Timestamp = DateTime.Now,
                UserEmail = User.Identity?.Name ?? "Admin",
                HttpMethod = "POST",
                Path = "/Announcement/Create",
                ControllerName = "Announcement",
                ActionName = "Create",
                StatusCode = 200,
                DurationMs = 10,
                IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString()
            });

            await _db.SaveChangesAsync();

            TempData["SuccessMessage"] = "Pengumuman berhasil dipublikasikan dan akan muncul di popup modal seluruh pengguna.";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int id)
        {
            ViewData["Title"] = "Edit Pengumuman";
            var announcement = await _db.Announcements.FindAsync(id);
            if (announcement == null)
            {
                TempData["ErrorMessage"] = "Pengumuman tidak ditemukan.";
                return RedirectToAction(nameof(Index));
            }

            var model = new AnnouncementFormViewModel
            {
                Id = announcement.Id,
                Title = announcement.Title,
                Content = announcement.Content,
                AnnouncementDate = announcement.AnnouncementDate,
                EndDate = announcement.EndDate,
                Status = announcement.Status,
                IsActive = announcement.IsActive
            };

            return View(model);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, AnnouncementFormViewModel model)
        {
            if (model.EndDate < model.AnnouncementDate)
            {
                ModelState.AddModelError("EndDate", "Tanggal berakhir tidak boleh sebelum tanggal pemberitahuan / pengumuman.");
            }

            if (!ModelState.IsValid)
            {
                ViewData["Title"] = "Edit Pengumuman";
                return View(model);
            }

            var announcement = await _db.Announcements.FindAsync(id);
            if (announcement == null)
            {
                TempData["ErrorMessage"] = "Pengumuman tidak ditemukan.";
                return RedirectToAction(nameof(Index));
            }

            announcement.Title = model.Title.Trim();
            announcement.Content = model.Content.Trim();
            announcement.AnnouncementDate = model.AnnouncementDate.Date;
            announcement.EndDate = model.EndDate.Date;
            announcement.Status = model.Status == "Informasi Penting" ? "Informasi Penting" : "Pengumuman Umum";
            announcement.IsActive = model.IsActive;
            announcement.UpdatedAt = DateTime.Now;

            // Audit log
            _db.AuditLogs.Add(new AuditLog
            {
                Timestamp = DateTime.Now,
                UserEmail = User.Identity?.Name ?? "Admin",
                HttpMethod = "POST",
                Path = $"/Announcement/Edit/{id}",
                ControllerName = "Announcement",
                ActionName = "Edit",
                StatusCode = 200,
                DurationMs = 10,
                IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString()
            });

            await _db.SaveChangesAsync();

            TempData["SuccessMessage"] = "Pengumuman berhasil diperbarui.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var announcement = await _db.Announcements.FindAsync(id);
            if (announcement == null)
            {
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return Json(new { success = false, message = "Pengumuman tidak ditemukan." });
                }
                TempData["ErrorMessage"] = "Pengumuman tidak ditemukan.";
                return RedirectToAction(nameof(Index));
            }

            _db.Announcements.Remove(announcement);

            // Audit log
            _db.AuditLogs.Add(new AuditLog
            {
                Timestamp = DateTime.Now,
                UserEmail = User.Identity?.Name ?? "Admin",
                HttpMethod = "POST",
                Path = $"/Announcement/Delete/{id}",
                ControllerName = "Announcement",
                ActionName = "Delete",
                StatusCode = 200,
                DurationMs = 8,
                IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString()
            });

            await _db.SaveChangesAsync();

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return Json(new { success = true, message = "Pengumuman berhasil dihapus." });
            }

            TempData["SuccessMessage"] = "Pengumuman berhasil dihapus.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleActive(int id)
        {
            var announcement = await _db.Announcements.FindAsync(id);
            if (announcement == null)
            {
                return Json(new { success = false, message = "Pengumuman tidak ditemukan." });
            }

            announcement.IsActive = !announcement.IsActive;
            announcement.UpdatedAt = DateTime.Now;
            await _db.SaveChangesAsync();

            return Json(new
            {
                success = true,
                isActive = announcement.IsActive,
                message = announcement.IsActive ? "Pengumuman diaktifkan." : "Pengumuman dinonaktifkan."
            });
        }

        // ══════════════════════════════════════════════════════════════════════════
        // 2. MODAL POPUP ENDPOINT (FOR ALL AUTHENTICATED USERS)
        // ══════════════════════════════════════════════════════════════════════════

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> GetActiveAnnouncements()
        {
            var today = DateTime.Today;

            var activeList = await _db.Announcements
                .Include(a => a.CreatedByUser)
                .Where(a => a.IsActive && a.AnnouncementDate.Date <= today && a.EndDate.Date >= today)
                .OrderByDescending(a => a.Status == "Informasi Penting") // Prioritize "Informasi Penting"
                .ThenByDescending(a => a.AnnouncementDate)
                .ThenByDescending(a => a.Id)
                .AsNoTracking()
                .ToListAsync();

            var dtos = activeList.Select(a => new AnnouncementPopupDto
            {
                Id = a.Id,
                Title = a.Title,
                Content = a.Content,
                AnnouncementDate = a.AnnouncementDate.ToString("dd MMMM yyyy", new System.Globalization.CultureInfo("id-ID")),
                EndDate = a.EndDate.ToString("dd MMMM yyyy", new System.Globalization.CultureInfo("id-ID")),
                Status = a.Status,
                IsImportant = a.Status == "Informasi Penting",
                DaysRemaining = Math.Max(0, (a.EndDate.Date - today).Days),
                CreatedByName = a.CreatedByUser?.FullName ?? "Administrator"
            }).ToList();

            return Json(new
            {
                success = true,
                count = dtos.Count,
                announcements = dtos
            });
        }
    }
}
