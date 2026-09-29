using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TrackerKerja.Data;
using TrackerKerja.Models;
using TrackerKerja.Services;
using TrackerKerja.ViewModels;

namespace TrackerKerja.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        private readonly AppDbContext _db;
        private readonly UserManager<AppUser> _userManager;
        private readonly IGamificationService _gamificationService;

        public HomeController(AppDbContext db, UserManager<AppUser> userManager, IGamificationService gamificationService)
        {
            _db = db;
            _userManager = userManager;
            _gamificationService = gamificationService;
        }

        public async Task<IActionResult> Index()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var currentUserId = currentUser?.Id ?? "";
            var isAdmin = User.IsInRole("Admin");
            var userCompanyId = currentUser?.CompanyId;

            var today = DateTime.Today;
            var weekStart = today.AddDays(-(int)today.DayOfWeek + 1);

            var tasksQuery = _db.Tasks
                .Include(t => t.Project)
                .Include(t => t.Category)
                .Include(t => t.AssignedToUser)
                .Include(t => t.Sessions)
                .AsQueryable();

            var projectsQuery = _db.Projects
                .Include(p => p.Tasks)
                .OrderByDescending(p => p.CreatedAt)
                .AsQueryable();

            var usersQuery = _db.Users
                .OrderBy(u => u.FullName)
                .AsQueryable();

            var sessionsQuery = _db.Sessions
                .Include(s => s.Task)
                    .ThenInclude(t => t!.Project)
                .AsQueryable();

            if (!isAdmin)
            {
                tasksQuery = tasksQuery.Where(t => t.CompanyId == userCompanyId || (t.Project != null && t.Project.CompanyId == userCompanyId));
                projectsQuery = projectsQuery.Where(p => p.CompanyId == userCompanyId);
                usersQuery = usersQuery.Where(u => u.CompanyId == userCompanyId);
                sessionsQuery = sessionsQuery.Where(s => s.Task != null && (s.Task.CompanyId == userCompanyId || (s.Task.Project != null && s.Task.Project.CompanyId == userCompanyId)));
            }

            var allTasks = await tasksQuery.ToListAsync();
            var allProjects = await projectsQuery.ToListAsync();
            var allUsers = await usersQuery.ToListAsync();

            var todaySessions = await sessionsQuery
                .Where(s => s.StartTime.Date == today)
                .ToListAsync();

            var weekSessions = await sessionsQuery
                .Where(s => s.StartTime >= weekStart)
                .ToListAsync();

            var runningSession = await _db.Sessions
                .Include(s => s.Task)
                    .ThenInclude(t => t!.Project)
                .FirstOrDefaultAsync(s => s.EndTime == null && (isAdmin || (s.Task != null && (s.Task.CompanyId == userCompanyId || (s.Task.Project != null && s.Task.Project.CompanyId == userCompanyId)))));

            // ── PERSONAL STATS (FOR LOGGED IN USER) ─────────────
            var myTasks = allTasks.Where(t => t.AssignedToUserId == currentUserId).ToList();
            var myTodaySessions = todaySessions.Where(s => s.Task != null && s.Task.AssignedToUserId == currentUserId).ToList();
            var myRecentNotes = await _db.Notes
                .Include(n => n.Task)
                .Where(n => n.AuthorUserId == currentUserId)
                .OrderByDescending(n => n.UpdatedAt)
                .Take(5)
                .ToListAsync();

            // ── WEEKLY WORK HOURS ──────────────────────────────
            var weekLabels = new List<string>();
            var weekHours = new List<long>();
            for (int i = 0; i < 7; i++)
            {
                var day = weekStart.AddDays(i);
                weekLabels.Add(day.ToString("ddd"));
                var daySeconds = weekSessions
                    .Where(s => s.StartTime.Date == day.Date)
                    .Sum(s => s.Duration);
                weekHours.Add(daySeconds / 3600);
            }

            // ── STATUS DISTRIBUTION (OVERALL) ─────────────────
            var overdueCount = allTasks.Count(t => t.DueDate < DateTime.Now && t.Status != Models.TaskStatus.Done);
            var inProgressCount = allTasks.Count(t => t.Status == Models.TaskStatus.InProgress);
            var doneCount = allTasks.Count(t => t.Status == Models.TaskStatus.Done);
            var todoCount = allTasks.Count(t => t.Status == Models.TaskStatus.Todo && (t.DueDate == null || t.DueDate >= DateTime.Now));

            var statusLabels = new List<string> { "Todo", "In Progress", "Done", "Overdue" };
            var statusCounts = new List<int> { todoCount, inProgressCount, doneCount, overdueCount };

            // ── PROJECT TASK DISTRIBUTION ─────────────────────
            var projectLabels = new List<string>();
            var projectTodo = new List<int>();
            var projectInProgress = new List<int>();
            var projectDone = new List<int>();

            foreach (var proj in allProjects.Take(6))
            {
                projectLabels.Add(proj.Name);
                projectTodo.Add(proj.Tasks.Count(t => t.Status == Models.TaskStatus.Todo));
                projectInProgress.Add(proj.Tasks.Count(t => t.Status == Models.TaskStatus.InProgress));
                projectDone.Add(proj.Tasks.Count(t => t.Status == Models.TaskStatus.Done));
            }

            // ── MEMBER WORKLOAD DISTRIBUTION CHART ─────────────
            var memberLabels = new List<string>();
            var memberTodo = new List<int>();
            var memberInProgress = new List<int>();
            var memberDone = new List<int>();
            var memberHours = new List<double>();

            foreach (var u in allUsers)
            {
                var uTasks = allTasks.Where(t => t.AssignedToUserId == u.Id).ToList();
                var shortName = u.FullName.Split(' ').FirstOrDefault() ?? u.UserName ?? "User";
                if (u.FullName.Split(' ').Length > 1)
                {
                    shortName += " " + u.FullName.Split(' ')[1].Substring(0, 1) + ".";
                }

                memberLabels.Add(shortName);
                memberTodo.Add(uTasks.Count(t => t.Status == Models.TaskStatus.Todo));
                memberInProgress.Add(uTasks.Count(t => t.Status == Models.TaskStatus.InProgress));
                memberDone.Add(uTasks.Count(t => t.Status == Models.TaskStatus.Done));

                var secs = uTasks.SelectMany(t => t.Sessions).Sum(s => s.DurationSeconds);
                memberHours.Add(Math.Round(secs / 3600.0, 1));
            }

            // Also add "Unassigned" if there are tasks without PIC
            var unassignedTasks = allTasks.Where(t => t.AssignedToUserId == null).ToList();
            if (unassignedTasks.Any())
            {
                memberLabels.Add("Belum Ditugaskan");
                memberTodo.Add(unassignedTasks.Count(t => t.Status == Models.TaskStatus.Todo));
                memberInProgress.Add(unassignedTasks.Count(t => t.Status == Models.TaskStatus.InProgress));
                memberDone.Add(unassignedTasks.Count(t => t.Status == Models.TaskStatus.Done));
                memberHours.Add(0);
            }

            // ── PROJECT-MEMBER MATRIX ─────────────────────────
            var projectMemberDist = new List<ProjectMemberDistributionDto>();
            foreach (var proj in allProjects)
            {
                foreach (var u in allUsers)
                {
                    var pTasks = allTasks.Where(t => t.ProjectId == proj.Id && t.AssignedToUserId == u.Id).ToList();
                    if (pTasks.Any())
                    {
                        var secs = pTasks.SelectMany(t => t.Sessions).Sum(s => s.DurationSeconds);
                        projectMemberDist.Add(new ProjectMemberDistributionDto
                        {
                            ProjectId = proj.Id,
                            ProjectName = proj.Name,
                            MemberName = u.FullName,
                            MemberAvatar = u.ProfilePictureUrl ?? "",
                            MemberColor = u.AvatarColor ?? "#6366F1",
                            TodoCount = pTasks.Count(t => t.Status == Models.TaskStatus.Todo),
                            InProgressCount = pTasks.Count(t => t.Status == Models.TaskStatus.InProgress),
                            DoneCount = pTasks.Count(t => t.Status == Models.TaskStatus.Done),
                            LoggedHours = Math.Round(secs / 3600.0, 1)
                        });
                    }
                }
            }

            DailyCheckInStatusDto checkInStatus = new();
            GamificationUserPointsDto pointsSummary = new();
            if (!string.IsNullOrEmpty(currentUserId))
            {
                checkInStatus = await _gamificationService.GetDailyCheckInStatusAsync(currentUserId);
                pointsSummary = await _gamificationService.GetUserPointsSummaryAsync(currentUserId);
            }

            var vm = new DashboardViewModel
            {
                IsAdmin = isAdmin,
                CurrentUserId = currentUserId,
                CurrentUserName = currentUser?.FullName ?? currentUser?.Email ?? "Pengguna",
                CurrentUserEmail = currentUser?.Email ?? "",
                CheckInStatus = checkInStatus,
                PointsSummary = pointsSummary,
                MyTotalTasks = myTasks.Count,
                MyDoneTasks = myTasks.Count(t => t.Status == Models.TaskStatus.Done),
                MyInProgressTasks = myTasks.Count(t => t.Status == Models.TaskStatus.InProgress),
                MyTodoTasks = myTasks.Count(t => t.Status == Models.TaskStatus.Todo),
                MyOverdueTasks = myTasks.Count(t => t.DueDate < DateTime.Now && t.Status != Models.TaskStatus.Done),
                MyTodayWorkSeconds = myTodaySessions.Sum(s => s.Duration),
                MyTasks = myTasks.OrderByDescending(t => t.CreatedAt).Take(8).ToList(),
                MyRecentNotes = myRecentNotes,

                TotalTasks = allTasks.Count,
                DoneTasks = doneCount,
                PendingTasks = allTasks.Count(t => t.Status == Models.TaskStatus.Todo),
                InProgressTasks = inProgressCount,
                OverdueTasks = overdueCount,
                TotalProjects = allProjects.Count,
                TodayWorkSeconds = todaySessions.Sum(s => s.Duration),
                TodayTasks = allTasks.Where(t => t.DueDate?.Date == today && t.Status != Models.TaskStatus.Done).Take(8).ToList(),
                OverdueTaskList = allTasks.Where(t => t.DueDate < DateTime.Now && t.Status != Models.TaskStatus.Done).Take(5).ToList(),
                ActiveProjects = allProjects.Where(p => p.Status == Models.ProjectStatus.Active).Take(4).ToList(),
                RunningSession = runningSession,
                WeekLabels = weekLabels,
                WeekHours = weekHours,
                StatusChartLabels = statusLabels,
                StatusChartCounts = statusCounts,
                ProjectChartLabels = projectLabels,
                ProjectChartTodo = projectTodo,
                ProjectChartInProgress = projectInProgress,
                ProjectChartDone = projectDone,

                MemberChartLabels = memberLabels,
                MemberChartTodo = memberTodo,
                MemberChartInProgress = memberInProgress,
                MemberChartDone = memberDone,
                MemberChartHours = memberHours,

                AllProjects = allProjects,
                ProjectMemberDistributions = projectMemberDist
            };

            return View(vm);
        }

        // ── AJAX ENDPOINT: FILTER PROJECT-MEMBER MATRIX ─────────
        [HttpGet]
        public async Task<IActionResult> GetProjectMemberDistribution(int? projectId)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var isAdmin = User.IsInRole("Admin");
            var userCompanyId = currentUser?.CompanyId;

            var query = _db.Tasks
                .Include(t => t.Project)
                .Include(t => t.AssignedToUser)
                .Include(t => t.Sessions)
                .AsQueryable();

            if (!isAdmin)
            {
                query = query.Where(t => t.CompanyId == userCompanyId || (t.Project != null && t.Project.CompanyId == userCompanyId));
            }

            if (projectId.HasValue && projectId.Value > 0)
            {
                query = query.Where(t => t.ProjectId == projectId.Value);
            }

            var tasks = await query.ToListAsync();

            var usersQuery = _db.Users.OrderBy(u => u.FullName).AsQueryable();
            if (!isAdmin)
            {
                usersQuery = usersQuery.Where(u => u.CompanyId == userCompanyId);
            }
            var users = await usersQuery.ToListAsync();

            var result = new List<object>();

            foreach (var u in users)
            {
                var uTasks = tasks.Where(t => t.AssignedToUserId == u.Id).ToList();
                if (uTasks.Any() || !projectId.HasValue)
                {
                    var secs = uTasks.SelectMany(t => t.Sessions).Sum(s => s.DurationSeconds);
                    result.Add(new
                    {
                        memberName = u.FullName,
                        avatar = u.ProfilePictureUrl ?? "",
                        initials = u.Initials,
                        color = u.AvatarColor ?? "#6366F1",
                        todo = uTasks.Count(t => t.Status == Models.TaskStatus.Todo),
                        inProgress = uTasks.Count(t => t.Status == Models.TaskStatus.InProgress),
                        done = uTasks.Count(t => t.Status == Models.TaskStatus.Done),
                        total = uTasks.Count,
                        hours = Math.Round(secs / 3600.0, 1)
                    });
                }
            }

            return Json(result);
        }

        [HttpPost]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> RunBackgroundSync()
        {
            var tasks = await _db.Tasks.ToListAsync();
            int syncedTasksCount = 0;

            foreach (var task in tasks)
            {
                bool modified = false;
                if (task.Status == Models.TaskStatus.Done && task.Progress != 100)
                {
                    task.Progress = 100;
                    modified = true;
                }
                else if (task.Progress >= 100 && task.Status != Models.TaskStatus.Done)
                {
                    task.Status = Models.TaskStatus.Done;
                    task.Progress = 100;
                    modified = true;
                }

                if (modified)
                {
                    task.UpdatedAt = DateTime.Now;
                    syncedTasksCount++;
                }
            }

            var projects = await _db.Projects.Include(p => p.Tasks).ToListAsync();
            int syncedProjectsCount = 0;

            foreach (var proj in projects)
            {
                if (proj.Tasks.Any())
                {
                    syncedProjectsCount++;
                }
            }

            if (syncedTasksCount > 0)
            {
                await _db.SaveChangesAsync();
            }

            return Json(new
            {
                success = true,
                timestamp = DateTime.Now.ToString("HH:mm:ss"),
                syncedTasksCount,
                syncedProjectsCount,
                totalTasks = tasks.Count,
                doneTasks = tasks.Count(t => t.Status == Models.TaskStatus.Done),
                inProgressTasks = tasks.Count(t => t.Status == Models.TaskStatus.InProgress),
                todoTasks = tasks.Count(t => t.Status == Models.TaskStatus.Todo),
                overdueTasks = tasks.Count(t => t.DueDate.HasValue && t.DueDate.Value < DateTime.Now && t.Status != Models.TaskStatus.Done)
            });
        }

        // ── AJAX: DASHBOARD ANALYTICS FILTER BY PERIOD ──────────────
        [HttpGet]
        public async Task<IActionResult> GetDashboardAnalytics(string period = "week", int? projectId = null)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var isAdmin = User.IsInRole("Admin");
            var userCompanyId = currentUser?.CompanyId;
            var today = DateTime.Today;

            // Determine date range based on period
            DateTime dateFrom;
            DateTime dateTo = today.AddDays(1).AddTicks(-1);
            string periodLabel;
            int dayCount;

            switch (period)
            {
                case "today":
                    dateFrom = today;
                    periodLabel = "Hari Ini";
                    dayCount = 1;
                    break;
                case "week":
                    dateFrom = today.AddDays(-6);
                    periodLabel = "7 Hari Terakhir";
                    dayCount = 7;
                    break;
                case "month":
                    dateFrom = today.AddDays(-29);
                    periodLabel = "30 Hari Terakhir";
                    dayCount = 30;
                    break;
                case "quarter":
                    dateFrom = today.AddDays(-89);
                    periodLabel = "90 Hari Terakhir";
                    dayCount = 90;
                    break;
                default:
                    dateFrom = today.AddDays(-6);
                    periodLabel = "7 Hari Terakhir";
                    dayCount = 7;
                    break;
            }

            // Base queries with company scope
            var tasksQuery = _db.Tasks
                .Include(t => t.Project)
                .Include(t => t.AssignedToUser)
                .Include(t => t.Sessions)
                .AsQueryable();

            var sessionsQuery = _db.Sessions
                .Include(s => s.Task)
                    .ThenInclude(t => t!.Project)
                .AsQueryable();

            var usersQuery = _db.Users
                .OrderBy(u => u.FullName)
                .AsQueryable();

            if (!isAdmin)
            {
                tasksQuery = tasksQuery.Where(t =>
                    t.CompanyId == userCompanyId ||
                    (t.Project != null && t.Project.CompanyId == userCompanyId));
                sessionsQuery = sessionsQuery.Where(s =>
                    s.Task != null &&
                    (s.Task.CompanyId == userCompanyId ||
                     (s.Task.Project != null && s.Task.Project.CompanyId == userCompanyId)));
                usersQuery = usersQuery.Where(u => u.CompanyId == userCompanyId);
            }

            // Optional project filter
            if (projectId.HasValue && projectId.Value > 0)
            {
                tasksQuery = tasksQuery.Where(t => t.ProjectId == projectId.Value);
                sessionsQuery = sessionsQuery.Where(s => s.Task != null && s.Task.ProjectId == projectId.Value);
            }

            // Load data
            var allTasks = await tasksQuery.ToListAsync();
            var periodSessions = await sessionsQuery
                .Where(s => s.StartTime.Date >= dateFrom.Date && s.StartTime.Date <= today)
                .ToListAsync();
            var allUsers = await usersQuery.ToListAsync();

            // KPI counts (all-time totals for counts, period for hours)
            var totalWork = periodSessions.Sum(s => s.Duration);
            var totalWorkHours = Math.Round(totalWork / 3600.0, 1);
            var avgDailyHours = dayCount > 0 ? Math.Round(totalWorkHours / dayCount, 1) : 0;

            // Build day-by-day trend
            var trendLabels = new List<string>();
            var trendHours = new List<double>();
            var trendDone = new List<int>();

            // Use compact label based on period
            string dayFormat = dayCount > 30 ? "dd/MM" : (dayCount > 7 ? "dd MMM" : "ddd dd/M");
            for (int i = dayCount - 1; i >= 0; i--)
            {
                var d = today.AddDays(-i);
                trendLabels.Add(d.ToString(dayFormat));
                var dayHours = periodSessions
                    .Where(s => s.StartTime.Date == d.Date)
                    .Sum(s => s.Duration) / 3600.0;
                trendHours.Add(Math.Round(dayHours, 1));
                var dayDone = allTasks
                    .Count(t => t.Status == Models.TaskStatus.Done && t.UpdatedAt.Date == d.Date);
                trendDone.Add(dayDone);
            }

            // Member productivity
            var memberProductivity = allUsers.Select(u =>
            {
                var uTasks = allTasks.Where(t => t.AssignedToUserId == u.Id).ToList();
                var uSecs = periodSessions
                    .Where(s => s.UserId == u.Id)
                    .Sum(s => s.Duration);
                var nameParts = u.FullName.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
                var shortName = nameParts.Length >= 2
                    ? $"{nameParts[0]} {nameParts[1][0]}."
                    : (nameParts.Length == 1 ? nameParts[0] : u.UserName ?? "User");
                return new MemberProductivityItemDto
                {
                    MemberId = u.Id,
                    MemberName = shortName,
                    MemberColor = u.AvatarColor ?? "#6366F1",
                    Initials = u.Initials,
                    DoneTasks = uTasks.Count(t => t.Status == Models.TaskStatus.Done),
                    TotalTasks = uTasks.Count,
                    WorkHours = Math.Round(uSecs / 3600.0, 1)
                };
            }).Where(m => m.TotalTasks > 0).ToList();

            var dto = new DashboardAnalyticsDto
            {
                TotalTasks = allTasks.Count,
                DoneTasks = allTasks.Count(t => t.Status == Models.TaskStatus.Done),
                InProgressTasks = allTasks.Count(t => t.Status == Models.TaskStatus.InProgress),
                TodoTasks = allTasks.Count(t => t.Status == Models.TaskStatus.Todo),
                OverdueTasks = allTasks.Count(t => t.DueDate < DateTime.Now && t.Status != Models.TaskStatus.Done),
                TotalWorkHours = totalWorkHours,
                AvgDailyWorkHours = avgDailyHours,
                TrendLabels = trendLabels,
                TrendHours = trendHours,
                TrendDoneTasks = trendDone,
                StatusLabels = new List<string> { "Todo", "In Progress", "Done", "Overdue" },
                StatusCounts = new List<int>
                {
                    allTasks.Count(t => t.Status == Models.TaskStatus.Todo && (t.DueDate == null || t.DueDate >= DateTime.Now)),
                    allTasks.Count(t => t.Status == Models.TaskStatus.InProgress),
                    allTasks.Count(t => t.Status == Models.TaskStatus.Done),
                    allTasks.Count(t => t.DueDate < DateTime.Now && t.Status != Models.TaskStatus.Done)
                },
                MemberProductivity = memberProductivity,
                Period = period,
                PeriodLabel = periodLabel,
                DateFrom = dateFrom.ToString("dd MMM yyyy"),
                DateTo = today.ToString("dd MMM yyyy")
            };

            return Json(dto);
        }

        // ── AJAX: KPI DRILL-DOWN TASK LIST ──────────────────────────
        [HttpGet]
        public async Task<IActionResult> GetKpiDrillDown(string kpi, string period = "week", int? projectId = null)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var isAdmin = User.IsInRole("Admin");
            var userCompanyId = currentUser?.CompanyId;
            var today = DateTime.Today;

            DateTime dateFrom = period switch
            {
                "today" => today,
                "month" => today.AddDays(-29),
                "quarter" => today.AddDays(-89),
                _ => today.AddDays(-6)
            };

            var tasksQuery = _db.Tasks
                .Include(t => t.Project)
                .Include(t => t.AssignedToUser)
                .Include(t => t.Sessions)
                .AsQueryable();

            if (!isAdmin)
                tasksQuery = tasksQuery.Where(t =>
                    t.CompanyId == userCompanyId ||
                    (t.Project != null && t.Project.CompanyId == userCompanyId));

            if (projectId.HasValue && projectId.Value > 0)
                tasksQuery = tasksQuery.Where(t => t.ProjectId == projectId.Value);

            tasksQuery = kpi switch
            {
                "done" => tasksQuery.Where(t => t.Status == Models.TaskStatus.Done),
                "inprogress" => tasksQuery.Where(t => t.Status == Models.TaskStatus.InProgress),
                "todo" => tasksQuery.Where(t => t.Status == Models.TaskStatus.Todo && (t.DueDate == null || t.DueDate >= DateTime.Now)),
                "overdue" => tasksQuery.Where(t => t.DueDate < DateTime.Now && t.Status != Models.TaskStatus.Done),
                _ => tasksQuery
            };

            var tasks = await tasksQuery.OrderByDescending(t => t.UpdatedAt).Take(50).ToListAsync();

            var result = tasks.Select(t => new DrillDownTaskDto
            {
                Id = t.Id,
                Title = t.Title,
                Status = t.Status.ToString(),
                Priority = t.Priority.ToString(),
                ProjectName = t.Project?.Name,
                AssigneeName = t.AssignedToUser?.FullName,
                DueDate = t.DueDate?.ToString("dd MMM yyyy"),
                IsOverdue = t.DueDate.HasValue && t.DueDate.Value < DateTime.Now && t.Status != Models.TaskStatus.Done,
                WorkHours = Math.Round(t.Sessions.Sum(s => s.Duration) / 3600.0, 1)
            }).ToList();

            return Json(new { success = true, tasks = result, kpi, total = result.Count });
        }
    }
}
