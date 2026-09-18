using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TrackerKerja.Data;
using TrackerKerja.Models;
using TrackerKerja.Services;
using TrackerKerja.ViewModels;

namespace TrackerKerja.Controllers.Api
{
    [ApiController]
    [Route("api/members")]
    [Produces("application/json")]
    [Authorize]
    public class MembersApiController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly UserManager<AppUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IEmailService _emailService;

        public MembersApiController(
            AppDbContext db,
            UserManager<AppUser> userManager,
            RoleManager<IdentityRole> roleManager,
            IEmailService emailService)
        {
            _db = db;
            _userManager = userManager;
            _roleManager = roleManager;
            _emailService = emailService;
        }

        /// <summary>
        /// Mengambil daftar seluruh anggota tim beserta metrik kontribusi (GET /api/members)
        /// </summary>
        /// <param name="search">Pencarian nama, email, atau jabatan</param>
        /// <param name="role">Filter peran (Admin / User)</param>
        /// <param name="companyId">Filter tim / perusahaan</param>
        /// <param name="isApproved">Filter status approval persetujuan admin</param>
        [HttpGet]
        [ProducesResponseType(typeof(ApiResponse<List<MemberResponseDto>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAll([FromQuery] string? search, [FromQuery] string? role, [FromQuery] int? companyId, [FromQuery] bool? isApproved)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var isAdmin = User.IsInRole("Admin");
            var userCompanyId = currentUser?.CompanyId;

            var usersQuery = _db.Users.Include(u => u.Company).AsQueryable();

            if (!isAdmin && currentUser != null)
            {
                usersQuery = usersQuery.Where(u => u.CompanyId == userCompanyId);
            }
            else if (companyId.HasValue)
            {
                usersQuery = usersQuery.Where(u => u.CompanyId == companyId.Value);
            }

            if (isApproved.HasValue)
            {
                usersQuery = usersQuery.Where(u => u.IsApproved == isApproved.Value);
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim().ToLower();
                usersQuery = usersQuery.Where(u =>
                    u.FullName.ToLower().Contains(s) ||
                    (u.Email != null && u.Email.ToLower().Contains(s)) ||
                    (u.JobTitle != null && u.JobTitle.ToLower().Contains(s)));
            }

            var users = await usersQuery.OrderBy(u => u.FullName).ToListAsync();
            var allTasks = await _db.Tasks.Include(t => t.Sessions).AsNoTracking().ToListAsync();
            var allNotes = await _db.Notes.AsNoTracking().ToListAsync();

            var list = new List<MemberResponseDto>();

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

                list.Add(new MemberResponseDto
                {
                    Id = user.Id,
                    FullName = user.FullName,
                    Email = user.Email ?? "",
                    JobTitle = user.JobTitle,
                    AvatarColor = user.AvatarColor,
                    ProfilePictureUrl = user.ProfilePictureUrl,
                    CompanyId = user.CompanyId,
                    CompanyName = user.Company?.Name,
                    Role = userRole,
                    Initials = user.Initials,
                    CreatedAt = user.CreatedAt,
                    IsApproved = user.IsApproved,
                    ApprovedAt = user.ApprovedAt,
                    ApprovedByUserId = user.ApprovedByUserId,
                    RejectionReason = user.RejectionReason,
                    TotalTasks = userTasks.Count,
                    ActiveTasks = userTasks.Count(t => t.Status != Models.TaskStatus.Done),
                    DoneTasks = userTasks.Count(t => t.Status == Models.TaskStatus.Done),
                    TotalHoursWorked = totalHours,
                    NotesContributedCount = userNotesCount
                });
            }

            return Ok(ApiResponse<List<MemberResponseDto>>.Ok(list, $"Berhasil mengambil {list.Count} anggota tim."));
        }

        /// <summary>
        /// Mengambil detail satu anggota tim berdasarkan ID (GET /api/members/{id})
        /// </summary>
        /// <param name="id">ID Pengguna</param>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(ApiResponse<MemberResponseDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetById(string id)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var isAdmin = User.IsInRole("Admin");

            var user = await _db.Users.Include(u => u.Company).FirstOrDefaultAsync(u => u.Id == id);
            if (user == null)
            {
                return NotFound(ApiResponse<MemberResponseDto>.Fail($"Anggota dengan ID '{id}' tidak ditemukan."));
            }

            if (!TaskPermissionHelper.CanAccessCompany(currentUser, isAdmin, user.CompanyId))
            {
                return StatusCode(StatusCodes.Status403Forbidden, ApiResponse<MemberResponseDto>.Fail("Akses ditolak: Anggota ini berasal dari tim/perusahaan lain."));
            }

            var roles = await _userManager.GetRolesAsync(user);
            var userRole = roles.FirstOrDefault() ?? "User";

            var userTasks = await _db.Tasks.Include(t => t.Sessions).Where(t => t.AssignedToUserId == user.Id).AsNoTracking().ToListAsync();
            var userNotesCount = await _db.Notes.CountAsync(n => n.AuthorUserId == user.Id);
            var totalSecs = userTasks.SelectMany(t => t.Sessions).Sum(s => s.DurationSeconds);
            var totalHours = Math.Round(totalSecs / 3600.0, 1);

            var dto = new MemberResponseDto
            {
                Id = user.Id,
                FullName = user.FullName,
                Email = user.Email ?? "",
                JobTitle = user.JobTitle,
                AvatarColor = user.AvatarColor,
                ProfilePictureUrl = user.ProfilePictureUrl,
                CompanyId = user.CompanyId,
                CompanyName = user.Company?.Name,
                Role = userRole,
                Initials = user.Initials,
                CreatedAt = user.CreatedAt,
                IsApproved = user.IsApproved,
                ApprovedAt = user.ApprovedAt,
                ApprovedByUserId = user.ApprovedByUserId,
                RejectionReason = user.RejectionReason,
                TotalTasks = userTasks.Count,
                ActiveTasks = userTasks.Count(t => t.Status != Models.TaskStatus.Done),
                DoneTasks = userTasks.Count(t => t.Status == Models.TaskStatus.Done),
                TotalHoursWorked = totalHours,
                NotesContributedCount = userNotesCount
            };

            return Ok(ApiResponse<MemberResponseDto>.Ok(dto, "Detail anggota tim berhasil diambil."));
        }

        /// <summary>
        /// Mendaftarkan anggota tim baru (POST /api/members)
        /// </summary>
        /// <param name="dto">Payload anggota tim baru</param>
        [HttpPost]
        [ProducesResponseType(typeof(ApiResponse<MemberResponseDto>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Create([FromBody] CreateMemberRequestDto dto)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var isAdmin = User.IsInRole("Admin");
            var userCompanyId = currentUser?.CompanyId;

            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                return BadRequest(ApiResponse<MemberResponseDto>.Fail("Validasi gagal.", errors));
            }

            var existingUser = await _userManager.FindByEmailAsync(dto.Email);
            if (existingUser != null)
            {
                return BadRequest(ApiResponse<MemberResponseDto>.Fail($"Email '{dto.Email}' sudah terdaftar."));
            }

            var role = string.Equals(dto.Role, "Admin", StringComparison.OrdinalIgnoreCase) ? "Admin" : "User";
            if (!await _roleManager.RoleExistsAsync(role))
            {
                await _roleManager.CreateAsync(new IdentityRole(role));
            }

            var newUser = new AppUser
            {
                UserName = dto.Email.Trim(),
                Email = dto.Email.Trim(),
                FullName = dto.FullName.Trim(),
                JobTitle = dto.JobTitle.Trim(),
                AvatarColor = string.IsNullOrWhiteSpace(dto.AvatarColor) ? "#6366F1" : dto.AvatarColor.Trim(),
                CompanyId = userCompanyId,
                CreatedAt = DateTime.Now,
                EmailConfirmed = true,
                IsApproved = true,
                ApprovedAt = DateTime.Now,
                ApprovedByUserId = currentUser?.Id
            };

            var result = await _userManager.CreateAsync(newUser, dto.Password);
            if (!result.Succeeded)
            {
                var errors = result.Errors.Select(e => e.Description).ToList();
                return BadRequest(ApiResponse<MemberResponseDto>.Fail("Gagal membuat user baru.", errors));
            }

            await _userManager.AddToRoleAsync(newUser, role);

            var company = userCompanyId.HasValue ? await _db.Companies.FindAsync(userCompanyId.Value) : null;

            var responseDto = new MemberResponseDto
            {
                Id = newUser.Id,
                FullName = newUser.FullName,
                Email = newUser.Email,
                JobTitle = newUser.JobTitle,
                AvatarColor = newUser.AvatarColor,
                CompanyId = userCompanyId,
                CompanyName = company?.Name,
                Role = role,
                Initials = newUser.Initials,
                CreatedAt = newUser.CreatedAt
            };

            return CreatedAtAction(nameof(GetById), new { id = newUser.Id }, ApiResponse<MemberResponseDto>.Ok(responseDto, "Anggota tim berhasil ditambahkan."));
        }

        /// <summary>
        /// Memperbarui informasi anggota tim (PUT /api/members/{id})
        /// </summary>
        /// <param name="id">ID Pengguna</param>
        /// <param name="dto">Payload update data anggota</param>
        [HttpPut("{id}")]
        [ProducesResponseType(typeof(ApiResponse<MemberResponseDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Update(string id, [FromBody] UpdateMemberRequestDto dto)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var isAdmin = User.IsInRole("Admin");

            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                return BadRequest(ApiResponse<MemberResponseDto>.Fail("Validasi gagal.", errors));
            }

            var user = await _db.Users.Include(u => u.Company).FirstOrDefaultAsync(u => u.Id == id);
            if (user == null)
            {
                return NotFound(ApiResponse<MemberResponseDto>.Fail($"Anggota dengan ID '{id}' tidak ditemukan."));
            }

            if (!TaskPermissionHelper.CanAccessCompany(currentUser, isAdmin, user.CompanyId))
            {
                return StatusCode(StatusCodes.Status403Forbidden, ApiResponse<MemberResponseDto>.Fail("Akses ditolak: Anda tidak memiliki wewenang untuk mengubah data pengguna ini."));
            }

            user.FullName = dto.FullName.Trim();
            user.JobTitle = dto.JobTitle.Trim();
            user.AvatarColor = string.IsNullOrWhiteSpace(dto.AvatarColor) ? "#6366F1" : dto.AvatarColor.Trim();

            var updateRes = await _userManager.UpdateAsync(user);
            if (!updateRes.Succeeded)
            {
                var errors = updateRes.Errors.Select(e => e.Description).ToList();
                return BadRequest(ApiResponse<MemberResponseDto>.Fail("Gagal memperbarui user.", errors));
            }

            if (!string.IsNullOrWhiteSpace(dto.Role))
            {
                var targetRole = string.Equals(dto.Role, "Admin", StringComparison.OrdinalIgnoreCase) ? "Admin" : "User";
                var currentRoles = await _userManager.GetRolesAsync(user);
                await _userManager.RemoveFromRolesAsync(user, currentRoles);
                await _userManager.AddToRoleAsync(user, targetRole);
            }

            var currentRole = (await _userManager.GetRolesAsync(user)).FirstOrDefault() ?? "User";

            var responseDto = new MemberResponseDto
            {
                Id = user.Id,
                FullName = user.FullName,
                Email = user.Email ?? "",
                JobTitle = user.JobTitle,
                AvatarColor = user.AvatarColor,
                CompanyId = user.CompanyId,
                CompanyName = user.Company?.Name,
                Role = currentRole,
                Initials = user.Initials,
                CreatedAt = user.CreatedAt
            };

            return Ok(ApiResponse<MemberResponseDto>.Ok(responseDto, "Data anggota tim berhasil diperbarui."));
        }

        /// <summary>
        /// Menghapus anggota tim (DELETE /api/members/{id})
        /// </summary>
        /// <param name="id">ID Pengguna</param>
        [HttpDelete("{id}")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Delete(string id)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var isAdmin = User.IsInRole("Admin");

            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound(ApiResponse<object>.Fail($"Anggota dengan ID '{id}' tidak ditemukan."));
            }

            if (!TaskPermissionHelper.CanAccessCompany(currentUser, isAdmin, user.CompanyId))
            {
                return StatusCode(StatusCodes.Status403Forbidden, ApiResponse<object>.Fail("Akses ditolak: Anda tidak memiliki wewenang untuk menghapus pengguna ini."));
            }

            // Cegah penghapusan jika ini satu-satunya Admin
            var roles = await _userManager.GetRolesAsync(user);
            if (roles.Contains("Admin"))
            {
                var admins = await _userManager.GetUsersInRoleAsync("Admin");
                if (admins.Count <= 1)
                {
                    return BadRequest(ApiResponse<object>.Fail("Tidak dapat menghapus satu-satunya akun Administrator pada sistem."));
                }
            }

            // Lepaskan relasi tasks dan notes
            var assignedTasks = await _db.Tasks.Where(t => t.AssignedToUserId == user.Id).ToListAsync();
            foreach (var task in assignedTasks)
            {
                task.AssignedToUserId = null;
            }

            var userNotes = await _db.Notes.Where(n => n.AuthorUserId == user.Id).ToListAsync();
            foreach (var note in userNotes)
            {
                note.AuthorUserId = null;
            }

            await _db.SaveChangesAsync();

            var result = await _userManager.DeleteAsync(user);
            if (!result.Succeeded)
            {
                var errors = result.Errors.Select(e => e.Description).ToList();
                return BadRequest(ApiResponse<object>.Fail("Gagal menghapus user.", errors));
            }

            return Ok(ApiResponse<object>.Ok(new { id = id }, $"Anggota tim '{user.FullName}' berhasil dihapus."));
        }

        /// <summary>
        /// Mengunci atau membuka kunci status akun anggota tim (POST /api/members/{id}/toggle-lock)
        /// </summary>
        /// <param name="id">ID Pengguna</param>
        [HttpPost("{id}/toggle-lock")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> ToggleLock(string id)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var isAdmin = User.IsInRole("Admin");

            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound(ApiResponse<object>.Fail($"Anggota dengan ID '{id}' tidak ditemukan."));
            }

            if (!TaskPermissionHelper.CanAccessCompany(currentUser, isAdmin, user.CompanyId))
            {
                return StatusCode(StatusCodes.Status403Forbidden, ApiResponse<object>.Fail("Akses ditolak."));
            }

            var isLocked = await _userManager.IsLockedOutAsync(user);
            if (isLocked)
            {
                await _userManager.SetLockoutEndDateAsync(user, null);
                return Ok(ApiResponse<object>.Ok(new { id = id, isLocked = false }, $"Akun '{user.FullName}' berhasil dibuka kuncinya."));
            }
            else
            {
                await _userManager.SetLockoutEndDateAsync(user, DateTimeOffset.UtcNow.AddYears(100));
                return Ok(ApiResponse<object>.Ok(new { id = id, isLocked = true }, $"Akun '{user.FullName}' berhasil dinonaktifkan / dikunci."));
            }
        }

        /// <summary>
        /// Mengambil ringkasan kontribusi dan beban kerja anggota tim (GET /api/members/{id}/contributions)
        /// </summary>
        /// <param name="id">ID Pengguna</param>
        [HttpGet("{id}/contributions")]
        [ProducesResponseType(typeof(ApiResponse<MemberWorkloadReportDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetContributions(string id)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var isAdmin = User.IsInRole("Admin");

            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound(ApiResponse<object>.Fail($"Anggota dengan ID '{id}' tidak ditemukan."));
            }

            if (!TaskPermissionHelper.CanAccessCompany(currentUser, isAdmin, user.CompanyId))
            {
                return StatusCode(StatusCodes.Status403Forbidden, ApiResponse<object>.Fail("Akses ditolak."));
            }

            var tasks = await _db.Tasks
                .Include(t => t.Sessions)
                .Where(t => t.AssignedToUserId == id)
                .AsNoTracking()
                .ToListAsync();

            var totalSecs = tasks.SelectMany(t => t.Sessions).Sum(s => s.Duration);

            var report = new MemberWorkloadReportDto
            {
                MemberId = user.Id,
                MemberName = user.FullName,
                JobTitle = user.JobTitle,
                TodoTasks = tasks.Count(t => t.Status == Models.TaskStatus.Todo),
                InProgressTasks = tasks.Count(t => t.Status == Models.TaskStatus.InProgress),
                DoneTasks = tasks.Count(t => t.Status == Models.TaskStatus.Done),
                TotalTasks = tasks.Count,
                TotalHours = Math.Round(totalSecs / 3600.0, 1)
            };

            return Ok(ApiResponse<MemberWorkloadReportDto>.Ok(report, $"Statistik kontribusi untuk '{user.FullName}' berhasil diambil."));
        }

        /// <summary>
        /// Mengubah password anggota tim secara langsung oleh Administrator (POST /api/members/{id}/reset-password)
        /// </summary>
        /// <param name="id">ID Pengguna</param>
        /// <param name="dto">Payload password baru</param>
        [HttpPost("{id}/reset-password")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> ResetPassword(string id, [FromBody] AdminResetMemberPasswordDto dto)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                return BadRequest(ApiResponse<object>.Fail("Validasi gagal.", errors));
            }

            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound(ApiResponse<object>.Fail($"Anggota dengan ID '{id}' tidak ditemukan."));
            }

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var result = await _userManager.ResetPasswordAsync(user, token, dto.NewPassword);

            if (!result.Succeeded)
            {
                var errors = result.Errors.Select(e => e.Description).ToList();
                return BadRequest(ApiResponse<object>.Fail("Gagal mengubah password.", errors));
            }

            // Send Password Reset Notification Email (Background Safe)
            if (!string.IsNullOrWhiteSpace(user.Email))
            {
                var resetVars = new Dictionary<string, string>
                {
                    { "FullName", user.FullName },
                    { "Email", user.Email },
                    { "NewPassword", dto.NewPassword },
                    { "LoginUrl", "/Account/Login" },
                    { "CurrentDate", DateTime.Now.ToString("dd MMM yyyy HH:mm") }
                };
                _ = Task.Run(async () => await _emailService.SendEventEmailAsync("PASSWORD_RESET_NOTIFICATION", user.Email, resetVars));
            }

            return Ok(ApiResponse<object>.Ok(new { id = user.Id, email = user.Email }, $"Password untuk '{user.FullName}' berhasil diubah secara langsung."));
        }

        /// <summary>
        /// Menyetujui pendaftaran akun member baru (POST /api/members/{id}/approve) - Khusus Admin
        /// </summary>
        /// <param name="id">ID Pengguna</param>
        [HttpPost("{id}/approve")]
        [HttpPut("{id}/approve")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Approve(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound(ApiResponse<object>.Fail($"Anggota dengan ID '{id}' tidak ditemukan."));
            }

            var currentAdmin = await _userManager.GetUserAsync(User);
            user.IsApproved = true;
            user.ApprovedAt = DateTime.Now;
            user.ApprovedByUserId = currentAdmin?.Id;
            user.RejectionReason = null;

            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                var errors = result.Errors.Select(e => e.Description).ToList();
                return BadRequest(ApiResponse<object>.Fail("Gagal menyetujui akun anggota.", errors));
            }

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

            return Ok(ApiResponse<object>.Ok(new
            {
                user.Id,
                user.Email,
                user.FullName,
                user.IsApproved,
                user.ApprovedAt
            }, $"Pendaftaran akun '{user.FullName}' ({user.Email}) berhasil disetujui. Akun sekarang dapat digunakan untuk login."));
        }

        /// <summary>
        /// Menolak pendaftaran akun member baru (POST /api/members/{id}/reject) - Khusus Admin
        /// </summary>
        /// <param name="id">ID Pengguna</param>
        /// <param name="body">Alasan penolakan opsional</param>
        [HttpPost("{id}/reject")]
        [HttpPut("{id}/reject")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Reject(string id, [FromBody] RejectMemberRequestDto? body = null)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound(ApiResponse<object>.Fail($"Anggota dengan ID '{id}' tidak ditemukan."));
            }

            if (user.IsApproved)
            {
                return BadRequest(ApiResponse<object>.Fail("Akun ini sudah disetujui sebelumnya. Gunakan penonaktifan akun atau hapus member jika diperlukan."));
            }

            // Send USER_REJECTED Email Notification before deleting (Background Safe)
            if (!string.IsNullOrWhiteSpace(user.Email))
            {
                var rejectVars = new Dictionary<string, string>
                {
                    { "FullName", user.FullName },
                    { "Email", user.Email },
                    { "RejectionReason", string.IsNullOrWhiteSpace(body?.Reason) ? "Kriteria pendaftaran belum terpenuhi." : body.Reason.Trim() },
                    { "CurrentDate", DateTime.Now.ToString("dd MMM yyyy HH:mm") }
                };
                _ = Task.Run(async () => await _emailService.SendEventEmailAsync("USER_REJECTED", user.Email, rejectVars));
            }

            var result = await _userManager.DeleteAsync(user);
            if (!result.Succeeded)
            {
                var errors = result.Errors.Select(e => e.Description).ToList();
                return BadRequest(ApiResponse<object>.Fail("Gagal menolak pendaftaran anggota.", errors));
            }

            return Ok(ApiResponse<object>.Ok(new
            {
                id,
                user.Email,
                user.FullName
            }, $"Pendaftaran akun '{user.FullName}' ({user.Email}) telah ditolak dan dibatalkan."));
        }
    }
}
