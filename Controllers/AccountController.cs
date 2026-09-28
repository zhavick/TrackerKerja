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
                if (await _userManager.IsLockedOutAsync(existingUser))
                {
                    var lockoutEnd = await _userManager.GetLockoutEndDateAsync(existingUser);
                    var remaining = lockoutEnd.HasValue ? Math.Max(1, (int)Math.Ceiling((lockoutEnd.Value - DateTimeOffset.UtcNow).TotalMinutes)) : 30;
                    ModelState.AddModelError("", $"Akun Anda sedang DIKUNCI demi alasan keamanan (sisa waktu: {remaining} menit) karena token reset kedaluwarsa atau kesalahan re-entry. Silakan hubungi Administrator.");
                    return View(model);
                }

                var isPasswordCorrect = await _userManager.CheckPasswordAsync(existingUser, model.Password);
                if (isPasswordCorrect && !existingUser.IsApproved)
                {
                    ModelState.AddModelError("", "Akun Anda sedang menunggu persetujuan (approval) dari Administrator sebelum dapat digunakan untuk login.");
                    return View(model);
                }
            }

            var userName = existingUser?.UserName ?? model.Email;
            var result = await _signInManager.PasswordSignInAsync(
                userName, model.Password, model.RememberMe, lockoutOnFailure: true);

            if (result.Succeeded)
            {
                var user = existingUser ?? await _userManager.FindByEmailAsync(model.Email.Trim());
                if (user != null)
                {
                    var roles = await _userManager.GetRolesAsync(user);
                    // Generate JWT with 1 hour (60 minutes) expiration for security auto-logout
                    var token = _jwtService.GenerateToken(user, roles, out var expiresAt, expiryMinutes: 60);
                    Response.Cookies.Append("jwt_token", token, new CookieOptions
                    {
                        HttpOnly = true,
                        Secure = Request.IsHttps,
                        SameSite = SameSiteMode.Lax,
                        Expires = expiresAt
                    });

                    // Log audit trail and evaluate gamification
                    try
                    {
                        var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
                        _db.AuditLogs.Add(new AuditLog
                        {
                            UserId = user.Id,
                            UserEmail = user.Email,
                            UserName = user.UserName ?? user.FullName,
                            ControllerName = "Account",
                            ActionName = "Login",
                            HttpMethod = "POST",
                            Path = "/Account/Login",
                            IpAddress = ip,
                            StatusCode = 200,
                            DurationMs = 0,
                            Timestamp = DateTime.Now,
                            Details = $"Pengguna {user.FullName} ({user.Email}) berhasil masuk ke sistem."
                        });
                        await _db.SaveChangesAsync();
                        await _gamificationService.EvaluateAndAwardBadgesAsync(user.Id);
                    }
                    catch { }
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
            var user = await _userManager.GetUserAsync(User);
            if (user != null)
            {
                try
                {
                    var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
                    _db.AuditLogs.Add(new AuditLog
                    {
                        UserId = user.Id,
                        UserEmail = user.Email,
                        UserName = user.UserName ?? user.FullName,
                        ControllerName = "Account",
                        ActionName = "Logout",
                        HttpMethod = "POST",
                        Path = "/Account/Logout",
                        IpAddress = ip,
                        StatusCode = 200,
                        DurationMs = 0,
                        Timestamp = DateTime.Now,
                        Details = $"Pengguna {user.FullName} ({user.Email}) berhasil keluar dari sistem."
                    });
                    await _db.SaveChangesAsync();
                    await _gamificationService.EvaluateAndAwardBadgesAsync(user.Id);
                }
                catch { }
            }

            await _signInManager.SignOutAsync();
            Response.Cookies.Delete("jwt_token");
            return RedirectToAction("Login");
        }

        [HttpGet]
        public async Task<IActionResult> Logout(string? reason = null)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user != null)
            {
                try
                {
                    var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
                    _db.AuditLogs.Add(new AuditLog
                    {
                        UserId = user.Id,
                        UserEmail = user.Email,
                        UserName = user.UserName ?? user.FullName,
                        ControllerName = "Account",
                        ActionName = "Logout",
                        HttpMethod = "GET",
                        Path = "/Account/Logout",
                        IpAddress = ip,
                        StatusCode = 200,
                        DurationMs = 0,
                        Timestamp = DateTime.Now,
                        Details = $"Pengguna {user.FullName} ({user.Email}) keluar dari sistem (Reason: {reason ?? "manual"})."
                    });
                    await _db.SaveChangesAsync();
                    await _gamificationService.EvaluateAndAwardBadgesAsync(user.Id);
                }
                catch { }
            }

            await _signInManager.SignOutAsync();
            Response.Cookies.Delete("jwt_token");
            if (string.Equals(reason, "timeout", StringComparison.OrdinalIgnoreCase))
            {
                TempData["Warning"] = "Sesi Anda telah berakhir setelah 1 jam tidak aktif demi alasan keamanan. Silakan masuk kembali.";
            }
            return RedirectToAction("Login");
        }

        // ── KEEP ALIVE (SESSION EXTENSION) ─────────────────────
        [HttpGet]
        [Authorize]
        public IActionResult KeepAlive()
        {
            return Json(new { status = "active", timestamp = DateTime.UtcNow });
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

        // ── FORGOT PASSWORD ───────────────────────────────────────────
        [HttpGet]
        public IActionResult ForgotPassword()
        {
            if (User.Identity?.IsAuthenticated == true) return RedirectToAction("Index", "Home");
            return View(new ForgotPasswordViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var user = await _userManager.FindByEmailAsync(model.Email.Trim());
            if (user == null)
            {
                ModelState.AddModelError("Email", "Alamat email tidak terdaftar dalam sistem TrackerKerja.");
                return View(model);
            }

            if (await _userManager.IsLockedOutAsync(user))
            {
                var lockoutEnd = await _userManager.GetLockoutEndDateAsync(user);
                var remaining = lockoutEnd.HasValue ? Math.Max(1, (int)Math.Ceiling((lockoutEnd.Value - DateTimeOffset.UtcNow).TotalMinutes)) : 30;
                ModelState.AddModelError("", $"Akun ini sedang DIKUNCI demi alasan keamanan ({remaining} menit tersisa) karena token kedaluwarsa atau salah re-entry berulang. Silakan hubungi Administrator.");
                return View(model);
            }

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var resetUrl = Url.Action("ResetPassword", "Account", new { email = user.Email, token = token }, Request.Scheme) 
                           ?? $"{Request.Scheme}://{Request.Host}/Account/ResetPassword?email={Uri.EscapeDataString(user.Email!)}&token={Uri.EscapeDataString(token)}";

            var emailConfig = await _emailService.GetEmailConfigAsync();
            bool isEmailConfigured = emailConfig.IsEnabled && 
                                     !string.IsNullOrWhiteSpace(emailConfig.SenderPassword) && 
                                     !string.IsNullOrWhiteSpace(emailConfig.SmtpHost);

            if (isEmailConfigured)
            {
                try
                {
                    await _emailService.SendRawEmailAsync(
                        user.Email!, 
                        user.FullName, 
                        "[Work Tracker Pro] Permintaan Reset Kata Sandi Akun",
                        $@"<div style=""font-family:'Inter',sans-serif;max-width:600px;margin:0 auto;padding:24px;border:1px solid #e2e8f0;border-radius:16px;background:#ffffff;"">
                            <h2 style=""color:#4f46e5;margin-top:0;"">Reset Kata Sandi Akun</h2>
                            <p>Halo <strong>{user.FullName}</strong>,</p>
                            <p>Kami menerima permintaan untuk mengatur ulang kata sandi akun TrackerKerja Anda.</p>
                            <p>Silakan klik tombol di bawah ini untuk mengatur kata sandi baru Anda (berlaku selama 15 menit):</p>
                            <div style=""margin:24px 0;"">
                                <a href=""{resetUrl}"" style=""background:#4f46e5;color:#ffffff;padding:12px 24px;border-radius:10px;text-decoration:none;font-weight:bold;display:inline-block;"">Reset Kata Sandi Sekarang &rarr;</a>
                            </div>
                            <p style=""font-size:12px;color:#64748b;"">Peringatan Keamanan: Jika tautan kedaluwarsa (15 menit) atau terjadi salah re-entry berulang, akun Anda akan otomatis dikunci selama 30 menit.</p>
                            <p style=""font-size:12px;color:#94a3b8;border-top:1px solid #f1f5f9;padding-top:12px;"">Jika Anda tidak meminta pengaturan ulang kata sandi, abaikan email ini.</p>
                        </div>");
                }
                catch
                {
                    // Fallback to claim link if email fails
                    isEmailConfigured = false;
                }
            }

            // Log Audit Trail
            _db.AuditLogs.Add(new AuditLog
            {
                UserId = user.Id,
                UserEmail = user.Email,
                UserName = user.UserName ?? user.FullName,
                ControllerName = "Account",
                ActionName = "ForgotPasswordRequest",
                HttpMethod = "POST",
                Path = "/Account/ForgotPassword",
                StatusCode = 200,
                Details = isEmailConfigured 
                    ? $"Permintaan reset kata sandi terkirim ke email {user.Email}" 
                    : $"Permintaan reset kata sandi menggunakan Tautan Klaim Pengguna (User Claim Link) untuk {user.Email} karena email SMTP belum dikonfigurasi.",
                IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1",
                Timestamp = DateTime.Now
            });
            await _db.SaveChangesAsync();

            var claimVm = new PasswordResetClaimViewModel
            {
                Email = user.Email!,
                FullName = user.FullName,
                Token = token,
                ClaimUrl = resetUrl,
                GeneratedAt = DateTime.Now,
                ExpiresAt = DateTime.Now.AddMinutes(15),
                IsEmailConfigured = isEmailConfigured,
                Message = isEmailConfigured 
                    ? $"Instruksi dan tautan reset kata sandi telah dikirimkan ke email {user.Email}. Sebagai alternatif keamanan, Anda juga dapat mengakses tautan klaim langsung di bawah ini." 
                    : "Server email (SMTP) saat ini belum dikonfigurasi. Anda dapat melanjutkan proses pengaturan ulang kata sandi secara langsung menggunakan Tautan Klaim Pengguna (User Claim Link) yang dibuat secara aman di bawah ini."
            };

            return View("PasswordResetClaim", claimVm);
        }

        // ── RESET PASSWORD FORM ────────────────────────────────────────
        [HttpGet]
        public async Task<IActionResult> ResetPassword(string? email, string? token)
        {
            if (User.Identity?.IsAuthenticated == true) return RedirectToAction("Index", "Home");

            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(token))
            {
                TempData["Error"] = "Tautan reset kata sandi tidak valid atau parameter tidak lengkap.";
                return RedirectToAction("Login");
            }

            var user = await _userManager.FindByEmailAsync(email.Trim());
            if (user == null)
            {
                TempData["Error"] = "Pengguna tidak ditemukan dalam sistem.";
                return RedirectToAction("Login");
            }

            if (await _userManager.IsLockedOutAsync(user))
            {
                var lockoutEnd = await _userManager.GetLockoutEndDateAsync(user);
                var remaining = lockoutEnd.HasValue ? Math.Max(1, (int)Math.Ceiling((lockoutEnd.Value - DateTimeOffset.UtcNow).TotalMinutes)) : 30;
                TempData["Error"] = $"Akun ini sedang DIKUNCI demi alasan keamanan ({remaining} menit tersisa) karena token kedaluwarsa atau salah re-entry berulang. Silakan hubungi Administrator.";
                return RedirectToAction("Login");
            }

            var model = new ResetPasswordViewModel
            {
                Email = email.Trim(),
                Token = token
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var user = await _userManager.FindByEmailAsync(model.Email.Trim());
            if (user == null)
            {
                ModelState.AddModelError("", "Pengguna tidak ditemukan.");
                return View(model);
            }

            if (await _userManager.IsLockedOutAsync(user))
            {
                var lockoutEnd = await _userManager.GetLockoutEndDateAsync(user);
                var remaining = lockoutEnd.HasValue ? Math.Max(1, (int)Math.Ceiling((lockoutEnd.Value - DateTimeOffset.UtcNow).TotalMinutes)) : 30;
                ModelState.AddModelError("", $"Akun Anda sedang DIKUNCI demi keamanan ({remaining} menit tersisa). Silakan hubungi Administrator.");
                return View(model);
            }

            // Check password confirmation re-entry
            if (model.Password != model.ConfirmPassword)
            {
                await _userManager.AccessFailedAsync(user);
                var failedAttempts = await _userManager.GetAccessFailedCountAsync(user);
                if (failedAttempts >= 3)
                {
                    await _userManager.SetLockoutEnabledAsync(user, true);
                    await _userManager.SetLockoutEndDateAsync(user, DateTimeOffset.UtcNow.AddMinutes(30));
                    _db.AuditLogs.Add(new AuditLog
                    {
                        UserId = user.Id,
                        UserEmail = user.Email,
                        UserName = user.UserName ?? user.FullName,
                        ControllerName = "Account",
                        ActionName = "PasswordResetLockout",
                        HttpMethod = "POST",
                        Path = "/Account/ResetPassword",
                        StatusCode = 403,
                        Details = $"Akun {user.Email} DIKUNCI otomatis 30 menit karena kesalahan konfirmasi password berulang.",
                        IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1",
                        Timestamp = DateTime.Now
                    });
                    await _db.SaveChangesAsync();

                    ModelState.AddModelError("", "Akun Anda telah DIKUNCI otomatis selama 30 menit demi keamanan karena salah re-entry password melebihi batas percobaan. Silakan hubungi Administrator.");
                    return View(model);
                }

                ModelState.AddModelError("ConfirmPassword", $"Konfirmasi kata sandi tidak cocok. Percobaan gagal: {failedAttempts}/3.");
                return View(model);
            }

            // Reset password using token
            var result = await _userManager.ResetPasswordAsync(user, model.Token, model.Password);
            if (!result.Succeeded)
            {
                await _userManager.AccessFailedAsync(user);
                var failedAttempts = await _userManager.GetAccessFailedCountAsync(user);
                var isTokenError = result.Errors.Any(e => e.Code.Contains("Token", StringComparison.OrdinalIgnoreCase) || 
                                                          e.Description.Contains("token", StringComparison.OrdinalIgnoreCase) ||
                                                          e.Description.Contains("expired", StringComparison.OrdinalIgnoreCase));

                if (isTokenError || failedAttempts >= 3)
                {
                    await _userManager.SetLockoutEnabledAsync(user, true);
                    await _userManager.SetLockoutEndDateAsync(user, DateTimeOffset.UtcNow.AddMinutes(30));
                    _db.AuditLogs.Add(new AuditLog
                    {
                        UserId = user.Id,
                        UserEmail = user.Email,
                        UserName = user.UserName ?? user.FullName,
                        ControllerName = "Account",
                        ActionName = "PasswordResetLockout",
                        HttpMethod = "POST",
                        Path = "/Account/ResetPassword",
                        StatusCode = 403,
                        Details = $"Akun {user.Email} DIKUNCI otomatis 30 menit karena token kedaluwarsa atau salah re-entry.",
                        IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1",
                        Timestamp = DateTime.Now
                    });
                    await _db.SaveChangesAsync();

                    ModelState.AddModelError("", "Tautan atau token reset kata sandi telah KEDALUWARSA atau tidak valid. Sesuai kebijakan keamanan, akun Anda telah DIKUNCI selama 30 menit. Silakan hubungi Administrator untuk membuka kunci akun.");
                    return View(model);
                }

                foreach (var err in result.Errors)
                {
                    ModelState.AddModelError("", $"{err.Description} (Percobaan gagal: {failedAttempts}/3)");
                }
                return View(model);
            }

            // Success: clear lockout & failed count
            await _userManager.ResetAccessFailedCountAsync(user);
            await _userManager.SetLockoutEndDateAsync(user, null);

            _db.AuditLogs.Add(new AuditLog
            {
                UserId = user.Id,
                UserEmail = user.Email,
                UserName = user.UserName ?? user.FullName,
                ControllerName = "Account",
                ActionName = "PasswordResetSuccess",
                HttpMethod = "POST",
                Path = "/Account/ResetPassword",
                StatusCode = 200,
                Details = $"Kata sandi akun {user.Email} berhasil diatur ulang via Form Reset Password.",
                IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1",
                Timestamp = DateTime.Now
            });
            await _db.SaveChangesAsync();

            TempData["Success"] = "Kata sandi Anda berhasil diperbarui! Silakan masuk dengan kata sandi baru Anda.";
            return RedirectToAction("Login");
        }
    }
}
