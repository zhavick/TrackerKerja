using ClosedXML.Excel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TrackerKerja.Data;
using TrackerKerja.Helpers;
using TrackerKerja.Models;
using TrackerKerja.ViewModels;

namespace TrackerKerja.Controllers
{
    [Authorize]
    public class AttendanceController : Controller
    {
        private readonly AppDbContext _db;
        private readonly UserManager<AppUser> _userManager;

        public AttendanceController(AppDbContext db, UserManager<AppUser> userManager)
        {
            _db = db;
            _userManager = userManager;
        }

        // ── INDEX / MAIN VIEW ──────────────────────────────────────
        public async Task<IActionResult> Index(int? month, int? year, string? memberId, string? type)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return Challenge();

            var isAdmin = User.IsInRole("Admin");
            var userCompanyId = currentUser.CompanyId;

            var targetMonth = month ?? DateTimeHelper.Today.Month;
            var targetYear = year ?? DateTimeHelper.Today.Year;

            if (targetMonth < 1) targetMonth = 1;
            if (targetMonth > 12) targetMonth = 12;
            if (targetYear < 2020 || targetYear > 2050) targetYear = DateTimeHelper.Today.Year;

            var startDate = new DateTime(targetYear, targetMonth, 1);
            var endDate = startDate.AddMonths(1).AddDays(-1);

            var query = _db.Attendances
                .Include(a => a.User)
                .Include(a => a.ApprovedByUser)
                .Where(a => a.Date >= startDate && a.Date <= endDate)
                .AsQueryable();

            // Non-admin can only see their own attendance or same company
            if (!isAdmin)
            {
                memberId = currentUser.Id;
                query = query.Where(a => a.User != null && a.User.CompanyId == userCompanyId);
            }
            else if (string.IsNullOrEmpty(memberId))
            {
                // Default admin to currentUser unless specified or "all"
                memberId = currentUser.Id;
            }

            if (!string.IsNullOrEmpty(memberId) && memberId != "all")
            {
                query = query.Where(a => a.UserId == memberId);
            }

            if (!string.IsNullOrEmpty(type) && Enum.TryParse<AttendanceType>(type, out var attType))
            {
                query = query.Where(a => a.Type == attType);
            }

            var attendances = await query
                .OrderByDescending(a => a.Date)
                .ThenBy(a => a.ClockIn)
                .ToListAsync();

            // Today's record for logged-in user (GMT+7)
            var today = DateTimeHelper.Today;
            var todayRecord = await _db.Attendances
                .Include(a => a.User)
                .FirstOrDefaultAsync(a => a.UserId == currentUser.Id && a.Date == today);

            // Monthly statistics calculation
            var presentList = attendances.Where(a => a.Type == AttendanceType.Present).ToList();
            var totalPresent = presentList.Count;
            var totalWfo = presentList.Count(a => a.WorkLocation == WorkLocationType.WFO);
            var totalWfh = presentList.Count(a => a.WorkLocation == WorkLocationType.WFH);
            var totalLeave = attendances.Count(a => a.Type == AttendanceType.Leave);
            var totalSick = attendances.Count(a => a.Type == AttendanceType.Sick);
            var totalPermission = attendances.Count(a => a.Type == AttendanceType.Permission || a.Type == AttendanceType.BusinessTrip);
            var totalHours = Math.Round(presentList.Sum(a => a.TotalHours), 2);
            var avgHours = totalPresent > 0 ? Math.Round(totalHours / totalPresent, 1) : 0;

            var membersQuery = _db.Users.AsQueryable();
            if (!isAdmin)
            {
                membersQuery = membersQuery.Where(u => u.CompanyId == userCompanyId);
            }
            var allMembers = await membersQuery.OrderBy(u => u.FullName).ToListAsync();

            var viewModel = new AttendanceIndexViewModel
            {
                Month = targetMonth,
                Year = targetYear,
                SelectedMemberId = memberId,
                SelectedType = type,
                IsAdmin = isAdmin,
                CurrentUser = currentUser,
                Members = allMembers,
                Attendances = attendances,
                TodayRecord = todayRecord,
                TotalPresentDays = totalPresent,
                TotalWfoDays = totalWfo,
                TotalWfhDays = totalWfh,
                TotalLeaveDays = totalLeave,
                TotalSickDays = totalSick,
                TotalPermissionDays = totalPermission,
                TotalMonthHours = totalHours,
                AverageDailyHours = avgHours
            };

            return View(viewModel);
        }

        // ── 1-CLICK CLOCK-IN ───────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ClockIn(string location, string? notes)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return Challenge();

            var today = DateTimeHelper.Today;
            var nowGmt7 = DateTimeHelper.Now;
            var existing = await _db.Attendances.FirstOrDefaultAsync(a => a.UserId == currentUser.Id && a.Date == today);

            if (existing != null)
            {
                if (existing.ClockIn.HasValue)
                {
                    TempData["Error"] = $"Anda sudah mencatat jam kedatangan hari ini pada pukul {existing.ClockIn.Value:HH:mm}.";
                    return RedirectToAction(nameof(Index));
                }

                existing.Type = AttendanceType.Present;
                existing.ClockIn = nowGmt7;
                if (Enum.TryParse<WorkLocationType>(location, out var locType))
                {
                    existing.WorkLocation = locType;
                }
                if (!string.IsNullOrWhiteSpace(notes))
                {
                    existing.Notes = notes.Trim();
                }
                existing.UpdatedAt = nowGmt7;
            }
            else
            {
                var locType = WorkLocationType.WFO;
                if (!string.IsNullOrEmpty(location)) Enum.TryParse(location, out locType);

                var record = new AttendanceRecord
                {
                    UserId = currentUser.Id,
                    Date = today,
                    Type = AttendanceType.Present,
                    WorkLocation = locType,
                    ClockIn = nowGmt7,
                    Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim(),
                    Status = AttendanceApprovalStatus.Approved,
                    CreatedAt = nowGmt7,
                    UpdatedAt = nowGmt7
                };
                _db.Attendances.Add(record);
            }

            await _db.SaveChangesAsync();
            TempData["Success"] = $"Jam kedatangan berhasil dicatat: {nowGmt7:HH:mm} ({location}). Semangat bekerja!";
            return RedirectToAction(nameof(Index));
        }

        // ── 1-CLICK CLOCK-OUT ──────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ClockOut(string? notes)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return Challenge();

            var today = DateTimeHelper.Today;
            var record = await _db.Attendances.FirstOrDefaultAsync(a => a.UserId == currentUser.Id && a.Date == today);

            if (record == null || !record.ClockIn.HasValue)
            {
                TempData["Error"] = "Anda belum mencatat jam kedatangan hari ini. Silakan Clock-In terlebih dahulu.";
                return RedirectToAction(nameof(Index));
            }

            var now = DateTimeHelper.Now;
            record.ClockOut = now;

            var duration = (now - record.ClockIn.Value).TotalHours;
            record.TotalHours = Math.Max(0, Math.Round(duration, 2));

            if (!string.IsNullOrWhiteSpace(notes))
            {
                record.Notes = string.IsNullOrEmpty(record.Notes) ? notes.Trim() : $"{record.Notes} | {notes.Trim()}";
            }
            record.UpdatedAt = now;

            await _db.SaveChangesAsync();
            TempData["Success"] = $"Jam pulang berhasil dicatat: {now:HH:mm}. Total durasi kehadiran: {record.DurationFormatted}. Selamat beristirahat!";
            return RedirectToAction(nameof(Index));
        }

        // ── SAVE MANUAL / EDIT ATTENDANCE ──────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveManual(ManualAttendanceInputModel model)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return Challenge();

            var isAdmin = User.IsInRole("Admin");

            // Non-admin can only edit/create their own record
            var targetUserId = isAdmin && !string.IsNullOrEmpty(model.UserId) ? model.UserId : currentUser.Id;

            AttendanceRecord? record = null;
            if (model.Id > 0)
            {
                record = await _db.Attendances.FindAsync(model.Id);
                if (record == null)
                {
                    TempData["Error"] = "Data presensi tidak ditemukan.";
                    return RedirectToAction(nameof(Index));
                }

                if (!isAdmin && record.UserId != currentUser.Id)
                {
                    TempData["Error"] = "Akses ditolak: Anda hanya dapat mengubah presensi milik Anda sendiri.";
                    return RedirectToAction(nameof(Index));
                }
            }
            else
            {
                // Check if already exists for this date and user
                record = await _db.Attendances.FirstOrDefaultAsync(a => a.UserId == targetUserId && a.Date == model.Date.Date);
                if (record == null)
                {
                    record = new AttendanceRecord
                    {
                        UserId = targetUserId,
                        Date = model.Date.Date,
                        CreatedAt = DateTimeHelper.Now
                    };
                    _db.Attendances.Add(record);
                }
            }

            record.Type = model.Type;
            record.WorkLocation = model.WorkLocation;
            record.LeaveReason = string.IsNullOrWhiteSpace(model.LeaveReason) ? null : model.LeaveReason.Trim();
            record.Notes = string.IsNullOrWhiteSpace(model.Notes) ? null : model.Notes.Trim();
            record.UpdatedAt = DateTimeHelper.Now;

            if (model.Type == AttendanceType.Present)
            {
                // Parse ClockIn
                if (!string.IsNullOrWhiteSpace(model.ClockInTime) && TimeSpan.TryParse(model.ClockInTime, out var inSpan))
                {
                    record.ClockIn = model.Date.Date.Add(inSpan);
                }
                else
                {
                    record.ClockIn = null;
                }

                // Parse ClockOut
                if (!string.IsNullOrWhiteSpace(model.ClockOutTime) && TimeSpan.TryParse(model.ClockOutTime, out var outSpan))
                {
                    record.ClockOut = model.Date.Date.Add(outSpan);
                }
                else
                {
                    record.ClockOut = null;
                }

                // Compute hours
                if (record.ClockIn.HasValue && record.ClockOut.HasValue && record.ClockOut.Value >= record.ClockIn.Value)
                {
                    record.TotalHours = Math.Round((record.ClockOut.Value - record.ClockIn.Value).TotalHours, 2);
                }
                else
                {
                    record.TotalHours = 0;
                }
            }
            else
            {
                // Leaves / Permissions have no clock-in/out
                record.ClockIn = null;
                record.ClockOut = null;
                record.TotalHours = 0;
            }

            await _db.SaveChangesAsync();
            TempData["Success"] = $"Data presensi tanggal {model.Date:dd MMM yyyy} berhasil disimpan.";
            return RedirectToAction(nameof(Index), new { month = model.Date.Month, year = model.Date.Year, memberId = targetUserId });
        }

        // ── SUBMIT LEAVE / PERMISSION (SINGLE OR MULTI-DAY) ────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitLeave(LeaveRequestModel model)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return Challenge();

            var isAdmin = User.IsInRole("Admin");
            var targetUserId = isAdmin && !string.IsNullOrEmpty(model.UserId) ? model.UserId : currentUser.Id;

            if (model.EndDate < model.StartDate)
            {
                TempData["Error"] = "Tanggal selesai cuti tidak boleh lebih awal dari tanggal mulai.";
                return RedirectToAction(nameof(Index));
            }

            var daysCount = 0;
            var curDate = model.StartDate.Date;
            var endDate = model.EndDate.Date;
            var nowGmt7 = DateTimeHelper.Now;

            while (curDate <= endDate)
            {
                // Exclude Sunday (Minggu) automatically from leave count
                if (curDate.DayOfWeek != DayOfWeek.Sunday)
                {
                    var existing = await _db.Attendances.FirstOrDefaultAsync(a => a.UserId == targetUserId && a.Date == curDate);
                    if (existing != null)
                    {
                        existing.Type = model.Type;
                        existing.WorkLocation = WorkLocationType.WFO;
                        existing.ClockIn = null;
                        existing.ClockOut = null;
                        existing.TotalHours = 0;
                        existing.LeaveReason = model.LeaveReason?.Trim();
                        existing.Notes = model.Notes?.Trim();
                        existing.Status = AttendanceApprovalStatus.Approved;
                        existing.UpdatedAt = nowGmt7;
                    }
                    else
                    {
                        var newRecord = new AttendanceRecord
                        {
                            UserId = targetUserId,
                            Date = curDate,
                            Type = model.Type,
                            WorkLocation = WorkLocationType.WFO,
                            ClockIn = null,
                            ClockOut = null,
                            TotalHours = 0,
                            LeaveReason = model.LeaveReason?.Trim(),
                            Notes = model.Notes?.Trim(),
                            Status = AttendanceApprovalStatus.Approved,
                            CreatedAt = nowGmt7,
                            UpdatedAt = nowGmt7
                        };
                        _db.Attendances.Add(newRecord);
                    }
                    daysCount++;
                }
                curDate = curDate.AddDays(1);
            }

            await _db.SaveChangesAsync();
            TempData["Success"] = $"Berhasil mencatatkan {model.LeaveReason} sebanyak {daysCount} hari kerja ({model.StartDate:dd/MM/yyyy} s/d {model.EndDate:dd/MM/yyyy}).";
            return RedirectToAction(nameof(Index), new { month = model.StartDate.Month, year = model.StartDate.Year, memberId = targetUserId });
        }

        // ── DELETE ATTENDANCE / LEAVE RECORD ───────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return Challenge();

            var isAdmin = User.IsInRole("Admin");
            var record = await _db.Attendances.FindAsync(id);

            if (record == null)
            {
                TempData["Error"] = "Catatan presensi tidak ditemukan.";
                return RedirectToAction(nameof(Index));
            }

            if (!isAdmin && record.UserId != currentUser.Id)
            {
                TempData["Error"] = "Akses ditolak: Anda tidak memiliki wewenang untuk menghapus catatan presensi ini.";
                return RedirectToAction(nameof(Index));
            }

            var recordDate = record.Date;
            var recordUserId = record.UserId;

            _db.Attendances.Remove(record);
            await _db.SaveChangesAsync();

            TempData["Success"] = "Catatan presensi/cuti berhasil dihapus.";
            return RedirectToAction(nameof(Index), new { month = recordDate.Month, year = recordDate.Year, memberId = recordUserId });
        }

        // ── EXPORT EXCEL REKAPITULASI PRESENSI (.xlsx) ─────────────
        [HttpGet]
        public async Task<IActionResult> ExportExcel(int? month, int? year, string? memberId)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return Challenge();

            var isAdmin = User.IsInRole("Admin");
            if (!isAdmin)
            {
                memberId = currentUser.Id;
            }
            else if (string.IsNullOrEmpty(memberId))
            {
                memberId = currentUser.Id;
            }

            var targetMonth = month ?? DateTimeHelper.Today.Month;
            var targetYear = year ?? DateTimeHelper.Today.Year;
            var startDate = new DateTime(targetYear, targetMonth, 1);
            var endDate = startDate.AddMonths(1).AddDays(-1);

            var query = _db.Attendances
                .Include(a => a.User)
                .Where(a => a.Date >= startDate && a.Date <= endDate)
                .AsQueryable();

            if (!isAdmin)
            {
                query = query.Where(a => a.User != null && a.User.CompanyId == currentUser.CompanyId);
            }

            string employeeName = "Semua Karyawan";
            if (!string.IsNullOrEmpty(memberId) && memberId != "all")
            {
                query = query.Where(a => a.UserId == memberId);
                var user = await _db.Users.FindAsync(memberId);
                if (user != null) employeeName = user.FullName;
            }

            var list = await query.OrderBy(a => a.Date).ThenBy(a => a.ClockIn).ToListAsync();

            using var wb = new XLWorkbook();
            var ws = wb.Worksheets.Add("Rekap Presensi");
            ws.ShowGridLines = true;

            // 1. Header Banner
            ws.Range("A1:I1").Merge();
            var title = ws.Cell("A1");
            title.Value = "LAPORAN REKAPITULASI PRESENSI & CUTI KARYAWAN";
            title.Style.Font.Bold = true;
            title.Style.Font.FontSize = 15;
            title.Style.Font.FontColor = XLColor.White;
            title.Style.Fill.BackgroundColor = XLColor.FromArgb(49, 46, 129); // Indigo-900
            title.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            title.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            ws.Row(1).Height = 32;

            ws.Range("A2:I2").Merge();
            var sub = ws.Cell("A2");
            sub.Value = $"Work Tracker Pro • Periode: {startDate:MMMM yyyy} | Karyawan: {employeeName}";
            sub.Style.Font.Italic = true;
            sub.Style.Font.FontSize = 10;
            sub.Style.Font.FontColor = XLColor.FromArgb(224, 231, 255);
            sub.Style.Fill.BackgroundColor = XLColor.FromArgb(67, 56, 202);
            sub.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            ws.Row(2).Height = 20;

            // 2. Info Box (Rows 4-5)
            var totalPresent = list.Count(a => a.Type == AttendanceType.Present);
            var totalWfo = list.Count(a => a.Type == AttendanceType.Present && a.WorkLocation == WorkLocationType.WFO);
            var totalWfh = list.Count(a => a.Type == AttendanceType.Present && a.WorkLocation == WorkLocationType.WFH);
            var totalLeave = list.Count(a => a.Type == AttendanceType.Leave);
            var totalSick = list.Count(a => a.Type == AttendanceType.Sick);
            var totalPermission = list.Count(a => a.Type == AttendanceType.Permission || a.Type == AttendanceType.BusinessTrip);
            var totalHours = Math.Round(list.Where(a => a.Type == AttendanceType.Present).Sum(a => a.TotalHours), 2);

            ws.Cell("A4").Value = "Karyawan:";
            ws.Cell("A4").Style.Font.Bold = true;
            ws.Cell("B4").Value = employeeName;
            ws.Range("B4:C4").Merge();

            ws.Cell("D4").Value = "Bulan/Tahun:";
            ws.Cell("D4").Style.Font.Bold = true;
            ws.Cell("E4").Value = $"{startDate:MMMM yyyy}";

            ws.Cell("F4").Value = "Total Hadir:";
            ws.Cell("F4").Style.Font.Bold = true;
            ws.Cell("G4").Value = $"{totalPresent} Hari (WFO: {totalWfo}, WFH: {totalWfh})";
            ws.Range("G4:I4").Merge();

            ws.Cell("A5").Value = "Total Jam Hadir:";
            ws.Cell("A5").Style.Font.Bold = true;
            ws.Cell("B5").Value = $"{totalHours:F2} Jam";
            ws.Range("B5:C5").Merge();

            ws.Cell("D5").Value = "Cuti / Izin / Sakit:";
            ws.Cell("D5").Style.Font.Bold = true;
            ws.Cell("E5").Value = $"Cuti: {totalLeave} | Sakit: {totalSick} | Izin: {totalPermission}";
            ws.Range("E5:I5").Merge();

            var meta = ws.Range("A4:I5");
            meta.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            meta.Style.Border.OutsideBorderColor = XLColor.FromArgb(199, 210, 254);
            meta.Style.Fill.BackgroundColor = XLColor.FromArgb(248, 250, 252);
            meta.Style.Font.FontSize = 10;

            // 3. Table Column Headers (Row 7)
            var headers = new[]
            {
                "No",              // A
                "Tanggal",         // B
                "Hari",            // C
                "Nama Karyawan",   // D
                "Status / Tipe",   // E
                "Lokasi",          // F
                "Jam Masuk",       // G
                "Jam Pulang",      // H
                "Durasi (Jam)"     // I
            };

            const int hRow = 7;
            ws.Row(hRow).Height = 25;
            for (int i = 0; i < headers.Length; i++)
            {
                var cell = ws.Cell(hRow, i + 1);
                cell.Value = headers[i];
                cell.Style.Font.Bold = true;
                cell.Style.Font.FontSize = 10;
                cell.Style.Font.FontColor = XLColor.White;
                cell.Style.Fill.BackgroundColor = XLColor.FromArgb(67, 56, 202);
                cell.Style.Alignment.Horizontal = (i == 0 || i == 1 || i == 2 || i == 4 || i == 5 || i == 6 || i == 7)
                    ? XLAlignmentHorizontalValues.Center
                    : (i == 8 ? XLAlignmentHorizontalValues.Right : XLAlignmentHorizontalValues.Left);
                cell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            }

            // 4. Data Rows
            string[] dayNames = { "Minggu", "Senin", "Selasa", "Rabu", "Kamis", "Jumat", "Sabtu" };
            int row = 8;
            int no = 1;

            if (!list.Any())
            {
                ws.Range(row, 1, row, headers.Length).Merge();
                var emp = ws.Cell(row, 1);
                emp.Value = "Tidak ada catatan presensi atau cuti pada periode ini.";
                emp.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                emp.Style.Font.Italic = true;
                emp.Style.Font.FontColor = XLColor.FromArgb(148, 163, 184);
                row++;
            }
            else
            {
                foreach (var a in list)
                {
                    ws.Cell(row, 1).Value = no;
                    ws.Cell(row, 2).Value = a.Date.ToString("yyyy-MM-dd");
                    ws.Cell(row, 3).Value = dayNames[(int)a.Date.DayOfWeek];
                    ws.Cell(row, 4).Value = a.User?.FullName ?? "-";
                    ws.Cell(row, 5).Value = a.TypeDisplayName;
                    ws.Cell(row, 6).Value = a.Type == AttendanceType.Present ? a.WorkLocation.ToString() : "-";
                    ws.Cell(row, 7).Value = a.ClockIn.HasValue ? a.ClockIn.Value.ToString("HH:mm:ss") : "-";
                    ws.Cell(row, 8).Value = a.ClockOut.HasValue ? a.ClockOut.Value.ToString("HH:mm:ss") : "-";

                    var durCell = ws.Cell(row, 9);
                    durCell.Value = a.TotalHours;
                    durCell.Style.NumberFormat.Format = "#,##0.00";

                    ws.Cell(row, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    ws.Cell(row, 2).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    ws.Cell(row, 3).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    ws.Cell(row, 5).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    ws.Cell(row, 6).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    ws.Cell(row, 7).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    ws.Cell(row, 8).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    ws.Cell(row, 9).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;

                    var rRange = ws.Range(row, 1, row, headers.Length);
                    rRange.Style.Font.FontSize = 9.5;
                    rRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                    rRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
                    rRange.Style.Border.OutsideBorderColor = XLColor.FromArgb(226, 232, 240);
                    rRange.Style.Border.InsideBorderColor = XLColor.FromArgb(226, 232, 240);

                    if (no % 2 == 0)
                    {
                        rRange.Style.Fill.BackgroundColor = XLColor.FromArgb(248, 250, 252);
                    }

                    ws.Row(row).Height = 21;
                    no++;
                    row++;
                }

                // Total Row
                int lastDataRow = row - 1;
                ws.Range(row, 1, row, 8).Merge();
                var totLabel = ws.Cell(row, 1);
                totLabel.Value = "TOTAL JAM KEHADIRAN";
                totLabel.Style.Font.Bold = true;
                totLabel.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
                totLabel.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

                var totHours = ws.Cell(row, 9);
                totHours.FormulaA1 = $"SUM(I8:I{lastDataRow})";
                totHours.Style.Font.Bold = true;
                totHours.Style.NumberFormat.Format = "#,##0.00";
                totHours.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;

                var totRange = ws.Range(row, 1, row, headers.Length);
                totRange.Style.Fill.BackgroundColor = XLColor.FromArgb(238, 242, 255);
                totRange.Style.Font.FontColor = XLColor.FromArgb(49, 46, 129);
                totRange.Style.Border.TopBorder = XLBorderStyleValues.Medium;
                totRange.Style.Border.TopBorderColor = XLColor.FromArgb(67, 56, 202);
                totRange.Style.Border.BottomBorder = XLBorderStyleValues.Double;
                totRange.Style.Border.BottomBorderColor = XLColor.FromArgb(67, 56, 202);
                ws.Row(row).Height = 24;
            }

            ws.Columns().AdjustToContents();
            ws.Column(1).Width = 6;
            ws.Column(2).Width = 14;
            ws.Column(3).Width = 12;
            ws.Column(4).Width = 26;
            ws.Column(5).Width = 24;
            ws.Column(6).Width = 12;
            ws.Column(7).Width = 14;
            ws.Column(8).Width = 14;
            ws.Column(9).Width = 16;

            using var stream = new MemoryStream();
            wb.SaveAs(stream);
            var content = stream.ToArray();

            var cleanName = employeeName.Replace(" ", "_").Replace("/", "_");
            var fileName = $"Rekap_Presensi_{cleanName}_{startDate:yyyyMM}.xlsx";
            return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }
    }
}
