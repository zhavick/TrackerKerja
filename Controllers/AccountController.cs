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
    public class AccountController : Controller
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly SignInManager<AppUser> _signInManager;
        private readonly AppDbContext _db;
        private readonly IWebHostEnvironment _env;
        private readonly IGamificationService _gamificationService;
        private readonly IEmailService _emailService;
        private readonly IJwtService _jwtService;

        public AccountController(
            UserManager<AppUser> userManager,
            SignInManager<AppUser> signInManager,
            AppDbContext db,
            IWebHostEnvironment env,
            IGamificationService gamificationService,
            IEmailService emailService,
            IJwtService jwtService)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _db = db;
            _env = env;
            _gamificationService = gamificationService;
            _emailService = emailService;
            _jwtService = jwtService;
        }

        // ── LOGIN ────────────────────────────────────────────
        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            if (User.Identity?.IsAuthenticated == true) return RedirectToAction("Index", "Home");
            return View(new LoginViewModel { ReturnUrl = returnUrl });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var existingUser = await _userManager.FindByEmailAsync(model.Email.Trim());
            if (existingUser != null)
            {
                var isPasswordCorrect = await _userManager.CheckPasswordAsync(existingUser, model.Password);
                if (isPasswordCorrect && !existingUser.IsApproved)
                {
                    ModelState.AddModelError("", "Akun Anda sedang menunggu persetujuan (approval) dari Administrator sebelum dapat digunakan untuk login.");
                    return View(model);
                }
            }

            var result = await _signInManager.PasswordSignInAsync(
                model.Email, model.Password, model.RememberMe, lockoutOnFailure: true);

            if (result.Succeeded)
            {
                var user = existingUser ?? await _userManager.FindByEmailAsync(model.Email.Trim());
                if (user != null)
                {
                    var roles = await _userManager.GetRolesAsync(user);
                    var token = _jwtService.GenerateToken(user, roles, out var expiresAt);
                    Response.Cookies.Append("jwt_token", token, new CookieOptions
                    {
                        HttpOnly = true,
                        Secure = Request.IsHttps,
                        SameSite = SameSiteMode.Lax,
                        Expires = expiresAt
                    });
                }

                if (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
                    return Redirect(model.ReturnUrl);
                return RedirectToAction("Index", "Home");
            }

            if (result.IsLockedOut)
                ModelState.AddModelError("", "Akun terkunci. Coba lagi dalam 5 menit.");
            else
                ModelState.AddModelError("", "Email atau password salah.");

            return View(model);
        }

        // ── REGISTER ────────────────────────────────────────────
        [HttpGet]
        public async Task<IActionResult> Register()
        {
            if (User.Identity?.IsAuthenticated == true) return RedirectToAction("Index", "Home");
            ViewBag.Companies = await _db.Companies.OrderBy(c => c.Name).ToListAsync();
            return View(new RegisterViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Companies = await _db.Companies.OrderBy(c => c.Name).ToListAsync();
                return View(model);
            }

            int? assignedCompanyId = null;
            string companyNameForEmail = "Pribadi / Belum Ditentukan";

            if (model.CompanyOption == "existing" && model.CompanyId.HasValue)
            {
                var comp = await _db.Companies.FindAsync(model.CompanyId.Value);
                if (comp == null)
                {
                    ModelState.AddModelError("CompanyId", "Perusahaan / Tim yang dipilih tidak ditemukan.");
                    ViewBag.Companies = await _db.Companies.OrderBy(c => c.Name).ToListAsync();
                    return View(model);
                }
                assignedCompanyId = comp.Id;
                companyNameForEmail = comp.Name;
            }
            else
            {
                // New Company Registration
                var compName = !string.IsNullOrWhiteSpace(model.NewCompanyName)
                    ? model.NewCompanyName.Trim()
                    : "Tim " + model.FullName.Trim();

                var newComp = new Company
                {
                    Name = compName,
                    Code = !string.IsNullOrWhiteSpace(model.NewCompanyCode) ? model.NewCompanyCode.Trim().ToUpper() : null,
                    CreatedAt = DateTime.Now
                };
                _db.Companies.Add(newComp);
                await _db.SaveChangesAsync();
                assignedCompanyId = newComp.Id;
                companyNameForEmail = newComp.Name;
            }

            var colors = new[] { "#6366F1", "#06B6D4", "#10B981", "#F59E0B", "#8B5CF6", "#EF4444", "#EC4899" };
            var rnd = new Random();

            var user = new AppUser
            {
                UserName = model.Email,
                Email = model.Email,
                FullName = model.FullName,
                JobTitle = model.JobTitle,
                AvatarColor = colors[rnd.Next(colors.Length)],
                CompanyId = assignedCompanyId,
                CreatedAt = DateTime.Now,
                EmailConfirmed = true,
                IsApproved = false // Requires Admin Approval
            };

            var result = await _userManager.CreateAsync(user, model.Password);
            if (result.Succeeded)
            {
                await _userManager.AddToRoleAsync(user, "User");

                // Dispatch Email Notification to User (Background Safe)
                var userRegVars = new Dictionary<string, string>
                {
                    { "FullName", user.FullName },
                    { "Email", user.Email ?? "" },
                    { "CompanyName", companyNameForEmail },
                    { "JobTitle", user.JobTitle ?? "-" },
                    { "CurrentDate", DateTime.Now.ToString("dd MMM yyyy HH:mm") }
                };
                _ = Task.Run(async () => await _emailService.SendEventEmailAsync("USER_REGISTERED", user.Email!, userRegVars));

                // Dispatch Email Alert to Admin(s)
                _ = Task.Run(async () =>
                {
                    try
                    {
                        var adminUsers = await _userManager.GetUsersInRoleAsync("Admin");
                        var adminEmails = adminUsers.Where(a => !string.IsNullOrWhiteSpace(a.Email)).Select(a => a.Email!).Distinct().ToList();
                        var adminAlertVars = new Dictionary<string, string>
                        {
                            { "FullName", user.FullName },
                            { "Email", user.Email ?? "" },
                            { "CompanyName", companyNameForEmail },
                            { "JobTitle", user.JobTitle ?? "-" },
                            { "ApprovalUrl", "/Member?approvalStatus=pending" },
                            { "CurrentDate", DateTime.Now.ToString("dd MMM yyyy HH:mm") }
                        };

                        foreach (var adminEmail in adminEmails)
                        {
                            await _emailService.SendEventEmailAsync("ADMIN_NEW_USER_ALERT", adminEmail, adminAlertVars);
                        }
                    }
                    catch { }
                });

                TempData["Info"] = $"Pendaftaran berhasil! Akun {user.FullName} ({user.Email}) saat ini sedang menunggu persetujuan (approval) dari Administrator sebelum dapat digunakan untuk login.";
                return RedirectToAction("Login");
            }

            foreach (var error in result.Errors)
                ModelState.AddModelError("", error.Description);

            ViewBag.Companies = await _db.Companies.OrderBy(c => c.Name).ToListAsync();
            return View(model);
        }

        // ── LOGOUT ────────────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            Response.Cookies.Delete("jwt_token");
            return RedirectToAction("Login");
        }

        // ── PROFILE ────────────────────────────────────────────
        [HttpGet]
        [Authorize]
        public async Task<IActionResult> Profile()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login");

            // Automatically evaluate badges on profile view
            await _gamificationService.EvaluateAndAwardBadgesAsync(user.Id);

            var totalTasks = await _db.Tasks.CountAsync(t => t.AssignedToUserId == user.Id);
            var doneTasks = await _db.Tasks.CountAsync(t => t.AssignedToUserId == user.Id && t.Status == Models.TaskStatus.Done);
            var totalProjects = await _db.Projects.CountAsync();
            var totalSeconds = await _db.Sessions.Where(s => s.UserId == user.Id && s.EndTime != null).SumAsync(s => (long?)s.Duration) ?? 0;

            var gamification = await _gamificationService.GetGamificationStatsAsync(user.Id);

            var vm = new ProfileViewModel
            {
                FullName = user.FullName,
                JobTitle = user.JobTitle,
                AvatarColor = user.AvatarColor,
                ProfilePictureUrl = user.ProfilePictureUrl,
                CoverPictureUrl = user.CoverPictureUrl,
                Email = user.Email ?? "",
                Initials = user.Initials,
                CreatedAt = user.CreatedAt,
                TotalTasks = totalTasks,
                DoneTasks = doneTasks,
                TotalProjects = totalProjects,
                TotalHours = Math.Round(totalSeconds / 3600.0, 1),
                Gamification = gamification
            };

            ViewData["Title"] = "Profil Saya";
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> Profile(ProfileViewModel model)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login");

            if (!ModelState.IsValid)
            {
                model.Email = user.Email ?? "";
                model.Initials = user.Initials;
                model.CreatedAt = user.CreatedAt;
                model.ProfilePictureUrl = user.ProfilePictureUrl;
                model.CoverPictureUrl = user.CoverPictureUrl;
                model.Gamification = await _gamificationService.GetGamificationStatsAsync(user.Id);
                return View(model);
            }

            // Handle Profile Picture Upload
            if (model.ProfilePicture != null && model.ProfilePicture.Length > 0)
            {
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp", ".gif" };
                var ext = Path.GetExtension(model.ProfilePicture.FileName).ToLower();

                if (!allowedExtensions.Contains(ext))
                {
                    TempData["Error"] = "Format foto tidak didukung. Gunakan JPG, PNG, atau WEBP.";
                    return RedirectToAction("Profile");
                }

                if (model.ProfilePicture.Length > 5 * 1024 * 1024) // 5MB limit
                {
                    TempData["Error"] = "Ukuran foto terlalu besar. Maksimal 5MB.";
                    return RedirectToAction("Profile");
                }

                var uploadsFolder = Path.Combine(_env.WebRootPath, "uploads", "avatars");
                if (!Directory.Exists(uploadsFolder))
                    Directory.CreateDirectory(uploadsFolder);

                var uniqueFileName = $"{Guid.NewGuid()}{ext}";
                var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await model.ProfilePicture.CopyToAsync(fileStream);
                }

                user.ProfilePictureUrl = $"/uploads/avatars/{uniqueFileName}";
            }

            // Handle Cover Picture Upload or Reset
            if (model.CoverPicture != null && model.CoverPicture.Length > 0)
            {
                var allowedCoverExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp", ".gif" };
                var coverExt = Path.GetExtension(model.CoverPicture.FileName).ToLower();

                if (!allowedCoverExtensions.Contains(coverExt))
                {
                    TempData["Error"] = "Format foto cover tidak didukung. Gunakan JPG, PNG, atau WEBP.";
                    return RedirectToAction("Profile");
                }

                if (model.CoverPicture.Length > 10 * 1024 * 1024) // 10MB limit
                {
                    TempData["Error"] = "Ukuran foto cover terlalu besar. Maksimal 10MB.";
                    return RedirectToAction("Profile");
                }

                var coversFolder = Path.Combine(_env.WebRootPath, "uploads", "covers");
                if (!Directory.Exists(coversFolder))
                    Directory.CreateDirectory(coversFolder);

                var uniqueCoverName = $"cover_{Guid.NewGuid()}{coverExt}";
                var coverFilePath = Path.Combine(coversFolder, uniqueCoverName);

                using (var fileStream = new FileStream(coverFilePath, FileMode.Create))
                {
                    await model.CoverPicture.CopyToAsync(fileStream);
                }

                user.CoverPictureUrl = $"/uploads/covers/{uniqueCoverName}";
            }
            else if (model.RemoveCover)
            {
                user.CoverPictureUrl = null;
            }

            user.FullName = model.FullName;
            user.JobTitle = model.JobTitle;
            user.AvatarColor = model.AvatarColor;

            await _userManager.UpdateAsync(user);

            // Re-evaluate badges after profile update
            var newBadges = await _gamificationService.EvaluateAndAwardBadgesAsync(user.Id);
            if (newBadges.Any())
            {
                TempData["Success"] = $"Profil & Cover diperbarui! 🎉 Selamat, kamu membuka badge baru: {string.Join(", ", newBadges.Select(b => b.Name))}!";
            }
            else
            {
                TempData["Success"] = "Profil & Cover berhasil diperbarui!";
            }

            return RedirectToAction("Profile");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> ToggleFeatureBadge(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login");

            await _gamificationService.ToggleFeatureBadgeAsync(user.Id, id);
            TempData["Success"] = "Status badge utama berhasil diperbarui!";
            return RedirectToAction("Profile");
        }

        // ── CHANGE PASSWORD ────────────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                TempData["Error"] = "Validasi gagal. Periksa kembali input Anda.";
                return RedirectToAction("Profile");
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login");

            var result = await _userManager.ChangePasswordAsync(user, model.CurrentPassword, model.NewPassword);
            if (result.Succeeded)
            {
                await _signInManager.RefreshSignInAsync(user);
                var roles = await _userManager.GetRolesAsync(user);
                var token = _jwtService.GenerateToken(user, roles, out var expiresAt);
                Response.Cookies.Append("jwt_token", token, new CookieOptions
                {
                    HttpOnly = true,
                    Secure = Request.IsHttps,
                    SameSite = SameSiteMode.Lax,
                    Expires = expiresAt
                });
                TempData["Success"] = "Password berhasil diubah!";
            }
            else
            {
                TempData["Error"] = string.Join(", ", result.Errors.Select(e => e.Description));
            }

            return RedirectToAction("Profile");
        }

        // ── ACCESS DENIED ──────────────────────────────────────────────
        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }
    }
}
