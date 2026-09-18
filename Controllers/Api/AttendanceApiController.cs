using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TrackerKerja.Data;
using TrackerKerja.Helpers;
using TrackerKerja.Models;
using TrackerKerja.ViewModels;

namespace TrackerKerja.Controllers.Api
{
    [ApiController]
    [Route("api/attendance")]
    [Produces("application/json")]
    [Authorize]
    public class AttendanceApiController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly UserManager<AppUser> _userManager;

        public AttendanceApiController(AppDbContext db, UserManager<AppUser> userManager)
        {
            _db = db;
            _userManager = userManager;
        }

        /// <summary>
        /// Mengambil daftar riwayat presensi dan cuti dengan filter tanggal dan user (GET /api/attendance)
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(ApiResponse<List<AttendanceResponseDto>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAll(
            [FromQuery] string? userId,
            [FromQuery] DateTime? startDate,
            [FromQuery] DateTime? endDate,
            [FromQuery] string? type)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var isAdmin = User.IsInRole("Admin");

            var query = _db.Attendances
                .Include(a => a.User)
                .AsNoTracking()
                .AsQueryable();

            if (!isAdmin)
            {
                var companyId = currentUser?.CompanyId;
                query = query.Where(a => a.User != null && a.User.CompanyId == companyId);
            }

            if (!string.IsNullOrEmpty(userId))
                query = query.Where(a => a.UserId == userId);

            if (startDate.HasValue)
                query = query.Where(a => a.Date >= startDate.Value.Date);

            if (endDate.HasValue)
                query = query.Where(a => a.Date <= endDate.Value.Date);

            if (!string.IsNullOrEmpty(type) && Enum.TryParse<AttendanceType>(type, out var attType))
                query = query.Where(a => a.Type == attType);

            var items = await query.OrderByDescending(a => a.Date).ThenByDescending(a => a.ClockIn).ToListAsync();

            var dtos = items.Select(a => new AttendanceResponseDto
            {
                Id = a.Id,
                UserId = a.UserId,
                UserName = a.User?.UserName,
                UserFullName = a.User?.FullName,
                Date = a.Date,
                Type = a.Type.ToString(),
                TypeDisplayName = a.TypeDisplayName,
                WorkLocation = a.WorkLocation.ToString(),
                ClockIn = a.ClockIn,
                ClockOut = a.ClockOut,
                TotalHours = a.TotalHours,
                DurationFormatted = a.DurationFormatted,
                LeaveReason = a.LeaveReason,
                Notes = a.Notes,
                Status = a.Status.ToString(),
                CreatedAt = a.CreatedAt
            }).ToList();

            return Ok(ApiResponse<List<AttendanceResponseDto>>.Ok(dtos, $"Ditemukan {dtos.Count} rekaman presensi."));
        }

        /// <summary>
        /// Mengambil status presensi hari ini untuk pengguna yang sedang aktif/login (GET /api/attendance/today)
        /// </summary>
        [HttpGet("today")]
        [ProducesResponseType(typeof(ApiResponse<AttendanceResponseDto?>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetTodayStatus([FromQuery] string? userId)
        {
            var targetUserId = userId;
            if (string.IsNullOrEmpty(targetUserId))
            {
                var currentUser = await _userManager.GetUserAsync(User);
                targetUserId = currentUser?.Id;
            }

            if (string.IsNullOrEmpty(targetUserId))
            {
                return BadRequest(ApiResponse<AttendanceResponseDto?>.Fail("UserId tidak terdefinisi."));
            }

            var today = DateTimeHelper.Today;
            var record = await _db.Attendances
                .Include(a => a.User)
                .AsNoTracking()
                .FirstOrDefaultAsync(a => a.UserId == targetUserId && a.Date == today);

            if (record == null)
            {
                return Ok(ApiResponse<AttendanceResponseDto?>.Ok(null, "Belum ada presensi untuk hari ini."));
            }

            var dto = new AttendanceResponseDto
            {
                Id = record.Id,
                UserId = record.UserId,
                UserName = record.User?.UserName,
                UserFullName = record.User?.FullName,
                Date = record.Date,
                Type = record.Type.ToString(),
                TypeDisplayName = record.TypeDisplayName,
                WorkLocation = record.WorkLocation.ToString(),
                ClockIn = record.ClockIn,
                ClockOut = record.ClockOut,
                TotalHours = record.TotalHours,
                DurationFormatted = record.DurationFormatted,
                LeaveReason = record.LeaveReason,
                Notes = record.Notes,
                Status = record.Status.ToString(),
                CreatedAt = record.CreatedAt
            };

            return Ok(ApiResponse<AttendanceResponseDto?>.Ok(dto, "Status presensi hari ini berhasil dimuat."));
        }

        /// <summary>
        /// Mencatat jam kedatangan / Clock-In hari ini (POST /api/attendance/clock-in)
        /// </summary>
        [HttpPost("clock-in")]
        [ProducesResponseType(typeof(ApiResponse<AttendanceResponseDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> ClockIn([FromBody] ClockInApiRequestDto request)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                currentUser = await _db.Users.FirstOrDefaultAsync();
                if (currentUser == null) return Unauthorized();
            }

            var today = DateTimeHelper.Today;
            var nowGmt7 = DateTimeHelper.Now;
            var existing = await _db.Attendances.FirstOrDefaultAsync(a => a.UserId == currentUser.Id && a.Date == today);

            if (existing != null && existing.ClockIn.HasValue)
            {
                return BadRequest(ApiResponse<AttendanceResponseDto>.Fail($"Anda sudah melakukan clock-in hari ini pada {existing.ClockIn.Value:HH:mm}."));
            }

            var locType = WorkLocationType.WFO;
            if (!string.IsNullOrEmpty(request.WorkLocation)) Enum.TryParse(request.WorkLocation, out locType);

            if (existing == null)
            {
                existing = new AttendanceRecord
                {
                    UserId = currentUser.Id,
                    Date = today,
                    Type = AttendanceType.Present,
                    WorkLocation = locType,
                    ClockIn = nowGmt7,
                    Notes = request.Notes,
                    Status = AttendanceApprovalStatus.Approved,
                    CreatedAt = nowGmt7,
                    UpdatedAt = nowGmt7
                };
                _db.Attendances.Add(existing);
            }
            else
            {
                existing.Type = AttendanceType.Present;
                existing.WorkLocation = locType;
                existing.ClockIn = nowGmt7;
                existing.Notes = request.Notes ?? existing.Notes;
                existing.UpdatedAt = nowGmt7;
            }

            await _db.SaveChangesAsync();

            var dto = new AttendanceResponseDto
            {
                Id = existing.Id,
                UserId = existing.UserId,
                UserName = currentUser.UserName,
                UserFullName = currentUser.FullName,
                Date = existing.Date,
                Type = existing.Type.ToString(),
                TypeDisplayName = existing.TypeDisplayName,
                WorkLocation = existing.WorkLocation.ToString(),
                ClockIn = existing.ClockIn,
                ClockOut = existing.ClockOut,
                TotalHours = existing.TotalHours,
                DurationFormatted = existing.DurationFormatted,
                Notes = existing.Notes,
                Status = existing.Status.ToString(),
                CreatedAt = existing.CreatedAt
            };

            return Ok(ApiResponse<AttendanceResponseDto>.Ok(dto, $"Clock-In berhasil dicatat pada {existing.ClockIn:HH:mm}."));
        }

        /// <summary>
        /// Mencatat jam kepulangan / Clock-Out hari ini (POST /api/attendance/clock-out)
        /// </summary>
        [HttpPost("clock-out")]
        [ProducesResponseType(typeof(ApiResponse<AttendanceResponseDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> ClockOut([FromBody] ClockOutApiRequestDto request)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                currentUser = await _db.Users.FirstOrDefaultAsync();
                if (currentUser == null) return Unauthorized();
            }

            var today = DateTimeHelper.Today;
            var record = await _db.Attendances.FirstOrDefaultAsync(a => a.UserId == currentUser.Id && a.Date == today);

            if (record == null || !record.ClockIn.HasValue)
            {
                return BadRequest(ApiResponse<AttendanceResponseDto>.Fail("Anda belum melakukan clock-in hari ini."));
            }

            var now = DateTimeHelper.Now;
            record.ClockOut = now;
            var duration = (now - record.ClockIn.Value).TotalHours;
            record.TotalHours = Math.Max(0, Math.Round(duration, 2));

            if (!string.IsNullOrWhiteSpace(request.Notes))
            {
                record.Notes = string.IsNullOrEmpty(record.Notes) ? request.Notes : $"{record.Notes} | {request.Notes}";
            }
            record.UpdatedAt = now;

            await _db.SaveChangesAsync();

            var dto = new AttendanceResponseDto
            {
                Id = record.Id,
                UserId = record.UserId,
                UserName = currentUser.UserName,
                UserFullName = currentUser.FullName,
                Date = record.Date,
                Type = record.Type.ToString(),
                TypeDisplayName = record.TypeDisplayName,
                WorkLocation = record.WorkLocation.ToString(),
                ClockIn = record.ClockIn,
                ClockOut = record.ClockOut,
                TotalHours = record.TotalHours,
                DurationFormatted = record.DurationFormatted,
                Notes = record.Notes,
                Status = record.Status.ToString(),
                CreatedAt = record.CreatedAt
            };

            return Ok(ApiResponse<AttendanceResponseDto>.Ok(dto, $"Clock-Out berhasil dicatat pada {now:HH:mm}. Total jam kehadiran: {record.DurationFormatted}."));
        }

        /// <summary>
        /// Mengajukan cuti / izin untuk rentang tanggal (POST /api/attendance/leave)
        /// </summary>
        [HttpPost("leave")]
        [ProducesResponseType(typeof(ApiResponse<int>), StatusCodes.Status200OK)]
        public async Task<IActionResult> SubmitLeave([FromBody] LeaveApiRequestDto request)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var isAdmin = User.IsInRole("Admin");
            var targetUserId = request.UserId ?? currentUser?.Id;

            if (string.IsNullOrEmpty(targetUserId))
            {
                var firstUser = await _db.Users.FirstOrDefaultAsync();
                targetUserId = firstUser?.Id;
            }

            if (string.IsNullOrEmpty(targetUserId))
            {
                return BadRequest(ApiResponse<int>.Fail("Target user tidak valid."));
            }

            if (!isAdmin && targetUserId != currentUser?.Id)
            {
                var targetUser = await _db.Users.FindAsync(targetUserId);
                if (targetUser == null || targetUser.CompanyId != currentUser?.CompanyId)
                {
                    return StatusCode(StatusCodes.Status403Forbidden, ApiResponse<int>.Fail("Akses ditolak."));
                }
            }

            if (request.EndDate < request.StartDate)
            {
                return BadRequest(ApiResponse<int>.Fail("Tanggal selesai tidak boleh lebih awal dari tanggal mulai."));
            }

            var daysCount = 0;
            var curDate = request.StartDate.Date;
            var nowGmt7 = DateTimeHelper.Now;
            while (curDate <= request.EndDate.Date)
            {
                if (curDate.DayOfWeek != DayOfWeek.Sunday)
                {
                    var existing = await _db.Attendances.FirstOrDefaultAsync(a => a.UserId == targetUserId && a.Date == curDate);
                    if (existing != null)
                    {
                        existing.Type = request.Type;
                        existing.WorkLocation = WorkLocationType.WFO;
                        existing.ClockIn = null;
                        existing.ClockOut = null;
                        existing.TotalHours = 0;
                        existing.LeaveReason = request.LeaveReason;
                        existing.Notes = request.Notes;
                        existing.UpdatedAt = nowGmt7;
                    }
                    else
                    {
                        _db.Attendances.Add(new AttendanceRecord
                        {
                            UserId = targetUserId,
                            Date = curDate,
                            Type = request.Type,
                            WorkLocation = WorkLocationType.WFO,
                            LeaveReason = request.LeaveReason,
                            Notes = request.Notes,
                            Status = AttendanceApprovalStatus.Approved,
                            CreatedAt = nowGmt7,
                            UpdatedAt = nowGmt7
                        });
                    }
                    daysCount++;
                }
                curDate = curDate.AddDays(1);
            }

            await _db.SaveChangesAsync();
            return Ok(ApiResponse<int>.Ok(daysCount, $"Berhasil mencatatkan {request.LeaveReason} sebanyak {daysCount} hari kerja."));
        }
    }
}
