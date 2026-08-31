using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TrackerKerja.Data;
using TrackerKerja.Models;

namespace TrackerKerja.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AuditTrailController : Controller
    {
        private readonly AppDbContext _db;

        public AuditTrailController(AppDbContext db)
        {
            _db = db;
        }

        public async Task<IActionResult> Index(
            string? search,
            string? controllerFilter,
            string? methodFilter,
            string? userFilter,
            string? dateFilter,
            int page = 1,
            int pageSize = 30)
        {
            ViewData["Title"] = "Audit Trail";

            var query = _db.AuditLogs.AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim().ToLower();
                query = query.Where(a =>
                    a.Path.ToLower().Contains(s) ||
                    (a.UserEmail != null && a.UserEmail.ToLower().Contains(s)) ||
                    a.ControllerName.ToLower().Contains(s) ||
                    a.ActionName.ToLower().Contains(s) ||
                    (a.IpAddress != null && a.IpAddress.Contains(s))
                );
            }

            if (!string.IsNullOrWhiteSpace(controllerFilter))
            {
                query = query.Where(a => a.ControllerName == controllerFilter);
            }

            if (!string.IsNullOrWhiteSpace(methodFilter))
            {
                query = query.Where(a => a.HttpMethod == methodFilter);
            }

            if (!string.IsNullOrWhiteSpace(userFilter))
            {
                query = query.Where(a => a.UserEmail == userFilter);
            }

            if (!string.IsNullOrWhiteSpace(dateFilter) && DateTime.TryParse(dateFilter, out var filterDate))
            {
                query = query.Where(a => a.Timestamp.Date == filterDate.Date);
            }

            var totalItems = await query.CountAsync();
            var logs = await query
                .OrderByDescending(a => a.Timestamp)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // Populate filters data
            ViewBag.Search = search;
            ViewBag.ControllerFilter = controllerFilter;
            ViewBag.MethodFilter = methodFilter;
            ViewBag.UserFilter = userFilter;
            ViewBag.DateFilter = dateFilter;
            ViewBag.CurrentPage = page;
            ViewBag.PageSize = pageSize;
            ViewBag.TotalItems = totalItems;
            ViewBag.TotalPages = (int)Math.Ceiling((double)totalItems / pageSize);

            ViewBag.AvailableControllers = await _db.AuditLogs
                .Select(a => a.ControllerName)
                .Distinct()
                .OrderBy(c => c)
                .ToListAsync();

            // Prepare 7-day Line Chart Data: GET, Create, Edit, Delete, Login, Logout
            var chartDto = new TrackerKerja.ViewModels.AuditTrailChartDto();
            var startDate = DateTime.Today.AddDays(-6);
            var recentLogs = await _db.AuditLogs
                .Where(a => a.Timestamp >= startDate)
                .ToListAsync();

            for (int i = 0; i < 7; i++)
            {
                var targetDate = startDate.AddDays(i).Date;
                var dayLogs = recentLogs.Where(a => a.Timestamp.Date == targetDate).ToList();

                chartDto.Labels.Add(targetDate.ToString("dd MMM"));

                int getCount = 0;
                int createCount = 0;
                int editCount = 0;
                int deleteCount = 0;
                int loginCount = 0;
                int logoutCount = 0;

                foreach (var log in dayLogs)
                {
                    var cat = CategorizeAuditLog(log);
                    switch (cat)
                    {
                        case "GET": getCount++; break;
                        case "Create": createCount++; break;
                        case "Edit": editCount++; break;
                        case "Delete": deleteCount++; break;
                        case "Login": loginCount++; break;
                        case "Logout": logoutCount++; break;
                    }
                }

                chartDto.GetCounts.Add(getCount);
                chartDto.CreateCounts.Add(createCount);
                chartDto.EditCounts.Add(editCount);
                chartDto.DeleteCounts.Add(deleteCount);
                chartDto.LoginCounts.Add(loginCount);
                chartDto.LogoutCounts.Add(logoutCount);
            }

            // Totals from all logs for summary cards
            var allLogs = await _db.AuditLogs.ToListAsync();
            foreach (var log in allLogs)
            {
                var cat = CategorizeAuditLog(log);
                switch (cat)
                {
                    case "GET": chartDto.TotalGet++; break;
                    case "Create": chartDto.TotalCreate++; break;
                    case "Edit": chartDto.TotalEdit++; break;
                    case "Delete": chartDto.TotalDelete++; break;
                    case "Login": chartDto.TotalLogin++; break;
                    case "Logout": chartDto.TotalLogout++; break;
                }
            }

            ViewBag.ChartData = chartDto;

            return View(logs);
        }

        private static string CategorizeAuditLog(AuditLog log)
        {
            var ctrl = (log.ControllerName ?? "").ToLower();
            var act = (log.ActionName ?? "").ToLower();
            var method = (log.HttpMethod ?? "").ToUpper();
            var path = (log.Path ?? "").ToLower();

            if ((ctrl == "account" && act == "login" && method == "POST") || (path.EndsWith("/account/login") && method == "POST"))
                return "Login";

            if ((ctrl == "account" && act == "logout") || path.EndsWith("/account/logout"))
                return "Logout";

            if (method == "POST")
            {
                if (act.Contains("delete") || act.Contains("clear") || act.Contains("remove"))
                    return "Delete";
                if (act.Contains("create") || act.Contains("add") || act.Contains("upload") || act.Contains("confirm") || act.Contains("register"))
                    return "Create";
                if (act.Contains("edit") || act.Contains("update") || act.Contains("starttimer") || act.Contains("stoptimer") || act.Contains("togglepin") || act.Contains("profile"))
                    return "Edit";
                return "Edit";
            }

            return "GET";
        }

        [HttpGet]
        public async Task<IActionResult> ExportCsv()
        {
            var logs = await _db.AuditLogs
                .OrderByDescending(a => a.Timestamp)
                .Take(1000)
                .ToListAsync();

            var csv = new StringBuilder();
            csv.AppendLine("Id,Timestamp,UserEmail,HttpMethod,Controller,Action,Path,StatusCode,DurationMs,IpAddress");

            foreach (var log in logs)
            {
                csv.AppendLine($"\"{log.Id}\",\"{log.Timestamp:yyyy-MM-dd HH:mm:ss}\",\"{log.UserEmail}\",\"{log.HttpMethod}\",\"{log.ControllerName}\",\"{log.ActionName}\",\"{log.Path}\",\"{log.StatusCode}\",\"{log.DurationMs}\",\"{log.IpAddress}\"");
            }

            var bytes = Encoding.UTF8.GetBytes(csv.ToString());
            return File(bytes, "text/csv", $"AuditTrail_{DateTime.Now:yyyyMMdd_HHmmss}.csv");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ClearLogs()
        {
            _db.AuditLogs.RemoveRange(_db.AuditLogs);
            await _db.SaveChangesAsync();
            TempData["Success"] = "Riwayat audit trail berhasil dibersihkan.";
            return RedirectToAction("Index");
        }
    }
}
