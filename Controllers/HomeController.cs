using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TrackerKerja.Data;
using TrackerKerja.Models;
using TrackerKerja.ViewModels;

namespace TrackerKerja.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        private readonly AppDbContext _db;
        private readonly UserManager<AppUser> _userManager;

        public HomeController(AppDbContext db, UserManager<AppUser> userManager)
        {
            _db = db;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var currentUserId = currentUser?.Id ?? "";
            var isAdmin = User.IsInRole("Admin");

            var today = DateTime.Today;
            var weekStart = today.AddDays(-(int)today.DayOfWeek + 1);

            var allTasks = await _db.Tasks
                .Include(t => t.Project)
                .Include(t => t.Category)
                .Include(t => t.AssignedToUser)
                .Include(t => t.Sessions)
                .ToListAsync();

            var allProjects = await _db.Projects
                .Include(p => p.Tasks)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();

            var allUsers = await _db.Users
                .OrderBy(u => u.FullName)
                .ToListAsync();

            var todaySessions = await _db.Sessions
                .Where(s => s.StartTime.Date == today)
                .ToListAsync();

            var weekSessions = await _db.Sessions
                .Where(s => s.StartTime >= weekStart)
                .ToListAsync();

            var runningSession = await _db.Sessions
                .Include(s => s.Task)
                .FirstOrDefaultAsync(s => s.EndTime == null);

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

            var vm = new DashboardViewModel
            {
                IsAdmin = isAdmin,
                CurrentUserId = currentUserId,
                CurrentUserName = currentUser?.FullName ?? currentUser?.Email ?? "Pengguna",
                CurrentUserEmail = currentUser?.Email ?? "",
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
            var query = _db.Tasks
                .Include(t => t.Project)
                .Include(t => t.AssignedToUser)
                .Include(t => t.Sessions)
                .AsQueryable();

            if (projectId.HasValue && projectId.Value > 0)
            {
                query = query.Where(t => t.ProjectId == projectId.Value);
            }

            var tasks = await query.ToListAsync();
            var users = await _db.Users.OrderBy(u => u.FullName).ToListAsync();

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
    }
}
