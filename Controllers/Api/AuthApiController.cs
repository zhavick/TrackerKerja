using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TrackerKerja.Data;
using TrackerKerja.Models;
using TrackerKerja.ViewModels;
using TrackerKerja.Services;

namespace TrackerKerja.Controllers.Api
{
    /// <summary>
    /// Modul API Autentikasi Pengguna dan Profil Akun
    /// </summary>
    [ApiController]
    [Route("api/auth")]
    [Produces("application/json")]
    [Authorize]
    public class AuthApiController : ControllerBase
    {
        private readonly SignInManager<AppUser> _signInManager;
        private readonly UserManager<AppUser> _userManager;
        private readonly AppDbContext _db;
        private readonly IJwtService _jwtService;

        public AuthApiController(
            SignInManager<AppUser> signInManager,
            UserManager<AppUser> userManager,
            AppDbContext db,
            IJwtService jwtService)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _db = db;
            _jwtService = jwtService;
        }

        /// <summary>
        /// Melakukan autentikasi / login pengguna (POST /api/auth/login)
        /// </summary>
        /// <param name="dto">Kredensial email dan password</param>
        [HttpPost("login")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(ApiResponse<LoginResponseDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> Login([FromBody] LoginRequestDto dto)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                return BadRequest(ApiResponse<object>.Fail("Validasi payload gagal.", errors));
            }

            var user = await _userManager.FindByEmailAsync(dto.Email.Trim());
            if (user == null)
            {
                return Unauthorized(ApiResponse<object>.Fail("Email atau password yang Anda masukkan tidak valid."));
            }

            var isPasswordValid = await _userManager.CheckPasswordAsync(user, dto.Password);
            if (isPasswordValid && !user.IsApproved)
            {
                return StatusCode(StatusCodes.Status403Forbidden, ApiResponse<object>.Fail("Akun Anda sedang menunggu persetujuan (approval) dari Administrator sebelum dapat digunakan untuk login."));
            }

            var result = await _signInManager.PasswordSignInAsync(user.UserName!, dto.Password, dto.RememberMe, lockoutOnFailure: true);

            if (result.Succeeded)
            {
                var roles = await _userManager.GetRolesAsync(user);
                var primaryRole = roles.FirstOrDefault() ?? "User";

                // Generate JWT Token
                var token = _jwtService.GenerateToken(user, roles, out var expiresAt);

                // Set jwt_token cookie for browser clients
                Response.Cookies.Append("jwt_token", token, new CookieOptions
                {
                    HttpOnly = true,
                    Secure = Request.IsHttps,
                    SameSite = SameSiteMode.Lax,
                    Expires = expiresAt
                });

                var company = user.CompanyId.HasValue ? await _db.Companies.FindAsync(user.CompanyId.Value) : null;

                var response = new LoginResponseDto
                {
                    IsSuccess = true,
                    Token = token,
                    TokenType = "Bearer",
                    ExpiresAt = expiresAt,
                    UserId = user.Id,
                    Email = user.Email ?? "",
                    FullName = user.FullName,
                    JobTitle = user.JobTitle,
                    Role = primaryRole,
                    AvatarColor = user.AvatarColor,
                    ProfilePictureUrl = user.ProfilePictureUrl,
                    CompanyId = user.CompanyId,
                    CompanyName = company?.Name,
                    Message = $"Selamat datang, {user.FullName}!"
                };

                return Ok(ApiResponse<LoginResponseDto>.Ok(response, "Autentikasi login berhasil."));
            }

            if (result.IsLockedOut)
            {
                return StatusCode(StatusCodes.Status423Locked, ApiResponse<object>.Fail("Akun Anda sedang terkunci sementara karena terlalu banyak percobaan gagal. Silakan coba lagi nanti."));
            }

            return Unauthorized(ApiResponse<object>.Fail("Email atau password yang Anda masukkan tidak valid."));
        }

        /// <summary>
        /// Mengakhiri sesi login pengguna aktif (POST /api/auth/logout)
        /// </summary>
        [HttpPost("logout")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        public async Task<IActionResult> Logout()
        {
            Response.Cookies.Delete("jwt_token");
            await _signInManager.SignOutAsync();
            return Ok(ApiResponse<object>.Ok(new { isLoggedOut = true }, "Sesi login berhasil diakhiri."));
        }

        /// <summary>
        /// Mengambil data profil akun pengguna yang sedang login saat ini (GET /api/auth/me)
        /// </summary>
        [HttpGet("me")]
        [ProducesResponseType(typeof(ApiResponse<UserProfileDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetCurrentUser()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Unauthorized(ApiResponse<object>.Fail("Pengguna belum login atau sesi telah berakhir."));
            }

            var roles = await _userManager.GetRolesAsync(user);
            var primaryRole = roles.FirstOrDefault() ?? "User";

            var totalTasks = await _db.Tasks.CountAsync(t => t.AssignedToUserId == user.Id);
            var doneTasks = await _db.Tasks.CountAsync(t => t.AssignedToUserId == user.Id && t.Status == Models.TaskStatus.Done);
            var totalDuration = await _db.Sessions
                .Where(s => s.Task != null && s.Task.AssignedToUserId == user.Id)
                .SumAsync(s => (long?)s.Duration) ?? 0;

            var company = user.CompanyId.HasValue ? await _db.Companies.FindAsync(user.CompanyId.Value) : null;

            var profile = new UserProfileDto
            {
                Id = user.Id,
                Email = user.Email ?? "",
                FullName = user.FullName,
                JobTitle = user.JobTitle,
                PhoneNumber = user.PhoneNumber,
                Role = primaryRole,
                AvatarColor = user.AvatarColor,
                ProfilePictureUrl = user.ProfilePictureUrl,
                CoverPictureUrl = user.CoverPictureUrl,
                CompanyId = user.CompanyId,
                CompanyName = company?.Name,
                CreatedAt = user.CreatedAt,
                TotalAssignedTasks = totalTasks,
                CompletedTasks = doneTasks,
                TotalHoursLogged = Math.Round(totalDuration / 3600.0, 1)
            };

            return Ok(ApiResponse<UserProfileDto>.Ok(profile, "Data profil pengguna berhasil diambil."));
        }

        /// <summary>
        /// Mengubah password akun pengguna aktif (POST /api/auth/change-password)
        /// </summary>
        /// <param name="dto">Payload password lama dan password baru</param>
        [HttpPost("change-password")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequestDto dto)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                return BadRequest(ApiResponse<object>.Fail("Validasi payload gagal.", errors));
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Unauthorized(ApiResponse<object>.Fail("Pengguna belum login."));
            }

            var result = await _userManager.ChangePasswordAsync(user, dto.CurrentPassword, dto.NewPassword);
            if (!result.Succeeded)
            {
                var errors = result.Errors.Select(e => e.Description).ToList();
                return BadRequest(ApiResponse<object>.Fail("Gagal mengubah password.", errors));
            }

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
            return Ok(ApiResponse<object>.Ok(new { updated = true, token, tokenType = "Bearer", expiresAt }, "Password berhasil diperbarui."));
        }

        /// <summary>
        /// Memperbarui informasi profil pengguna aktif (PUT /api/auth/profile)
        /// </summary>
        /// <param name="dto">Payload pembaruan nama, jabatan, nomor telepon, dan warna avatar</param>
        [HttpPut("profile")]
        [ProducesResponseType(typeof(ApiResponse<UserProfileDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequestDto dto)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                return BadRequest(ApiResponse<object>.Fail("Validasi payload gagal.", errors));
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Unauthorized(ApiResponse<object>.Fail("Pengguna belum login."));
            }

            user.FullName = dto.FullName.Trim();
            if (!string.IsNullOrWhiteSpace(dto.JobTitle)) user.JobTitle = dto.JobTitle.Trim();
            if (!string.IsNullOrWhiteSpace(dto.PhoneNumber)) user.PhoneNumber = dto.PhoneNumber.Trim();
            if (!string.IsNullOrWhiteSpace(dto.AvatarColor)) user.AvatarColor = dto.AvatarColor.Trim();
            if (!string.IsNullOrWhiteSpace(dto.ProfilePictureUrl)) user.ProfilePictureUrl = dto.ProfilePictureUrl.Trim();
            if (dto.CoverPictureUrl != null) user.CoverPictureUrl = string.IsNullOrWhiteSpace(dto.CoverPictureUrl) ? null : dto.CoverPictureUrl.Trim();

            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                var errors = result.Errors.Select(e => e.Description).ToList();
                return BadRequest(ApiResponse<object>.Fail("Gagal memperbarui profil pengguna.", errors));
            }

            var roles = await _userManager.GetRolesAsync(user);
            var token = _jwtService.GenerateToken(user, roles, out var expiresAt);
            Response.Cookies.Append("jwt_token", token, new CookieOptions
            {
                HttpOnly = true,
                Secure = Request.IsHttps,
                SameSite = SameSiteMode.Lax,
                Expires = expiresAt
            });

            return await GetCurrentUser();
        }
    }
}
