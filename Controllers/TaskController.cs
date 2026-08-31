using ClosedXML.Excel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TrackerKerja.Data;
using TrackerKerja.Models;
using TrackerKerja.Services;
using TrackerKerja.ViewModels;
using ModelTaskStatus = TrackerKerja.Models.TaskStatus;

namespace TrackerKerja.Controllers
{
    public class StartTimerDto { public int TaskId { get; set; } }
    public class StopTimerDto { public int SessionId { get; set; } }
    public class UpdateKanbanStatusDto
    {
        public int TaskId { get; set; }
        public string NewStatus { get; set; } = string.Empty;
        public int? Progress { get; set; }
    }

    [Authorize]
    public class TaskController : Controller
    {
        private readonly AppDbContext _db;
        private readonly UserManager<AppUser> _userManager;

        public TaskController(AppDbContext db, UserManager<AppUser> userManager)
        {
            _db = db;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index(
            string? status,
            string? priority,
            int? projectId,
            string? assigneeId,
            string? milestone,
            string? search,
            string? sortBy = "created",
            string? sortOrder = "desc",
            int page = 1,
            int pageSize = 10)
        {
            if (page < 1) page = 1;
            if (pageSize < 5) pageSize = 10;

            var query = _db.Tasks
                .Include(t => t.Project)
                .Include(t => t.Category)
                .Include(t => t.AssignedToUser)
                .Include(t => t.ParentTask)
                .Include(t => t.ChildTasks)
                .Include(t => t.Sessions)
                .AsQueryable();

            if (!string.IsNullOrEmpty(status) && Enum.TryParse<ModelTaskStatus>(status, out var s))
                query = query.Where(t => t.Status == s);

            if (!string.IsNullOrEmpty(priority) && Enum.TryParse<TaskPriority>(priority, out var p))
                query = query.Where(t => t.Priority == p);

            if (projectId.HasValue)
                query = query.Where(t => t.ProjectId == projectId);

            if (!string.IsNullOrEmpty(assigneeId))
                query = query.Where(t => t.AssignedToUserId == assigneeId);

            if (!string.IsNullOrEmpty(milestone))
                query = query.Where(t => t.Milestone == milestone);

            if (!string.IsNullOrEmpty(search))
                query = query.Where(t => t.Title.Contains(search) || (t.Description != null && t.Description.Contains(search)));

            bool isAsc = string.Equals(sortOrder, "asc", StringComparison.OrdinalIgnoreCase);

            query = sortBy?.ToLower() switch
            {
                "title" => isAsc ? query.OrderBy(t => t.Title) : query.OrderByDescending(t => t.Title),
                "status" => isAsc ? query.OrderBy(t => t.Status) : query.OrderByDescending(t => t.Status),
                "priority" => isAsc ? query.OrderBy(t => t.Priority) : query.OrderByDescending(t => t.Priority),
                "progress" => isAsc ? query.OrderBy(t => t.Progress) : query.OrderByDescending(t => t.Progress),
                "duedate" => isAsc ? query.OrderBy(t => t.DueDate) : query.OrderByDescending(t => t.DueDate),
                "project" => isAsc ? query.OrderBy(t => t.Project != null ? t.Project.Name : "") : query.OrderByDescending(t => t.Project != null ? t.Project.Name : ""),
                "assignee" => isAsc ? query.OrderBy(t => t.AssignedToUser != null ? t.AssignedToUser.FullName : "") : query.OrderByDescending(t => t.AssignedToUser != null ? t.AssignedToUser.FullName : ""),
                "milestone" => isAsc ? query.OrderBy(t => t.Milestone) : query.OrderByDescending(t => t.Milestone),
                _ => isAsc ? query.OrderBy(t => t.CreatedAt) : query.OrderByDescending(t => t.CreatedAt)
            };

            var totalItems = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
            if (totalPages < 1) totalPages = 1;

            List<WorkTask> tasks;
            if (string.IsNullOrEmpty(sortBy) || sortBy.ToLower() == "created")
            {
                var allTasks = await query.ToListAsync();
                var rootTasks = allTasks.Where(t => t.ParentTaskId == null).ToList();
                var childLookup = allTasks.Where(t => t.ParentTaskId != null).ToLookup(t => t.ParentTaskId!.Value);

                var flatOrdered = new List<WorkTask>();
                foreach (var parent in rootTasks)
                {
                    flatOrdered.Add(parent);
                    if (childLookup.Contains(parent.Id))
                    {
                        flatOrdered.AddRange(childLookup[parent.Id]);
                    }
                }
                tasks = flatOrdered.Skip((page - 1) * pageSize).Take(pageSize).ToList();
            }
            else
            {
                tasks = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
            }

            ViewBag.Projects = await _db.Projects.ToListAsync();
            ViewBag.Users = await _db.Users.OrderBy(u => u.FullName).ToListAsync();
            ViewBag.Milestones = await _db.MasterMilestones.OrderBy(m => m.OrderIndex).ToListAsync();
            ViewBag.StatusFilter = status;
            ViewBag.PriorityFilter = priority;
            ViewBag.ProjectFilter = projectId;
            ViewBag.AssigneeFilter = assigneeId;
            ViewBag.MilestoneFilter = milestone;
            ViewBag.Search = search;
            ViewBag.SortBy = sortBy;
            ViewBag.SortOrder = sortOrder;

            ViewBag.CurrentPage = page;
            ViewBag.PageSize = pageSize;
            ViewBag.TotalItems = totalItems;
            ViewBag.TotalPages = totalPages;

            return View(tasks);
        }

        public async Task<IActionResult> Create()
        {
            return View(new TaskFormViewModel
            {
                Projects = await _db.Projects.Where(p => p.Status == ProjectStatus.Active).ToListAsync(),
                Categories = await _db.Categories.ToListAsync(),
                Users = await _db.Users.OrderBy(u => u.FullName).ToListAsync(),
                AvailableParentTasks = await _db.Tasks.OrderBy(t => t.Title).ToListAsync(),
                Milestones = await _db.MasterMilestones.OrderBy(m => m.OrderIndex).ToListAsync()
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(WorkTask model)
        {
            ModelState.Remove("Project");
            ModelState.Remove("Category");
            ModelState.Remove("AssignedToUser");
            ModelState.Remove("Sessions");
            ModelState.Remove("ParentTask");
            ModelState.Remove("ChildTasks");

            if (model.ParentTaskId.HasValue && model.ParentTaskId.Value <= 0)
            {
                model.ParentTaskId = null;
            }

            if (!ModelState.IsValid)
            {
                return View(new TaskFormViewModel
                {
                    Task = model,
                    Projects = await _db.Projects.Where(p => p.Status == ProjectStatus.Active).ToListAsync(),
                    Categories = await _db.Categories.ToListAsync(),
                    Users = await _db.Users.OrderBy(u => u.FullName).ToListAsync(),
                    AvailableParentTasks = await _db.Tasks.OrderBy(t => t.Title).ToListAsync(),
                    Milestones = await _db.MasterMilestones.OrderBy(m => m.OrderIndex).ToListAsync()
                });
            }

            // Smart Progress & Status synchronization
            if (model.Progress >= 100)
            {
                model.Progress = 100;
                model.Status = ModelTaskStatus.Done;
            }
            else if (model.Status == ModelTaskStatus.Done)
            {
                model.Progress = 100;
            }
            else if (model.Progress > 0 && model.Status == ModelTaskStatus.Todo)
            {
                model.Status = ModelTaskStatus.InProgress;
            }

            model.CreatedAt = DateTime.Now;
            model.UpdatedAt = DateTime.Now;
            _db.Tasks.Add(model);
            await _db.SaveChangesAsync();
            TempData["Success"] = "Tugas berhasil dibuat!";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int id)
        {
            var task = await _db.Tasks
                .Include(t => t.Sessions)
                .Include(t => t.Notes)
                    .ThenInclude(n => n.AuthorUser)
                .Include(t => t.AssignedToUser)
                .Include(t => t.Project)
                .Include(t => t.Category)
                .Include(t => t.ParentTask)
                .Include(t => t.ChildTasks)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (task == null) return NotFound();

            var currentUser = await _userManager.GetUserAsync(User);
            var isAdmin = User.IsInRole("Admin");

            if (!TaskPermissionHelper.CanEditTask(currentUser, isAdmin, task))
            {
                TempData["Error"] = "Akses Ditolak: Anda hanya memiliki izin untuk mengubah tugas milik Anda sendiri.";
                return RedirectToAction(nameof(Index));
            }

            return View(new TaskFormViewModel
            {
                Task = task,
                Projects = await _db.Projects.Where(p => p.Status == ProjectStatus.Active).ToListAsync(),
                Categories = await _db.Categories.ToListAsync(),
                Users = await _db.Users.OrderBy(u => u.FullName).ToListAsync(),
                AvailableParentTasks = await _db.Tasks.Where(t => t.Id != id).OrderBy(t => t.Title).ToListAsync(),
                Milestones = await _db.MasterMilestones.OrderBy(m => m.OrderIndex).ToListAsync()
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, WorkTask model)
        {
            if (id != model.Id) return BadRequest();

            var existingTask = await _db.Tasks.AsNoTracking().FirstOrDefaultAsync(t => t.Id == id);
            if (existingTask == null) return NotFound();

            var currentUser = await _userManager.GetUserAsync(User);
            var isAdmin = User.IsInRole("Admin");

            if (!TaskPermissionHelper.CanEditTask(currentUser, isAdmin, existingTask))
            {
                TempData["Error"] = "Akses Ditolak: Anda hanya memiliki izin untuk mengubah tugas milik Anda sendiri.";
                return RedirectToAction(nameof(Index));
            }

            ModelState.Remove("Project");
            ModelState.Remove("Category");
            ModelState.Remove("AssignedToUser");
            ModelState.Remove("Sessions");
            ModelState.Remove("ParentTask");
            ModelState.Remove("ChildTasks");

            if (model.ParentTaskId.HasValue && (model.ParentTaskId.Value <= 0 || model.ParentTaskId.Value == id))
            {
                model.ParentTaskId = null;
            }

            if (!ModelState.IsValid)
            {
                return View(new TaskFormViewModel
                {
                    Task = model,
                    Projects = await _db.Projects.Where(p => p.Status == ProjectStatus.Active).ToListAsync(),
                    Categories = await _db.Categories.ToListAsync(),
                    Users = await _db.Users.OrderBy(u => u.FullName).ToListAsync(),
                    AvailableParentTasks = await _db.Tasks.Where(t => t.Id != id).OrderBy(t => t.Title).ToListAsync(),
                    Milestones = await _db.MasterMilestones.OrderBy(m => m.OrderIndex).ToListAsync()
                });
            }

            // Smart Progress & Status synchronization
            if (model.Progress >= 100)
            {
                model.Progress = 100;
                model.Status = ModelTaskStatus.Done;
            }
            else if (model.Status == ModelTaskStatus.Done)
            {
                model.Progress = 100;
            }
            else if (model.Progress > 0 && model.Status == ModelTaskStatus.Todo)
            {
                model.Status = ModelTaskStatus.InProgress;
            }

            model.UpdatedAt = DateTime.Now;
            _db.Tasks.Update(model);
            await _db.SaveChangesAsync();
            TempData["Success"] = "Tugas berhasil diperbarui!";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var task = await _db.Tasks.FindAsync(id);
            if (task != null)
            {
                var currentUser = await _userManager.GetUserAsync(User);
                var isAdmin = User.IsInRole("Admin");

                if (!TaskPermissionHelper.CanDeleteTask(currentUser, isAdmin, task))
                {
                    TempData["Error"] = "Akses Ditolak: Anda tidak memiliki izin untuk menghapus tugas milik pengguna lain.";
                    return RedirectToAction(nameof(Index));
                }

                _db.Tasks.Remove(task);
                await _db.SaveChangesAsync();
                TempData["Success"] = "Tugas berhasil dihapus!";
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BulkDelete(List<int> ids)
        {
            if (ids == null || !ids.Any())
            {
                TempData["Error"] = "Pilih setidaknya satu tugas untuk dihapus.";
                return RedirectToAction(nameof(Index));
            }

            var currentUser = await _userManager.GetUserAsync(User);
            var isAdmin = User.IsInRole("Admin");

            var allSelectedTasks = await _db.Tasks.Where(t => ids.Contains(t.Id)).ToListAsync();
            var authorizedTasksToDelete = allSelectedTasks.Where(t => TaskPermissionHelper.CanDeleteTask(currentUser, isAdmin, t)).ToList();
            var unauthorizedCount = allSelectedTasks.Count - authorizedTasksToDelete.Count;

            if (!authorizedTasksToDelete.Any())
            {
                TempData["Error"] = "Akses Ditolak: Anda tidak memiliki izin untuk menghapus tugas-tugas milik pengguna lain.";
                return RedirectToAction(nameof(Index));
            }

            var authorizedIds = authorizedTasksToDelete.Select(t => t.Id).ToList();

            // 1. Unlink notes from selected authorized tasks
            var linkedNotes = await _db.Notes.Where(n => n.TaskId != null && authorizedIds.Contains(n.TaskId.Value)).ToListAsync();
            foreach (var note in linkedNotes)
            {
                note.TaskId = null;
            }

            // 2. Remove associated sessions
            var sessions = await _db.Sessions.Where(s => authorizedIds.Contains(s.TaskId)).ToListAsync();
            _db.Sessions.RemoveRange(sessions);

            // 3. Remove authorized tasks
            var count = authorizedTasksToDelete.Count;
            _db.Tasks.RemoveRange(authorizedTasksToDelete);

            await _db.SaveChangesAsync();

            if (unauthorizedCount > 0)
            {
                TempData["Success"] = $"{count} tugas berhasil dihapus. ({unauthorizedCount} tugas milik pengguna lain dilewati karena keterbatasan hak akses).";
            }
            else
            {
                TempData["Success"] = $"Sebanyak {count} tugas berhasil dihapus secara massal (Bulk Delete).";
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ClearAllTasks()
        {
            // Unlink notes from tasks so notes are preserved as standalone
            var linkedNotes = await _db.Notes.Where(n => n.TaskId != null).ToListAsync();
            foreach (var note in linkedNotes)
            {
                note.TaskId = null;
            }

            // Remove all sessions
            var sessions = await _db.Sessions.ToListAsync();
            _db.Sessions.RemoveRange(sessions);

            // Remove all tasks
            var tasks = await _db.Tasks.ToListAsync();
            var count = tasks.Count;
            _db.Tasks.RemoveRange(tasks);

            await _db.SaveChangesAsync();
            TempData["Success"] = $"Seluruh data tugas ({count} tugas) dan sesi kerja berhasil dikosongkan.";
            return RedirectToAction(nameof(Index));
        }

        // ==========================================
        // MANUAL DURATION & SESSION MANAGEMENT
        // ==========================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddManualSession(int taskId, int hours, int minutes, string? sessionDate, string? notes)
        {
            var task = await _db.Tasks.FindAsync(taskId);
            if (task == null) return NotFound();

            var currentUser = await _userManager.GetUserAsync(User);
            var isAdmin = User.IsInRole("Admin");

            if (!TaskPermissionHelper.CanEditTask(currentUser, isAdmin, task))
            {
                TempData["Error"] = "Akses Ditolak: Anda hanya dapat mencatat jam kerja untuk tugas Anda sendiri.";
                return RedirectToAction(nameof(Index));
            }

            var totalSeconds = (long)(hours * 3600 + minutes * 60);
            if (totalSeconds <= 0)
            {
                TempData["Error"] = "Durasi jam atau menit harus lebih dari 0.";
                return RedirectToAction(nameof(Edit), new { id = taskId });
            }

            DateTime date = DateTime.Now;
            if (!string.IsNullOrEmpty(sessionDate) && DateTime.TryParse(sessionDate, out var parsedDate))
            {
                date = parsedDate;
            }

            var session = new WorkSession
            {
                TaskId = taskId,
                StartTime = date,
                EndTime = date.AddSeconds(totalSeconds),
                Duration = totalSeconds,
                Notes = string.IsNullOrWhiteSpace(notes) ? "Log manual" : notes.Trim()
            };

            _db.Sessions.Add(session);
            task.UpdatedAt = DateTime.Now;
            await _db.SaveChangesAsync();

            TempData["Success"] = $"Log waktu {hours} jam {minutes} menit berhasil ditambahkan!";
            return RedirectToAction(nameof(Edit), new { id = taskId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteSession(int sessionId, int taskId)
        {
            var task = await _db.Tasks.FindAsync(taskId);
            var currentUser = await _userManager.GetUserAsync(User);
            var isAdmin = User.IsInRole("Admin");

            if (task != null && !TaskPermissionHelper.CanEditTask(currentUser, isAdmin, task))
            {
                TempData["Error"] = "Akses Ditolak: Anda tidak memiliki izin untuk menghapus sesi pada tugas ini.";
                return RedirectToAction(nameof(Edit), new { id = taskId });
            }

            var session = await _db.Sessions.FindAsync(sessionId);
            if (session != null)
            {
                _db.Sessions.Remove(session);
                await _db.SaveChangesAsync();
                TempData["Success"] = "Sesi kerja berhasil dihapus.";
            }
            return RedirectToAction(nameof(Edit), new { id = taskId });
        }

        // ========================
        // LIVE TIMER ACTIONS (MULTI-USER & MULTI-TASK CONCURRENT)
        // ========================

        [HttpPost]
        public async Task<IActionResult> StartTimer([FromBody] StartTimerDto? dto)
        {
            int id = dto?.TaskId ?? 0;
            if (id == 0 && Request.HasFormContentType && int.TryParse(Request.Form["taskId"], out var fId)) id = fId;
            if (id == 0 && Request.Query.ContainsKey("taskId") && int.TryParse(Request.Query["taskId"], out var qId)) id = qId;

            if (id == 0) return Json(new { success = false, message = "Invalid Task ID" });

            var task = await _db.Tasks.FindAsync(id);
            if (task == null) return Json(new { success = false, message = "Task tidak ditemukan." });

            var currentUser = await _userManager.GetUserAsync(User);
            var isAdmin = User.IsInRole("Admin");

            if (!TaskPermissionHelper.CanEditTask(currentUser, isAdmin, task))
            {
                return Json(new { success = false, message = "Akses Ditolak: Anda hanya dapat menjalankan timer pada tugas milik Anda sendiri." });
            }

            var currentUserId = currentUser?.Id;

            // Check if this specific user is ALREADY running a timer on THIS task
            var existingSession = await _db.Sessions.FirstOrDefaultAsync(s => 
                s.TaskId == id && 
                s.EndTime == null && 
                (s.UserId == currentUserId || (s.UserId == null && task.AssignedToUserId == currentUserId)));

            if (existingSession != null)
            {
                return Json(new { success = true, sessionId = existingSession.Id, taskId = task.Id, taskTitle = task.Title, message = "Timer sudah berjalan untuk tugas ini." });
            }

            // Set task to InProgress if it's Todo
            if (task.Status == ModelTaskStatus.Todo)
            {
                task.Status = ModelTaskStatus.InProgress;
                task.UpdatedAt = DateTime.Now;
            }

            var session = new WorkSession
            {
                TaskId = id,
                UserId = currentUserId,
                StartTime = DateTime.Now
            };
            _db.Sessions.Add(session);
            await _db.SaveChangesAsync();

            return Json(new { success = true, sessionId = session.Id, taskId = task.Id, taskTitle = task.Title });
        }

        [HttpPost]
        public async Task<IActionResult> StopTimer([FromBody] StopTimerDto? dto)
        {
            int id = dto?.SessionId ?? 0;
            if (id == 0 && Request.HasFormContentType && int.TryParse(Request.Form["sessionId"], out var fId)) id = fId;
            if (id == 0 && Request.Query.ContainsKey("sessionId") && int.TryParse(Request.Query["sessionId"], out var qId)) id = qId;

            int taskId = 0;
            if (Request.HasFormContentType && int.TryParse(Request.Form["taskId"], out var fTId)) taskId = fTId;
            if (taskId == 0 && Request.Query.ContainsKey("taskId") && int.TryParse(Request.Query["taskId"], out var qTId)) taskId = qTId;

            var currentUser = await _userManager.GetUserAsync(User);
            var currentUserId = currentUser?.Id;
            var isAdmin = User.IsInRole("Admin");

            WorkSession? session = null;
            if (id > 0)
            {
                session = await _db.Sessions.Include(s => s.Task).FirstOrDefaultAsync(s => s.Id == id);
            }
            else if (taskId > 0)
            {
                session = await _db.Sessions.Include(s => s.Task).FirstOrDefaultAsync(s => 
                    s.TaskId == taskId && 
                    s.EndTime == null && 
                    (s.UserId == currentUserId || (s.UserId == null && s.Task != null && s.Task.AssignedToUserId == currentUserId)));
            }
            else
            {
                session = await _db.Sessions.Include(s => s.Task).OrderByDescending(s => s.StartTime).FirstOrDefaultAsync(s => 
                    s.EndTime == null && 
                    (s.UserId == currentUserId || (s.UserId == null && s.Task != null && s.Task.AssignedToUserId == currentUserId)));
            }

            if (session == null) return Json(new { success = false, message = "Tidak ada sesi timer aktif yang ditemukan." });

            // Authorization check
            if (!isAdmin && session.UserId != currentUserId && (session.Task != null && session.Task.AssignedToUserId != currentUserId))
            {
                return Json(new { success = false, message = "Akses Ditolak: Anda tidak berhak menghentikan timer pengguna lain." });
            }

            session.EndTime = DateTime.Now;
            session.Duration = Math.Max(1, (long)(session.EndTime.Value - session.StartTime).TotalSeconds);
            await _db.SaveChangesAsync();

            return Json(new { success = true, duration = session.Duration, sessionId = session.Id, taskId = session.TaskId });
        }

        [HttpGet]
        public async Task<IActionResult> GetRunningTimer()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var currentUserId = currentUser?.Id;

            var runningSessions = await _db.Sessions
                .Include(s => s.Task)
                    .ThenInclude(t => t != null ? t.Project : null)
                .Where(s => s.EndTime == null && (s.UserId == currentUserId || (s.UserId == null && s.Task != null && s.Task.AssignedToUserId == currentUserId)))
                .OrderByDescending(s => s.StartTime)
                .ToListAsync();

            if (!runningSessions.Any())
            {
                return Json(new { running = false, count = 0, timers = new object[0] });
            }

            var timers = runningSessions.Select(s => new
            {
                sessionId = s.Id,
                taskId = s.TaskId,
                taskCode = s.Task?.TaskCode ?? $"TSK-{s.TaskId:D4}",
                taskTitle = s.Task?.Title ?? "Tugas",
                projectName = s.Task?.Project?.Name ?? "-",
                startTime = s.StartTime.ToString("yyyy-MM-dd HH:mm:ss"),
                elapsed = Math.Max(0, (long)(DateTime.Now - s.StartTime).TotalSeconds)
            }).ToList();

            var primary = timers.First();

            return Json(new
            {
                running = true,
                count = timers.Count,
                sessionId = primary.sessionId,
                taskId = primary.taskId,
                taskCode = primary.taskCode,
                taskTitle = primary.taskTitle,
                projectName = primary.projectName,
                elapsed = primary.elapsed,
                timers = timers
            });
        }

        [HttpGet]
        public async Task<IActionResult> Kanban(string? search, int? projectId, string? assigneeId, string? priority)
        {
            var query = _db.Tasks
                .Include(t => t.Project)
                .Include(t => t.Category)
                .Include(t => t.AssignedToUser)
                .Include(t => t.ParentTask)
                .Include(t => t.ChildTasks)
                .Include(t => t.Sessions)
                .AsQueryable();

            if (projectId.HasValue)
                query = query.Where(t => t.ProjectId == projectId);

            if (!string.IsNullOrEmpty(assigneeId))
                query = query.Where(t => t.AssignedToUserId == assigneeId);

            if (!string.IsNullOrEmpty(priority) && Enum.TryParse<TaskPriority>(priority, out var p))
                query = query.Where(t => t.Priority == p);

            if (!string.IsNullOrEmpty(search))
                query = query.Where(t => t.Title.Contains(search) || (t.Description != null && t.Description.Contains(search)));

            var tasks = await query.OrderByDescending(t => t.UpdatedAt).ToListAsync();

            // Logika Kanban: Cukup tampilkan non parent task, kecuali jika semua child task selesai semua (Done)!
            var kanbanTasks = tasks.Where(t =>
                !t.ChildTasks.Any() ||
                (t.ChildTasks.Any() && t.ChildTasks.All(c => c.Status == ModelTaskStatus.Done))
            ).ToList();

            ViewBag.Search = search;
            ViewBag.SelectedProject = projectId;
            ViewBag.SelectedAssignee = assigneeId;
            ViewBag.SelectedPriority = priority;
            ViewBag.Projects = await _db.Projects.OrderBy(p => p.Name).ToListAsync();
            ViewBag.Users = await _db.Users.OrderBy(u => u.FullName).ToListAsync();

            return View(kanbanTasks);
        }

        [HttpPost]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> UpdateKanbanStatus([FromBody] UpdateKanbanStatusDto dto)
        {
            if (dto == null || dto.TaskId <= 0)
                return Json(new { success = false, message = "Data tidak valid." });

            var task = await _db.Tasks.FindAsync(dto.TaskId);
            if (task == null)
                return Json(new { success = false, message = "Tugas tidak ditemukan." });

            var currentUser = await _userManager.GetUserAsync(User);
            var isAdmin = User.IsInRole("Admin");

            if (!TaskPermissionHelper.CanEditTask(currentUser, isAdmin, task))
            {
                return Json(new { success = false, message = "Akses Ditolak: Anda hanya memiliki izin untuk mengubah status tugas milik Anda sendiri." });
            }

            if (!Enum.TryParse<ModelTaskStatus>(dto.NewStatus, true, out var targetStatus))
                return Json(new { success = false, message = "Status tidak dikenali." });

            var prevStatus = task.Status;
            task.Status = targetStatus;

            // Logic: jika pindah ke Done atau progress >= 100, set progress = 100
            if (targetStatus == ModelTaskStatus.Done)
            {
                task.Progress = 100;
            }
            else if (prevStatus == ModelTaskStatus.Done && targetStatus == ModelTaskStatus.InProgress)
            {
                task.Progress = dto.Progress.HasValue && dto.Progress.Value < 100 ? dto.Progress.Value : 50;
            }
            else if (prevStatus == ModelTaskStatus.Done && targetStatus == ModelTaskStatus.Todo)
            {
                task.Progress = dto.Progress.HasValue && dto.Progress.Value < 100 ? dto.Progress.Value : 0;
            }
            else if (dto.Progress.HasValue)
            {
                task.Progress = Math.Clamp(dto.Progress.Value, 0, 100);
                if (task.Progress >= 100)
                {
                    task.Status = ModelTaskStatus.Done;
                }
            }

            task.UpdatedAt = DateTime.Now;
            await _db.SaveChangesAsync();

            return Json(new
            {
                success = true,
                taskId = task.Id,
                status = task.Status.ToString(),
                progress = task.Progress,
                message = $"Status berhasil diubah ke {task.Status} (Progress: {task.Progress}%)"
            });
        }

        [HttpPost]
        public async Task<IActionResult> UpdateStatus(int id, string status)
        {
            var task = await _db.Tasks.FindAsync(id);
            if (task == null) return Json(new { success = false, message = "Tugas tidak ditemukan." });

            var currentUser = await _userManager.GetUserAsync(User);
            var isAdmin = User.IsInRole("Admin");

            if (!TaskPermissionHelper.CanEditTask(currentUser, isAdmin, task))
            {
                return Json(new { success = false, message = "Akses Ditolak: Anda hanya dapat mengubah status tugas milik Anda sendiri." });
            }

            if (Enum.TryParse<ModelTaskStatus>(status, out var s))
            {
                task.Status = s;
                if (s == ModelTaskStatus.Done) task.Progress = 100;
                task.UpdatedAt = DateTime.Now;
                await _db.SaveChangesAsync();
                return Json(new { success = true });
            }
            return Json(new { success = false, message = "Status tidak valid." });
        }

        // ── EXPORT KE FORMAT ARMS EXCEL (SELECTED IDS & PERIOD SUPPORT) ──────────
        [HttpGet]
        public async Task<IActionResult> ExportArmsExcel(
            string? selectedIds,
            string? period,
            DateTime? startDate,
            DateTime? endDate,
            int? projectId,
            string? status,
            string? priority,
            string? assigneeId,
            string? milestone,
            string? search)
        {
            var query = _db.Tasks
                .Include(t => t.Project)
                .Include(t => t.Category)
                .Include(t => t.AssignedToUser)
                .Include(t => t.ParentTask)
                .Include(t => t.ChildTasks)
                .AsQueryable();

            query = ApplyTaskExportFilters(query, selectedIds, period, startDate, endDate, projectId, status, priority, assigneeId, milestone, search);

            var tasks = await query.OrderBy(t => t.ProjectId).ThenBy(t => t.Id).ToListAsync();

            // Retrieve team email defaults for BA, Tester, Infra
            var users = await _db.Users.ToListAsync();
            var baUsers = users.Where(u => !string.IsNullOrEmpty(u.JobTitle) && (u.JobTitle.Contains("Analyst", StringComparison.OrdinalIgnoreCase) || u.JobTitle.Contains("BA", StringComparison.OrdinalIgnoreCase) || u.JobTitle.Contains("Product", StringComparison.OrdinalIgnoreCase))).Select(u => u.Email).Where(e => !string.IsNullOrEmpty(e)).ToList();
            string defaultBaEmails = baUsers.Any() ? string.Join(";", baUsers) : "syafix.said@elistec.com;athallah.bariq@elistec.com";

            var testerUsers = users.Where(u => !string.IsNullOrEmpty(u.JobTitle) && (u.JobTitle.Contains("QA", StringComparison.OrdinalIgnoreCase) || u.JobTitle.Contains("Tester", StringComparison.OrdinalIgnoreCase) || u.JobTitle.Contains("DevOps", StringComparison.OrdinalIgnoreCase))).Select(u => u.Email).Where(e => !string.IsNullOrEmpty(e)).ToList();
            string defaultTesterEmails = testerUsers.Any() ? string.Join(";", testerUsers) : "mohammad.danang@elistec.com";

            using var wb = new XLWorkbook();
            var ws = wb.Worksheets.Add("ARMS Export");

            // Row 1 Headers (Exact ARMS 21-column specification from uploaded spreadsheet)
            var headers = new[]
            {
                "Task Code",            // Col A (1)
                "Project",              // Col B (2)
                "Requirement",          // Col C (3)
                "Title",                // Col D (4)
                "Status",               // Col E (5)
                "Priority",             // Col F (6)
                "Jenis Task",           // Col G (7)
                "Module",               // Col H (8)
                "Tipe Bugs",            // Col I (9)
                "Progress",             // Col J (10)
                "Start Date",           // Col K (11)
                "Due Date",             // Col L (12)
                "Completed Date",       // Col M (13)
                "Developer",            // Col N (14)
                "BA Emails",            // Col O (15)
                "Infra Emails",         // Col P (16)
                "Master Data Emails",   // Col Q (17)
                "Tester Emails",        // Col R (18)
                "Kendala",              // Col S (19)
                "Solusi",               // Col T (20)
                "Created At"            // Col U (21)
            };

            for (int i = 0; i < headers.Length; i++)
            {
                var cell = ws.Cell(1, i + 1);
                cell.Value = headers[i];
                cell.Style.Font.Bold = true;
                cell.Style.Font.FontColor = XLColor.Black;
                cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#F1F5F9"); // Clean Slate-100 Header
                cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
                cell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                cell.Style.Border.OutsideBorderColor = XLColor.FromHtml("#CBD5E1");
            }

            int rowIdx = 2;
            foreach (var task in tasks)
            {
                // Status standard ARMS: TODO, IN_PROGRESS, DONE
                string armsStatus = task.Status switch
                {
                    ModelTaskStatus.Done => "DONE",
                    ModelTaskStatus.InProgress => "IN_PROGRESS",
                    ModelTaskStatus.Overdue => "TODO",
                    _ => "TODO"
                };

                string armsPriority = task.Priority.ToString().ToUpper();
                
                string armsJenisTask = task.Category != null 
                    ? (task.Category.Name.ToUpper().Contains("ENHANCE") ? "ENHANCEMENT" 
                       : task.Category.Name.ToUpper().Contains("BUG") ? "BUG_FIX" 
                       : task.Category.Name.ToUpper().Contains("APP") ? "NEW_APP" 
                       : task.Category.Name.ToUpper().Replace(" ", "_")) 
                    : "ENHANCEMENT";

                string armsModule = task.Category?.Name ?? task.Project?.Name ?? "TCES";
                string reqTitle = task.ParentTask != null ? $"TSK-{task.ParentTask.Id:D4} {task.ParentTask.Title}" : "";

                ws.Cell(rowIdx, 1).Value = $"TSK-{task.Id:D4}";
                ws.Cell(rowIdx, 2).Value = task.Project?.Name ?? "";
                ws.Cell(rowIdx, 3).Value = reqTitle;
                ws.Cell(rowIdx, 4).Value = task.Title;
                ws.Cell(rowIdx, 5).Value = armsStatus;
                ws.Cell(rowIdx, 6).Value = armsPriority;
                ws.Cell(rowIdx, 7).Value = armsJenisTask;
                ws.Cell(rowIdx, 8).Value = armsModule;
                ws.Cell(rowIdx, 9).Value = ""; // Tipe Bugs
                ws.Cell(rowIdx, 10).Value = task.Progress;
                ws.Cell(rowIdx, 11).Value = task.StartDate?.ToString("yyyy-MM-dd") ?? task.CreatedAt.ToString("yyyy-MM-dd");
                ws.Cell(rowIdx, 12).Value = task.DueDate?.ToString("yyyy-MM-dd") ?? "";
                ws.Cell(rowIdx, 13).Value = task.Status == ModelTaskStatus.Done ? task.UpdatedAt.ToString("yyyy-MM-dd") : "";
                ws.Cell(rowIdx, 14).Value = task.AssignedToUser?.Email ?? "";
                ws.Cell(rowIdx, 15).Value = defaultBaEmails;
                ws.Cell(rowIdx, 16).Value = ""; // Infra Emails
                ws.Cell(rowIdx, 17).Value = ""; // Master Data Emails
                ws.Cell(rowIdx, 18).Value = defaultTesterEmails;
                ws.Cell(rowIdx, 19).Value = task.Obstacle ?? "";
                ws.Cell(rowIdx, 20).Value = task.Solution ?? "";
                ws.Cell(rowIdx, 21).Value = task.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss");

                rowIdx++;
            }

            // Styling table border and column widths
            var dataRange = ws.Range(1, 1, Math.Max(2, rowIdx - 1), headers.Length);
            dataRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            dataRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
            dataRange.Style.Border.OutsideBorderColor = XLColor.FromHtml("#CBD5E1");
            dataRange.Style.Border.InsideBorderColor = XLColor.FromHtml("#E2E8F0");
            ws.Columns().AdjustToContents(8, 50);

            using var stream = new MemoryStream();
            wb.SaveAs(stream);
            stream.Position = 0;

            var fileName = $"ARMS_Export_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
            return File(stream.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                fileName);
        }

        // ── EXPORT KE FORMAT STANDAR EXCEL (SELECTED IDS & PERIOD SUPPORT) ──────────
        [HttpGet]
        public async Task<IActionResult> ExportStandardExcel(
            string? selectedIds,
            string? period,
            DateTime? startDate,
            DateTime? endDate,
            int? projectId,
            string? status,
            string? priority,
            string? assigneeId,
            string? milestone,
            string? search)
        {
            var query = _db.Tasks
                .Include(t => t.Project)
                .Include(t => t.Category)
                .Include(t => t.AssignedToUser)
                .Include(t => t.ParentTask)
                .AsQueryable();

            query = ApplyTaskExportFilters(query, selectedIds, period, startDate, endDate, projectId, status, priority, assigneeId, milestone, search);

            var tasks = await query.OrderBy(t => t.ProjectId).ThenBy(t => t.Id).ToListAsync();

            using var wb = new XLWorkbook();
            var ws = wb.Worksheets.Add("Daftar Tugas");

            var headers = new[]
            {
                "No",
                "Kode Task",
                "Induk Task",
                "Nama Task",
                "Kategori",
                "Nama Project",
                "PIC",
                "Prioritas",
                "Status",
                "Progress (%)",
                "Milestone SDLC",
                "Tanggal Mulai",
                "Tanggal Berakhir (Deadline)",
                "Kendala",
                "Solusi",
                "Dibuat Pada"
            };

            for (int i = 0; i < headers.Length; i++)
            {
                var cell = ws.Cell(1, i + 1);
                cell.Value = headers[i];
                cell.Style.Font.Bold = true;
                cell.Style.Font.FontColor = XLColor.White;
                cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#6366F1"); // Indigo Primary
                cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                cell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                cell.Style.Border.OutsideBorderColor = XLColor.FromHtml("#4F46E5");
            }

            int rowIdx = 2;
            int counter = 1;
            foreach (var task in tasks)
            {
                string parentCode = task.ParentTask != null ? $"TSK-{task.ParentTask.Id:D4}" : "-";

                ws.Cell(rowIdx, 1).Value = counter++;
                ws.Cell(rowIdx, 2).Value = $"TSK-{task.Id:D4}";
                ws.Cell(rowIdx, 3).Value = parentCode;
                ws.Cell(rowIdx, 4).Value = task.Title;
                ws.Cell(rowIdx, 5).Value = task.Category?.Name ?? "-";
                ws.Cell(rowIdx, 6).Value = task.Project?.Name ?? "-";
                ws.Cell(rowIdx, 7).Value = task.AssignedToUser?.FullName ?? task.AssignedToUser?.Email ?? "-";
                ws.Cell(rowIdx, 8).Value = task.Priority.ToString();
                ws.Cell(rowIdx, 9).Value = task.Status.ToString();
                ws.Cell(rowIdx, 10).Value = task.Progress;
                ws.Cell(rowIdx, 11).Value = task.Milestone ?? "Implementation";
                ws.Cell(rowIdx, 12).Value = task.StartDate?.ToString("yyyy-MM-dd") ?? "";
                ws.Cell(rowIdx, 13).Value = task.DueDate?.ToString("yyyy-MM-dd") ?? "";
                ws.Cell(rowIdx, 14).Value = task.Obstacle ?? "";
                ws.Cell(rowIdx, 15).Value = task.Solution ?? "";
                ws.Cell(rowIdx, 16).Value = task.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss");

                // Highlight status cell
                var statusCell = ws.Cell(rowIdx, 9);
                if (task.Status == ModelTaskStatus.Done)
                {
                    statusCell.Style.Font.FontColor = XLColor.FromHtml("#059669");
                    statusCell.Style.Font.Bold = true;
                }
                else if (task.Status == ModelTaskStatus.InProgress)
                {
                    statusCell.Style.Font.FontColor = XLColor.FromHtml("#2563EB");
                    statusCell.Style.Font.Bold = true;
                }

                rowIdx++;
            }

            var dataRange = ws.Range(1, 1, Math.Max(2, rowIdx - 1), headers.Length);
            dataRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            dataRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
            dataRange.Style.Border.OutsideBorderColor = XLColor.FromHtml("#CBD5E1");
            dataRange.Style.Border.InsideBorderColor = XLColor.FromHtml("#E2E8F0");
            ws.Columns().AdjustToContents(8, 50);

            using var stream = new MemoryStream();
            wb.SaveAs(stream);
            stream.Position = 0;

            var fileName = $"Tasks_Export_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
            return File(stream.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                fileName);
        }

        #region Filter Helpers
        private IQueryable<WorkTask> ApplyTaskExportFilters(
            IQueryable<WorkTask> query,
            string? selectedIds,
            string? period,
            DateTime? startDate,
            DateTime? endDate,
            int? projectId,
            string? status,
            string? priority,
            string? assigneeId,
            string? milestone,
            string? search)
        {
            if (!string.IsNullOrWhiteSpace(selectedIds))
            {
                var idList = selectedIds.Split(new[] { ',', ';', ' ' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .Select(s => int.TryParse(s, out var i) ? i : 0)
                    .Where(i => i > 0)
                    .Distinct()
                    .ToList();

                if (idList.Any())
                {
                    query = query.Where(t => idList.Contains(t.Id));
                }
            }

            if (projectId.HasValue) query = query.Where(t => t.ProjectId == projectId.Value);
            if (!string.IsNullOrEmpty(status) && Enum.TryParse<ModelTaskStatus>(status, out var s)) query = query.Where(t => t.Status == s);
            if (!string.IsNullOrEmpty(priority) && Enum.TryParse<TaskPriority>(priority, out var p)) query = query.Where(t => t.Priority == p);
            if (!string.IsNullOrEmpty(assigneeId)) query = query.Where(t => t.AssignedToUserId == assigneeId);
            if (!string.IsNullOrEmpty(milestone)) query = query.Where(t => t.Milestone == milestone);
            if (!string.IsNullOrEmpty(search)) query = query.Where(t => t.Title.Contains(search) || (t.Description != null && t.Description.Contains(search)));

            var now = DateTime.Now;
            var todayStart = now.Date;
            var todayEnd = todayStart.AddDays(1).AddTicks(-1);

            if (!string.IsNullOrWhiteSpace(period))
            {
                switch (period.ToLower().Trim())
                {
                    case "today":
                    case "harian":
                    case "hari_ini":
                        query = query.Where(t =>
                            (t.StartDate.HasValue && t.StartDate.Value >= todayStart && t.StartDate.Value <= todayEnd) ||
                            (t.CreatedAt >= todayStart && t.CreatedAt <= todayEnd));
                        break;
                    case "this_week":
                    case "mingguan":
                    case "minggu_ini":
                        int diff = (7 + (now.DayOfWeek - DayOfWeek.Monday)) % 7;
                        var weekStart = now.AddDays(-1 * diff).Date;
                        var weekEnd = weekStart.AddDays(7).AddTicks(-1);
                        query = query.Where(t =>
                            (t.StartDate.HasValue && t.StartDate.Value >= weekStart && t.StartDate.Value <= weekEnd) ||
                            (t.CreatedAt >= weekStart && t.CreatedAt <= weekEnd));
                        break;
                    case "this_month":
                    case "bulanan":
                    case "bulan_ini":
                        var monthStart = new DateTime(now.Year, now.Month, 1);
                        var monthEnd = monthStart.AddMonths(1).AddTicks(-1);
                        query = query.Where(t =>
                            (t.StartDate.HasValue && t.StartDate.Value >= monthStart && t.StartDate.Value <= monthEnd) ||
                            (t.CreatedAt >= monthStart && t.CreatedAt <= monthEnd));
                        break;
                    case "custom":
                    case "kustom":
                        if (startDate.HasValue)
                        {
                            var sStart = startDate.Value.Date;
                            query = query.Where(t => (t.StartDate.HasValue && t.StartDate.Value >= sStart) || (t.DueDate.HasValue && t.DueDate.Value >= sStart) || t.CreatedAt >= sStart);
                        }
                        if (endDate.HasValue)
                        {
                            var sEnd = endDate.Value.Date.AddDays(1).AddTicks(-1);
                            query = query.Where(t => (t.StartDate.HasValue && t.StartDate.Value <= sEnd) || (t.DueDate.HasValue && t.DueDate.Value <= sEnd) || t.CreatedAt <= sEnd);
                        }
                        break;
                }
            }
            else if (startDate.HasValue || endDate.HasValue)
            {
                if (startDate.HasValue)
                {
                    var sStart = startDate.Value.Date;
                    query = query.Where(t => (t.StartDate.HasValue && t.StartDate.Value >= sStart) || (t.DueDate.HasValue && t.DueDate.Value >= sStart) || t.CreatedAt >= sStart);
                }
                if (endDate.HasValue)
                {
                    var sEnd = endDate.Value.Date.AddDays(1).AddTicks(-1);
                    query = query.Where(t => (t.StartDate.HasValue && t.StartDate.Value <= sEnd) || (t.DueDate.HasValue && t.DueDate.Value <= sEnd) || t.CreatedAt <= sEnd);
                }
            }

            return query;
        }
        #endregion
    }
}
