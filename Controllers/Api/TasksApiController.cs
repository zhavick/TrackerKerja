using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TrackerKerja.Data;
using TrackerKerja.Models;
using TrackerKerja.ViewModels;
using ModelTaskStatus = TrackerKerja.Models.TaskStatus;

namespace TrackerKerja.Controllers.Api
{
    [ApiController]
    [Route("api/tasks")]
    [Produces("application/json")]
    public class TasksApiController : ControllerBase
    {
        private readonly AppDbContext _db;

        public TasksApiController(AppDbContext db)
        {
            _db = db;
        }

        /// <summary>
        /// Mengambil daftar seluruh tugas dengan opsi filter dan pagination (GET)
        /// </summary>
        /// <param name="search">Pencarian judul atau deskripsi tugas</param>
        /// <param name="status">Filter status (Todo, InProgress, Done, Overdue)</param>
        /// <param name="priority">Filter prioritas (Low, Medium, High, Critical)</param>
        /// <param name="projectId">Filter ID Proyek</param>
        /// <param name="assigneeId">Filter ID Pengguna / PIC</param>
        /// <param name="milestone">Filter Milestone SDLC (Requirement Analysis, System Design, Implementation, Testing dan QA, Deployment, Maintenance)</param>
        /// <param name="parentTaskId">Filter ID Induk Tugas (null jika tugas utama)</param>
        /// <param name="page">Halaman (default: 1)</param>
        /// <param name="pageSize">Jumlah item per halaman (default: 10, max: 100)</param>
        [HttpGet]
        [ProducesResponseType(typeof(ApiResponse<PagedResult<TaskResponseDto>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAll(
            [FromQuery] string? search,
            [FromQuery] ModelTaskStatus? status,
            [FromQuery] TaskPriority? priority,
            [FromQuery] int? projectId,
            [FromQuery] string? assigneeId,
            [FromQuery] string? milestone,
            [FromQuery] int? parentTaskId,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 10;
            if (pageSize > 100) pageSize = 100;

            var query = _db.Tasks
                .Include(t => t.Project)
                .Include(t => t.Category)
                .Include(t => t.AssignedToUser)
                .Include(t => t.ParentTask)
                .Include(t => t.ChildTasks)
                .Include(t => t.Sessions)
                .AsNoTracking()
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim().ToLower();
                query = query.Where(t => t.Title.ToLower().Contains(term) || (t.Description != null && t.Description.ToLower().Contains(term)));
            }

            if (status.HasValue)
                query = query.Where(t => t.Status == status.Value);

            if (priority.HasValue)
                query = query.Where(t => t.Priority == priority.Value);

            if (projectId.HasValue)
                query = query.Where(t => t.ProjectId == projectId.Value);

            if (!string.IsNullOrWhiteSpace(assigneeId))
                query = query.Where(t => t.AssignedToUserId == assigneeId);

            if (!string.IsNullOrWhiteSpace(milestone))
                query = query.Where(t => t.Milestone == milestone);

            if (parentTaskId.HasValue)
                query = query.Where(t => t.ParentTaskId == parentTaskId.Value);

            var totalItems = await query.CountAsync();
            var tasks = await query
                .OrderByDescending(t => t.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var taskDtos = tasks.Select(MapToResponseDto).ToList();

            var result = new PagedResult<TaskResponseDto>
            {
                Items = taskDtos,
                TotalItems = totalItems,
                Page = page,
                PageSize = pageSize
            };

            return Ok(ApiResponse<PagedResult<TaskResponseDto>>.Ok(result, $"Berhasil mengambil {taskDtos.Count} tugas."));
        }

        /// <summary>
        /// Mengambil ringkasan metrik statistik tugas (GET /summary)
        /// </summary>
        [HttpGet("summary")]
        [ProducesResponseType(typeof(ApiResponse<TaskSummaryDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetSummary()
        {
            var tasks = await _db.Tasks
                .Include(t => t.Sessions)
                .AsNoTracking()
                .ToListAsync();

            var totalWorkSec = tasks.SelectMany(t => t.Sessions).Sum(s => s.Duration);
            var h = totalWorkSec / 3600;
            var m = (totalWorkSec % 3600) / 60;

            var summary = new TaskSummaryDto
            {
                TotalTasks = tasks.Count,
                TodoTasks = tasks.Count(t => t.Status == ModelTaskStatus.Todo),
                InProgressTasks = tasks.Count(t => t.Status == ModelTaskStatus.InProgress),
                DoneTasks = tasks.Count(t => t.Status == ModelTaskStatus.Done),
                OverdueTasks = tasks.Count(t => t.Status == ModelTaskStatus.Overdue),
                TotalWorkSeconds = totalWorkSec,
                TotalWorkFormatted = $"{h}j {m}m"
            };

            return Ok(ApiResponse<TaskSummaryDto>.Ok(summary, "Ringkasan statistik tugas berhasil diambil."));
        }

        /// <summary>
        /// Mengambil detail satu tugas berdasarkan ID (GET /api/tasks/{id})
        /// </summary>
        /// <param name="id">ID Tugas</param>
        [HttpGet("{id:int}")]
        [ProducesResponseType(typeof(ApiResponse<TaskResponseDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetById(int id)
        {
            var task = await _db.Tasks
                .Include(t => t.Project)
                .Include(t => t.Category)
                .Include(t => t.AssignedToUser)
                .Include(t => t.ParentTask)
                .Include(t => t.ChildTasks)
                .Include(t => t.Sessions)
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.Id == id);

            if (task == null)
            {
                return NotFound(ApiResponse<TaskResponseDto>.Fail($"Tugas dengan ID {id} tidak ditemukan."));
            }

            return Ok(ApiResponse<TaskResponseDto>.Ok(MapToResponseDto(task), "Detail tugas berhasil diambil."));
        }

        /// <summary>
        /// Membuat tugas baru (POST /api/tasks)
        /// </summary>
        /// <param name="dto">Payload data tugas baru</param>
        [HttpPost]
        [ProducesResponseType(typeof(ApiResponse<TaskResponseDto>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Create([FromBody] CreateTaskRequestDto dto)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();
                return BadRequest(ApiResponse<TaskResponseDto>.Fail("Validasi payload gagal.", errors));
            }

            // Validasi Project jika diisi
            if (dto.ProjectId.HasValue && !await _db.Projects.AnyAsync(p => p.Id == dto.ProjectId.Value))
            {
                return BadRequest(ApiResponse<TaskResponseDto>.Fail($"Project dengan ID {dto.ProjectId.Value} tidak ditemukan."));
            }

            // Validasi Category jika diisi
            if (dto.CategoryId.HasValue && !await _db.Categories.AnyAsync(c => c.Id == dto.CategoryId.Value))
            {
                return BadRequest(ApiResponse<TaskResponseDto>.Fail($"Kategori dengan ID {dto.CategoryId.Value} tidak ditemukan."));
            }

            // Validasi Assignee jika diisi
            if (!string.IsNullOrWhiteSpace(dto.AssignedToUserId) && !await _db.Users.AnyAsync(u => u.Id == dto.AssignedToUserId))
            {
                return BadRequest(ApiResponse<TaskResponseDto>.Fail($"Pengguna PIC dengan ID '{dto.AssignedToUserId}' tidak ditemukan."));
            }

            // Validasi Parent Task jika diisi
            if (dto.ParentTaskId.HasValue && !await _db.Tasks.AnyAsync(t => t.Id == dto.ParentTaskId.Value))
            {
                return BadRequest(ApiResponse<TaskResponseDto>.Fail($"Tugas induk (Parent Task) dengan ID {dto.ParentTaskId.Value} tidak ditemukan."));
            }

            var task = new WorkTask
            {
                Title = dto.Title.Trim(),
                Description = dto.Description?.Trim(),
                Obstacle = dto.Obstacle?.Trim(),
                Solution = dto.Solution?.Trim(),
                ProjectId = dto.ProjectId,
                CategoryId = dto.CategoryId,
                AssignedToUserId = string.IsNullOrWhiteSpace(dto.AssignedToUserId) ? null : dto.AssignedToUserId,
                Priority = dto.Priority,
                Status = dto.Status,
                Progress = dto.Status == ModelTaskStatus.Done ? 100 : Math.Clamp(dto.Progress, 0, 100),
                StartDate = dto.StartDate,
                DueDate = dto.DueDate,
                Milestone = string.IsNullOrWhiteSpace(dto.Milestone) ? "Implementation" : dto.Milestone.Trim(),
                Tags = dto.Tags?.Trim(),
                ParentTaskId = dto.ParentTaskId,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };

            _db.Tasks.Add(task);
            await _db.SaveChangesAsync();

            // Reload for navigation properties
            var createdTask = await _db.Tasks
                .Include(t => t.Project)
                .Include(t => t.Category)
                .Include(t => t.AssignedToUser)
                .Include(t => t.ParentTask)
                .Include(t => t.ChildTasks)
                .Include(t => t.Sessions)
                .AsNoTracking()
                .FirstAsync(t => t.Id == task.Id);

            var responseDto = MapToResponseDto(createdTask);
            return CreatedAtAction(nameof(GetById), new { id = task.Id }, ApiResponse<TaskResponseDto>.Ok(responseDto, "Tugas berhasil dibuat."));
        }

        /// <summary>
        /// Memperbarui data tugas secara menyeluruh (PUT /api/tasks/{id})
        /// </summary>
        /// <param name="id">ID Tugas yang akan diperbarui</param>
        /// <param name="dto">Payload pembaruan data tugas</param>
        [HttpPut("{id:int}")]
        [ProducesResponseType(typeof(ApiResponse<TaskResponseDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateTaskRequestDto dto)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();
                return BadRequest(ApiResponse<TaskResponseDto>.Fail("Validasi payload gagal.", errors));
            }

            var task = await _db.Tasks.FirstOrDefaultAsync(t => t.Id == id);
            if (task == null)
            {
                return NotFound(ApiResponse<TaskResponseDto>.Fail($"Tugas dengan ID {id} tidak ditemukan."));
            }

            // Cegah self-parent loop
            if (dto.ParentTaskId.HasValue && dto.ParentTaskId.Value == id)
            {
                return BadRequest(ApiResponse<TaskResponseDto>.Fail("Tugas tidak dapat dijadikan induk bagi dirinya sendiri."));
            }

            // Validasi Project jika diisi
            if (dto.ProjectId.HasValue && !await _db.Projects.AnyAsync(p => p.Id == dto.ProjectId.Value))
            {
                return BadRequest(ApiResponse<TaskResponseDto>.Fail($"Project dengan ID {dto.ProjectId.Value} tidak ditemukan."));
            }

            // Validasi Category jika diisi
            if (dto.CategoryId.HasValue && !await _db.Categories.AnyAsync(c => c.Id == dto.CategoryId.Value))
            {
                return BadRequest(ApiResponse<TaskResponseDto>.Fail($"Kategori dengan ID {dto.CategoryId.Value} tidak ditemukan."));
            }

            // Validasi Assignee jika diisi
            if (!string.IsNullOrWhiteSpace(dto.AssignedToUserId) && !await _db.Users.AnyAsync(u => u.Id == dto.AssignedToUserId))
            {
                return BadRequest(ApiResponse<TaskResponseDto>.Fail($"Pengguna PIC dengan ID '{dto.AssignedToUserId}' tidak ditemukan."));
            }

            // Validasi Parent Task jika diisi
            if (dto.ParentTaskId.HasValue && !await _db.Tasks.AnyAsync(t => t.Id == dto.ParentTaskId.Value))
            {
                return BadRequest(ApiResponse<TaskResponseDto>.Fail($"Tugas induk (Parent Task) dengan ID {dto.ParentTaskId.Value} tidak ditemukan."));
            }

            task.Title = dto.Title.Trim();
            task.Description = dto.Description?.Trim();
            task.Obstacle = dto.Obstacle?.Trim();
            task.Solution = dto.Solution?.Trim();
            task.ProjectId = dto.ProjectId;
            task.CategoryId = dto.CategoryId;
            task.AssignedToUserId = string.IsNullOrWhiteSpace(dto.AssignedToUserId) ? null : dto.AssignedToUserId;
            task.Priority = dto.Priority;
            task.Status = dto.Status;
            task.Progress = dto.Status == ModelTaskStatus.Done ? 100 : Math.Clamp(dto.Progress, 0, 100);
            task.StartDate = dto.StartDate;
            task.DueDate = dto.DueDate;
            if (!string.IsNullOrWhiteSpace(dto.Milestone))
            {
                task.Milestone = dto.Milestone.Trim();
            }
            task.Tags = dto.Tags?.Trim();
            task.ParentTaskId = dto.ParentTaskId;
            task.UpdatedAt = DateTime.Now;

            await _db.SaveChangesAsync();

            // Reload for navigation properties
            var updatedTask = await _db.Tasks
                .Include(t => t.Project)
                .Include(t => t.Category)
                .Include(t => t.AssignedToUser)
                .Include(t => t.ParentTask)
                .Include(t => t.ChildTasks)
                .Include(t => t.Sessions)
                .AsNoTracking()
                .FirstAsync(t => t.Id == task.Id);

            var responseDto = MapToResponseDto(updatedTask);
            return Ok(ApiResponse<TaskResponseDto>.Ok(responseDto, "Tugas berhasil diperbarui."));
        }

        /// <summary>
        /// Memperbarui status dan progress tugas secara spesifik (PUT /api/tasks/{id}/status)
        /// </summary>
        /// <param name="id">ID Tugas</param>
        /// <param name="dto">Payload status dan progress</param>
        [HttpPut("{id:int}/status")]
        [HttpPatch("{id:int}/status")]
        [ProducesResponseType(typeof(ApiResponse<TaskResponseDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateTaskStatusDto dto)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();
                return BadRequest(ApiResponse<TaskResponseDto>.Fail("Validasi payload gagal.", errors));
            }

            var task = await _db.Tasks.FirstOrDefaultAsync(t => t.Id == id);
            if (task == null)
            {
                return NotFound(ApiResponse<TaskResponseDto>.Fail($"Tugas dengan ID {id} tidak ditemukan."));
            }

            task.Status = dto.Status;
            if (dto.Progress.HasValue)
            {
                task.Progress = Math.Clamp(dto.Progress.Value, 0, 100);
            }
            else if (dto.Status == ModelTaskStatus.Done)
            {
                task.Progress = 100;
            }

            task.UpdatedAt = DateTime.Now;
            await _db.SaveChangesAsync();

            var updatedTask = await _db.Tasks
                .Include(t => t.Project)
                .Include(t => t.Category)
                .Include(t => t.AssignedToUser)
                .Include(t => t.ParentTask)
                .Include(t => t.ChildTasks)
                .Include(t => t.Sessions)
                .AsNoTracking()
                .FirstAsync(t => t.Id == task.Id);

            return Ok(ApiResponse<TaskResponseDto>.Ok(MapToResponseDto(updatedTask), $"Status tugas berhasil diubah menjadi '{task.Status}'."));
        }

        /// <summary>
        /// Menghapus tugas berdasarkan ID (DELETE /api/tasks/{id})
        /// </summary>
        /// <param name="id">ID Tugas yang akan dihapus</param>
        [HttpDelete("{id:int}")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Delete(int id)
        {
            var task = await _db.Tasks
                .Include(t => t.ChildTasks)
                .Include(t => t.Sessions)
                .Include(t => t.Notes)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (task == null)
            {
                return NotFound(ApiResponse<object>.Fail($"Tugas dengan ID {id} tidak ditemukan."));
            }

            // Lepaskan relasi child tasks sebelum hapus jika ada
            if (task.ChildTasks.Any())
            {
                foreach (var child in task.ChildTasks)
                {
                    child.ParentTaskId = null;
                }
            }

            _db.Tasks.Remove(task);
            await _db.SaveChangesAsync();

            return Ok(ApiResponse<object>.Ok(new { id = id }, $"Tugas dengan ID {id} ('{task.Title}') berhasil dihapus."));
        }

        /// <summary>
        /// Menghapus banyak tugas sekaligus berdasarkan daftar ID (DELETE /api/tasks/bulk)
        /// </summary>
        /// <param name="dto">Payload daftar ID tugas yang akan dihapus</param>
        [HttpDelete("bulk")]
        [ProducesResponseType(typeof(ApiResponse<BulkDeleteTasksResponseDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> BulkDelete([FromBody] BulkDeleteTasksRequestDto dto)
        {
            if (dto.TaskIds == null || dto.TaskIds.Count == 0)
            {
                return BadRequest(ApiResponse<object>.Fail("Daftar ID tugas tidak boleh kosong."));
            }

            var tasks = await _db.Tasks
                .Include(t => t.ChildTasks)
                .Include(t => t.Sessions)
                .Where(t => dto.TaskIds.Contains(t.Id))
                .ToListAsync();

            foreach (var t in tasks)
            {
                foreach (var child in t.ChildTasks)
                {
                    child.ParentTaskId = null;
                }
                _db.Tasks.Remove(t);
            }

            await _db.SaveChangesAsync();

            var result = new BulkDeleteTasksResponseDto
            {
                DeletedCount = tasks.Count,
                DeletedIds = tasks.Select(t => t.Id).ToList(),
                Message = $"Berhasil menghapus {tasks.Count} tugas secara permanen."
            };

            return Ok(ApiResponse<BulkDeleteTasksResponseDto>.Ok(result, result.Message));
        }

        /// <summary>
        /// Menghapus seluruh data tugas (DELETE /api/tasks/all)
        /// </summary>
        [HttpDelete("all")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        public async Task<IActionResult> ClearAll()
        {
            var tasks = await _db.Tasks.Include(t => t.Sessions).ToListAsync();
            var count = tasks.Count;

            _db.Tasks.RemoveRange(tasks);
            await _db.SaveChangesAsync();

            return Ok(ApiResponse<object>.Ok(new { deletedCount = count }, $"Seluruh {count} tugas berhasil dibersihkan dari database."));
        }

        /// <summary>
        /// Mengambil data tugas terkelompok untuk Kanban Board (GET /api/tasks/kanban)
        /// </summary>
        /// <param name="projectId">Filter ID Proyek</param>
        /// <param name="assigneeId">Filter ID PIC</param>
        /// <param name="priority">Filter Prioritas</param>
        [HttpGet("kanban")]
        [ProducesResponseType(typeof(ApiResponse<KanbanBoardResponseDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetKanbanBoard(
            [FromQuery] int? projectId,
            [FromQuery] string? assigneeId,
            [FromQuery] TaskPriority? priority)
        {
            var query = _db.Tasks
                .Include(t => t.Project)
                .Include(t => t.Category)
                .Include(t => t.AssignedToUser)
                .Include(t => t.Sessions)
                .AsNoTracking()
                .AsQueryable();

            if (projectId.HasValue)
                query = query.Where(t => t.ProjectId == projectId.Value);

            if (!string.IsNullOrWhiteSpace(assigneeId))
                query = query.Where(t => t.AssignedToUserId == assigneeId);

            if (priority.HasValue)
                query = query.Where(t => t.Priority == priority.Value);

            var tasks = await query.ToListAsync();

            var statuses = new[]
            {
                new { Key = "Todo", Title = "To Do", Color = "#64748B", Status = ModelTaskStatus.Todo },
                new { Key = "InProgress", Title = "In Progress", Color = "#6366F1", Status = ModelTaskStatus.InProgress },
                new { Key = "Done", Title = "Done", Color = "#10B981", Status = ModelTaskStatus.Done },
                new { Key = "Overdue", Title = "Overdue", Color = "#EF4444", Status = ModelTaskStatus.Overdue }
            };

            var columns = statuses.Select(s =>
            {
                var colTasks = tasks.Where(t => t.Status == s.Status).Select(MapToResponseDto).ToList();
                return new KanbanColumnDto
                {
                    Status = s.Key,
                    Title = s.Title,
                    BadgeColor = s.Color,
                    Count = colTasks.Count,
                    Tasks = colTasks
                };
            }).ToList();

            var board = new KanbanBoardResponseDto
            {
                TotalTasks = tasks.Count,
                Columns = columns
            };

            return Ok(ApiResponse<KanbanBoardResponseDto>.Ok(board, "Data Kanban Board berhasil diambil."));
        }

        /// <summary>
        /// Menambahkan sesi pencatatan durasi kerja ke suatu tugas (POST /api/tasks/{id}/sessions)
        /// </summary>
        /// <param name="id">ID Tugas</param>
        /// <param name="dto">Payload data sesi kerja</param>
        [HttpPost("{id:int}/sessions")]
        [ProducesResponseType(typeof(ApiResponse<TaskSessionResponseDto>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> AddSession(int id, [FromBody] AddTaskSessionRequestDto dto)
        {
            var task = await _db.Tasks.FirstOrDefaultAsync(t => t.Id == id);
            if (task == null)
            {
                return NotFound(ApiResponse<object>.Fail($"Tugas dengan ID {id} tidak ditemukan."));
            }

            var dur = dto.DurationSeconds > 0
                ? dto.DurationSeconds
                : (dto.EndTime.HasValue ? (long)(dto.EndTime.Value - dto.StartTime).TotalSeconds : 0);

            var session = new WorkSession
            {
                TaskId = id,
                StartTime = dto.StartTime,
                EndTime = dto.EndTime,
                Duration = dur,
                Notes = dto.Notes?.Trim()
            };

            _db.Sessions.Add(session);
            await _db.SaveChangesAsync();

            var h = dur / 3600;
            var m = (dur % 3600) / 60;
            var s = dur % 60;

            var res = new TaskSessionResponseDto
            {
                Id = session.Id,
                TaskId = session.TaskId,
                StartTime = session.StartTime,
                EndTime = session.EndTime,
                DurationSeconds = dur,
                DurationFormatted = $"{h:D2}:{m:D2}:{s:D2}",
                Notes = session.Notes,
                IsRunning = session.EndTime == null
            };

            return CreatedAtAction(nameof(GetById), new { id = id }, ApiResponse<TaskSessionResponseDto>.Ok(res, "Sesi kerja berhasil ditambahkan."));
        }

        /// <summary>
        /// Menghapus sesi pencatatan kerja dari tugas (DELETE /api/tasks/{id}/sessions/{sessionId})
        /// </summary>
        /// <param name="id">ID Tugas</param>
        /// <param name="sessionId">ID Sesi Kerja</param>
        [HttpDelete("{id:int}/sessions/{sessionId:int}")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteSession(int id, int sessionId)
        {
            var session = await _db.Sessions.FirstOrDefaultAsync(s => s.Id == sessionId && s.TaskId == id);
            if (session == null)
            {
                return NotFound(ApiResponse<object>.Fail($"Sesi kerja dengan ID {sessionId} pada tugas {id} tidak ditemukan."));
            }

            _db.Sessions.Remove(session);
            await _db.SaveChangesAsync();

            return Ok(ApiResponse<object>.Ok(new { sessionId = sessionId, taskId = id }, "Sesi kerja berhasil dihapus."));
        }

        #region Helper Mapping
        private static TaskResponseDto MapToResponseDto(WorkTask task)
        {
            var tagsList = string.IsNullOrWhiteSpace(task.Tags)
                ? new List<string>()
                : task.Tags.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();

            var childCount = task.ChildTasks?.Count ?? 0;
            var childDoneCount = task.ChildTasks?.Count(c => c.Status == ModelTaskStatus.Done) ?? 0;

            return new TaskResponseDto
            {
                Id = task.Id,
                TaskCode = task.TaskCode,
                Title = task.Title,
                Description = task.Description,
                Obstacle = task.Obstacle,
                Solution = task.Solution,
                Priority = task.Priority.ToString(),
                Status = task.Status.ToString(),
                Progress = task.Progress,
                StartDate = task.StartDate,
                DueDate = task.DueDate,
                Milestone = task.Milestone ?? "Implementation",
                Tags = task.Tags,
                TagsList = tagsList,
                ProjectId = task.ProjectId,
                Project = task.Project != null ? new ProjectShortDto
                {
                    Id = task.Project.Id,
                    Name = task.Project.Name,
                    Color = task.Project.Color
                } : null,
                CategoryId = task.CategoryId,
                Category = task.Category != null ? new CategoryShortDto
                {
                    Id = task.Category.Id,
                    Name = task.Category.Name,
                    Color = task.Category.Color
                } : null,
                AssignedToUserId = task.AssignedToUserId,
                AssignedToUser = task.AssignedToUser != null ? new UserShortDto
                {
                    Id = task.AssignedToUser.Id,
                    FullName = task.AssignedToUser.FullName,
                    Email = task.AssignedToUser.Email ?? "",
                    JobTitle = task.AssignedToUser.JobTitle,
                    AvatarColor = task.AssignedToUser.AvatarColor
                } : null,
                ParentTaskId = task.ParentTaskId,
                ParentCode = task.ParentCode,
                IsParent = task.IsParent,
                HasParent = task.HasParent,
                ChildTasksCount = childCount,
                CompletedChildTasksCount = childDoneCount,
                TotalDurationSeconds = task.TotalDurationSeconds,
                TotalDurationFormatted = task.TotalDurationFormatted,
                CreatedAt = task.CreatedAt,
                UpdatedAt = task.UpdatedAt
            };
        }
        #endregion
    }
}
