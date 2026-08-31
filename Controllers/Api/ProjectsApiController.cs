using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TrackerKerja.Data;
using TrackerKerja.Models;
using TrackerKerja.ViewModels;

namespace TrackerKerja.Controllers.Api
{
    [ApiController]
    [Route("api/projects")]
    [Produces("application/json")]
    public class ProjectsApiController : ControllerBase
    {
        private readonly AppDbContext _db;

        public ProjectsApiController(AppDbContext db)
        {
            _db = db;
        }

        /// <summary>
        /// Mengambil daftar seluruh proyek dengan metrik tugas dan durasi (GET /api/projects)
        /// </summary>
        /// <param name="status">Filter status proyek (Active, Completed, Archived)</param>
        /// <param name="search">Pencarian nama atau deskripsi proyek</param>
        [HttpGet]
        [ProducesResponseType(typeof(ApiResponse<List<ProjectResponseDto>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAll([FromQuery] ProjectStatus? status, [FromQuery] string? search)
        {
            var query = _db.Projects
                .Include(p => p.Tasks)
                    .ThenInclude(t => t.Sessions)
                .AsNoTracking()
                .AsQueryable();

            if (status.HasValue)
                query = query.Where(p => p.Status == status.Value);

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim().ToLower();
                query = query.Where(p => p.Name.ToLower().Contains(term) || (p.Description != null && p.Description.ToLower().Contains(term)));
            }

            var projects = await query.OrderByDescending(p => p.CreatedAt).ToListAsync();
            var dtos = projects.Select(MapToResponseDto).ToList();

            return Ok(ApiResponse<List<ProjectResponseDto>>.Ok(dtos, $"Berhasil mengambil {dtos.Count} proyek."));
        }

        /// <summary>
        /// Mengambil detail satu proyek beserta daftar tugasnya (GET /api/projects/{id})
        /// </summary>
        /// <param name="id">ID Proyek</param>
        [HttpGet("{id:int}")]
        [ProducesResponseType(typeof(ApiResponse<ProjectResponseDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetById(int id)
        {
            var project = await _db.Projects
                .Include(p => p.Tasks)
                    .ThenInclude(t => t.Sessions)
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == id);

            if (project == null)
            {
                return NotFound(ApiResponse<ProjectResponseDto>.Fail($"Proyek dengan ID {id} tidak ditemukan."));
            }

            return Ok(ApiResponse<ProjectResponseDto>.Ok(MapToResponseDto(project), "Detail proyek berhasil diambil."));
        }

        /// <summary>
        /// Membuat proyek baru (POST /api/projects)
        /// </summary>
        /// <param name="dto">Payload proyek baru</param>
        [HttpPost]
        [ProducesResponseType(typeof(ApiResponse<ProjectResponseDto>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Create([FromBody] CreateProjectRequestDto dto)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                return BadRequest(ApiResponse<ProjectResponseDto>.Fail("Validasi gagal.", errors));
            }

            var project = new Project
            {
                Name = dto.Name.Trim(),
                Description = dto.Description?.Trim(),
                Color = string.IsNullOrWhiteSpace(dto.Color) ? "#6366F1" : dto.Color.Trim(),
                Deadline = dto.Deadline,
                Status = dto.Status,
                CreatedAt = DateTime.Now
            };

            _db.Projects.Add(project);
            await _db.SaveChangesAsync();

            var responseDto = MapToResponseDto(project);
            return CreatedAtAction(nameof(GetById), new { id = project.Id }, ApiResponse<ProjectResponseDto>.Ok(responseDto, "Proyek berhasil dibuat."));
        }

        /// <summary>
        /// Memperbarui data proyek (PUT /api/projects/{id})
        /// </summary>
        /// <param name="id">ID Proyek</param>
        /// <param name="dto">Payload pembaruan proyek</param>
        [HttpPut("{id:int}")]
        [ProducesResponseType(typeof(ApiResponse<ProjectResponseDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateProjectRequestDto dto)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                return BadRequest(ApiResponse<ProjectResponseDto>.Fail("Validasi gagal.", errors));
            }

            var project = await _db.Projects.FirstOrDefaultAsync(p => p.Id == id);
            if (project == null)
            {
                return NotFound(ApiResponse<ProjectResponseDto>.Fail($"Proyek dengan ID {id} tidak ditemukan."));
            }

            project.Name = dto.Name.Trim();
            project.Description = dto.Description?.Trim();
            project.Color = string.IsNullOrWhiteSpace(dto.Color) ? "#6366F1" : dto.Color.Trim();
            project.Deadline = dto.Deadline;
            project.Status = dto.Status;

            await _db.SaveChangesAsync();

            // Reload for stats
            var updated = await _db.Projects
                .Include(p => p.Tasks)
                    .ThenInclude(t => t.Sessions)
                .AsNoTracking()
                .FirstAsync(p => p.Id == id);

            return Ok(ApiResponse<ProjectResponseDto>.Ok(MapToResponseDto(updated), "Proyek berhasil diperbarui."));
        }

        /// <summary>
        /// Mengambil seluruh tugas yang terdaftar di dalam proyek (GET /api/projects/{id}/tasks)
        /// </summary>
        /// <param name="id">ID Proyek</param>
        [HttpGet("{id:int}/tasks")]
        [ProducesResponseType(typeof(ApiResponse<List<TaskResponseDto>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetProjectTasks(int id)
        {
            var project = await _db.Projects.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id);
            if (project == null)
            {
                return NotFound(ApiResponse<object>.Fail($"Proyek dengan ID {id} tidak ditemukan."));
            }

            var tasks = await _db.Tasks
                .Include(t => t.Project)
                .Include(t => t.Category)
                .Include(t => t.AssignedToUser)
                .Include(t => t.ParentTask)
                .Include(t => t.ChildTasks)
                .Include(t => t.Sessions)
                .Where(t => t.ProjectId == id)
                .AsNoTracking()
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync();

            var dtos = tasks.Select(t => new TaskResponseDto
            {
                Id = t.Id,
                TaskCode = t.TaskCode,
                Title = t.Title,
                Description = t.Description,
                Obstacle = t.Obstacle,
                Solution = t.Solution,
                Priority = t.Priority.ToString(),
                Status = t.Status.ToString(),
                Progress = t.Progress,
                StartDate = t.StartDate,
                DueDate = t.DueDate,
                Milestone = t.Milestone ?? "Implementation",
                Tags = t.Tags,
                ProjectId = t.ProjectId,
                Project = new ProjectShortDto { Id = project.Id, Name = project.Name, Color = project.Color },
                CategoryId = t.CategoryId,
                Category = t.Category != null ? new CategoryShortDto { Id = t.Category.Id, Name = t.Category.Name, Color = t.Category.Color } : null,
                AssignedToUserId = t.AssignedToUserId,
                AssignedToUser = t.AssignedToUser != null ? new UserShortDto
                {
                    Id = t.AssignedToUser.Id,
                    FullName = t.AssignedToUser.FullName,
                    Email = t.AssignedToUser.Email ?? "",
                    JobTitle = t.AssignedToUser.JobTitle,
                    AvatarColor = t.AssignedToUser.AvatarColor
                } : null,
                ParentTaskId = t.ParentTaskId,
                ParentCode = t.ParentCode,
                IsParent = t.IsParent,
                HasParent = t.HasParent,
                TotalDurationSeconds = t.TotalDurationSeconds,
                TotalDurationFormatted = t.TotalDurationFormatted,
                CreatedAt = t.CreatedAt,
                UpdatedAt = t.UpdatedAt
            }).ToList();

            return Ok(ApiResponse<List<TaskResponseDto>>.Ok(dtos, $"Berhasil mengambil {dtos.Count} tugas dalam proyek '{project.Name}'."));
        }

        /// <summary>
        /// Mengambil ringkasan statistik dan metrik seluruh proyek (GET /api/projects/summary)
        /// </summary>
        [HttpGet("summary")]
        [ProducesResponseType(typeof(ApiResponse<List<ProjectProgressReportDto>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetSummary()
        {
            var projects = await _db.Projects
                .Include(p => p.Tasks)
                    .ThenInclude(t => t.Sessions)
                .AsNoTracking()
                .ToListAsync();

            var summary = projects.Select(p =>
            {
                var totalSecs = p.Tasks.SelectMany(t => t.Sessions).Sum(s => s.Duration);
                return new ProjectProgressReportDto
                {
                    ProjectId = p.Id,
                    ProjectName = p.Name,
                    Color = p.Color,
                    TotalTasks = p.TotalTasks,
                    CompletedTasks = p.CompletedTasks,
                    ProgressPercent = p.ProgressPercent,
                    TotalHours = Math.Round(totalSecs / 3600.0, 1)
                };
            }).ToList();

            return Ok(ApiResponse<List<ProjectProgressReportDto>>.Ok(summary, "Ringkasan statistik proyek berhasil diambil."));
        }

        /// <summary>
        /// Menghapus proyek beserta relasinya (DELETE /api/projects/{id})
        /// </summary>
        /// <param name="id">ID Proyek</param>
        [HttpDelete("{id:int}")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Delete(int id)
        {
            var project = await _db.Projects.Include(p => p.Tasks).FirstOrDefaultAsync(p => p.Id == id);
            if (project == null)
            {
                return NotFound(ApiResponse<object>.Fail($"Proyek dengan ID {id} tidak ditemukan."));
            }

            // Lepaskan projectId dari tasks
            foreach (var task in project.Tasks)
            {
                task.ProjectId = null;
            }

            _db.Projects.Remove(project);
            await _db.SaveChangesAsync();

            return Ok(ApiResponse<object>.Ok(new { id = id }, $"Proyek '{project.Name}' berhasil dihapus."));
        }

        private static ProjectResponseDto MapToResponseDto(Project p)
        {
            var totalSecs = p.Tasks.SelectMany(t => t.Sessions).Sum(s => s.Duration);
            var h = totalSecs / 3600;
            var m = (totalSecs % 3600) / 60;
            var s = totalSecs % 60;

            return new ProjectResponseDto
            {
                Id = p.Id,
                Name = p.Name,
                Description = p.Description,
                Color = p.Color,
                Deadline = p.Deadline,
                Status = p.Status.ToString(),
                TotalTasks = p.TotalTasks,
                CompletedTasks = p.CompletedTasks,
                ProgressPercent = p.ProgressPercent,
                TotalWorkSeconds = totalSecs,
                TotalWorkFormatted = $"{h:D2}:{m:D2}:{s:D2}",
                CreatedAt = p.CreatedAt
            };
        }
    }
}
