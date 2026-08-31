using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TrackerKerja.Data;
using TrackerKerja.Models;
using TrackerKerja.ViewModels;

namespace TrackerKerja.Controllers.Api
{
    /// <summary>
    /// Modul API Master Data Sistem (Kategori, Tingkat Prioritas, Alur Status, dan Milestone SDLC Waterfall)
    /// </summary>
    [ApiController]
    [Route("api/master-data")]
    [Produces("application/json")]
    public class MasterDataApiController : ControllerBase
    {
        private readonly AppDbContext _db;

        public MasterDataApiController(AppDbContext db)
        {
            _db = db;
        }

        /// <summary>
        /// Mengambil seluruh dataset master data dalam satu panggilan (GET /api/master-data/all)
        /// </summary>
        [HttpGet("all")]
        [ProducesResponseType(typeof(ApiResponse<MasterDataSummaryDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAll()
        {
            var categories = await _db.Categories
                .Include(c => c.Tasks)
                .AsNoTracking()
                .OrderBy(c => c.Name)
                .Select(c => new MasterCategoryResponseDto
                {
                    Id = c.Id,
                    Name = c.Name,
                    Color = c.Color,
                    Description = c.Description,
                    TasksCount = c.Tasks.Count
                })
                .ToListAsync();

            var priorities = await _db.MasterPriorities
                .AsNoTracking()
                .OrderBy(p => p.OrderIndex)
                .Select(p => new MasterPriorityResponseDto
                {
                    Id = p.Id,
                    Name = p.Name,
                    Color = p.Color,
                    Icon = p.Icon,
                    OrderIndex = p.OrderIndex,
                    Description = p.Description,
                    IsDefault = p.IsDefault,
                    TasksCount = _db.Tasks.Count(t => t.Priority.ToString() == p.Name)
                })
                .ToListAsync();

            var statuses = await _db.MasterStatuses
                .AsNoTracking()
                .OrderBy(s => s.OrderIndex)
                .Select(s => new MasterStatusResponseDto
                {
                    Id = s.Id,
                    Name = s.Name,
                    Color = s.Color,
                    IsDoneState = s.IsDoneState,
                    OrderIndex = s.OrderIndex,
                    Description = s.Description,
                    IsDefault = s.IsDefault,
                    TasksCount = _db.Tasks.Count(t => t.Status.ToString() == s.Name)
                })
                .ToListAsync();

            var milestones = await _db.MasterMilestones
                .AsNoTracking()
                .OrderBy(m => m.OrderIndex)
                .Select(m => new MasterMilestoneResponseDto
                {
                    Id = m.Id,
                    Name = m.Name,
                    Phase = m.Phase,
                    Color = m.Color,
                    Icon = m.Icon,
                    OrderIndex = m.OrderIndex,
                    Description = m.Description,
                    IsDefault = m.IsDefault,
                    TasksCount = _db.Tasks.Count(t => t.Milestone == m.Name)
                })
                .ToListAsync();

            var summary = new MasterDataSummaryDto
            {
                Categories = categories,
                Priorities = priorities,
                Statuses = statuses,
                Milestones = milestones
            };

            return Ok(ApiResponse<MasterDataSummaryDto>.Ok(summary, "Seluruh master data berhasil diambil."));
        }

        #region Kategori (Categories)
        /// <summary>
        /// Mengambil daftar seluruh kategori tugas (GET /api/master-data/categories)
        /// </summary>
        [HttpGet("categories")]
        [ProducesResponseType(typeof(ApiResponse<List<MasterCategoryResponseDto>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetCategories()
        {
            var categories = await _db.Categories
                .Include(c => c.Tasks)
                .AsNoTracking()
                .OrderBy(c => c.Name)
                .Select(c => new MasterCategoryResponseDto
                {
                    Id = c.Id,
                    Name = c.Name,
                    Color = c.Color,
                    Description = c.Description,
                    TasksCount = c.Tasks.Count
                })
                .ToListAsync();

            return Ok(ApiResponse<List<MasterCategoryResponseDto>>.Ok(categories, "Daftar kategori berhasil diambil."));
        }

        /// <summary>
        /// Membuat kategori tugas baru (POST /api/master-data/categories)
        /// </summary>
        [HttpPost("categories")]
        [ProducesResponseType(typeof(ApiResponse<MasterCategoryResponseDto>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CreateCategory([FromBody] CreateMasterCategoryRequestDto dto)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                return BadRequest(ApiResponse<object>.Fail("Validasi gagal.", errors));
            }

            if (await _db.Categories.AnyAsync(c => c.Name.ToLower() == dto.Name.Trim().ToLower()))
            {
                return BadRequest(ApiResponse<object>.Fail($"Kategori dengan nama '{dto.Name}' sudah ada."));
            }

            var category = new Category
            {
                Name = dto.Name.Trim(),
                Color = string.IsNullOrWhiteSpace(dto.Color) ? "#6366F1" : dto.Color.Trim(),
                Description = dto.Description?.Trim()
            };

            _db.Categories.Add(category);
            await _db.SaveChangesAsync();

            var res = new MasterCategoryResponseDto
            {
                Id = category.Id,
                Name = category.Name,
                Color = category.Color,
                Description = category.Description,
                TasksCount = 0
            };

            return CreatedAtAction(nameof(GetCategories), new { id = category.Id }, ApiResponse<MasterCategoryResponseDto>.Ok(res, "Kategori berhasil ditambahkan."));
        }

        /// <summary>
        /// Memperbarui kategori tugas (PUT /api/master-data/categories/{id})
        /// </summary>
        [HttpPut("categories/{id:int}")]
        [ProducesResponseType(typeof(ApiResponse<MasterCategoryResponseDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateCategory(int id, [FromBody] UpdateMasterCategoryRequestDto dto)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                return BadRequest(ApiResponse<object>.Fail("Validasi gagal.", errors));
            }

            var category = await _db.Categories.Include(c => c.Tasks).FirstOrDefaultAsync(c => c.Id == id);
            if (category == null)
            {
                return NotFound(ApiResponse<object>.Fail($"Kategori dengan ID {id} tidak ditemukan."));
            }

            if (await _db.Categories.AnyAsync(c => c.Id != id && c.Name.ToLower() == dto.Name.Trim().ToLower()))
            {
                return BadRequest(ApiResponse<object>.Fail($"Kategori lain dengan nama '{dto.Name}' sudah ada."));
            }

            category.Name = dto.Name.Trim();
            if (!string.IsNullOrWhiteSpace(dto.Color)) category.Color = dto.Color.Trim();
            category.Description = dto.Description?.Trim();

            await _db.SaveChangesAsync();

            var res = new MasterCategoryResponseDto
            {
                Id = category.Id,
                Name = category.Name,
                Color = category.Color,
                Description = category.Description,
                TasksCount = category.Tasks.Count
            };

            return Ok(ApiResponse<MasterCategoryResponseDto>.Ok(res, "Kategori berhasil diperbarui."));
        }

        /// <summary>
        /// Menghapus kategori tugas (DELETE /api/master-data/categories/{id})
        /// </summary>
        [HttpDelete("categories/{id:int}")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteCategory(int id)
        {
            var category = await _db.Categories.Include(c => c.Tasks).FirstOrDefaultAsync(c => c.Id == id);
            if (category == null)
            {
                return NotFound(ApiResponse<object>.Fail($"Kategori dengan ID {id} tidak ditemukan."));
            }

            foreach (var t in category.Tasks)
            {
                t.CategoryId = null;
            }

            _db.Categories.Remove(category);
            await _db.SaveChangesAsync();

            return Ok(ApiResponse<object>.Ok(new { id = id }, $"Kategori '{category.Name}' berhasil dihapus."));
        }
        #endregion

        #region Prioritas (Priorities)
        /// <summary>
        /// Mengambil daftar tingkat prioritas tugas (GET /api/master-data/priorities)
        /// </summary>
        [HttpGet("priorities")]
        [ProducesResponseType(typeof(ApiResponse<List<MasterPriorityResponseDto>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetPriorities()
        {
            var priorities = await _db.MasterPriorities
                .AsNoTracking()
                .OrderBy(p => p.OrderIndex)
                .Select(p => new MasterPriorityResponseDto
                {
                    Id = p.Id,
                    Name = p.Name,
                    Color = p.Color,
                    Icon = p.Icon,
                    OrderIndex = p.OrderIndex,
                    Description = p.Description,
                    IsDefault = p.IsDefault,
                    TasksCount = _db.Tasks.Count(t => t.Priority.ToString() == p.Name)
                })
                .ToListAsync();

            return Ok(ApiResponse<List<MasterPriorityResponseDto>>.Ok(priorities, "Daftar prioritas berhasil diambil."));
        }

        /// <summary>
        /// Menambahkan tingkat prioritas baru (POST /api/master-data/priorities)
        /// </summary>
        [HttpPost("priorities")]
        [ProducesResponseType(typeof(ApiResponse<MasterPriorityResponseDto>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CreatePriority([FromBody] CreateMasterPriorityRequestDto dto)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                return BadRequest(ApiResponse<object>.Fail("Validasi gagal.", errors));
            }

            var item = new MasterPriority
            {
                Name = dto.Name.Trim(),
                Color = string.IsNullOrWhiteSpace(dto.Color) ? "#F59E0B" : dto.Color.Trim(),
                Icon = string.IsNullOrWhiteSpace(dto.Icon) ? "fa-flag" : dto.Icon.Trim(),
                OrderIndex = dto.OrderIndex,
                Description = dto.Description?.Trim(),
                IsDefault = dto.IsDefault
            };

            _db.MasterPriorities.Add(item);
            await _db.SaveChangesAsync();

            var res = new MasterPriorityResponseDto
            {
                Id = item.Id,
                Name = item.Name,
                Color = item.Color,
                Icon = item.Icon,
                OrderIndex = item.OrderIndex,
                Description = item.Description,
                IsDefault = item.IsDefault,
                TasksCount = 0
            };

            return CreatedAtAction(nameof(GetPriorities), new { id = item.Id }, ApiResponse<MasterPriorityResponseDto>.Ok(res, "Prioritas berhasil ditambahkan."));
        }

        /// <summary>
        /// Memperbarui data prioritas tugas (PUT /api/master-data/priorities/{id})
        /// </summary>
        [HttpPut("priorities/{id:int}")]
        [ProducesResponseType(typeof(ApiResponse<MasterPriorityResponseDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdatePriority(int id, [FromBody] UpdateMasterPriorityRequestDto dto)
        {
            var item = await _db.MasterPriorities.FirstOrDefaultAsync(p => p.Id == id);
            if (item == null)
            {
                return NotFound(ApiResponse<object>.Fail($"Prioritas dengan ID {id} tidak ditemukan."));
            }

            item.Name = dto.Name.Trim();
            if (!string.IsNullOrWhiteSpace(dto.Color)) item.Color = dto.Color.Trim();
            if (!string.IsNullOrWhiteSpace(dto.Icon)) item.Icon = dto.Icon.Trim();
            item.OrderIndex = dto.OrderIndex;
            item.Description = dto.Description?.Trim();
            item.IsDefault = dto.IsDefault;

            await _db.SaveChangesAsync();

            var res = new MasterPriorityResponseDto
            {
                Id = item.Id,
                Name = item.Name,
                Color = item.Color,
                Icon = item.Icon,
                OrderIndex = item.OrderIndex,
                Description = item.Description,
                IsDefault = item.IsDefault,
                TasksCount = await _db.Tasks.CountAsync(t => t.Priority.ToString() == item.Name)
            };

            return Ok(ApiResponse<MasterPriorityResponseDto>.Ok(res, "Prioritas berhasil diperbarui."));
        }

        /// <summary>
        /// Menghapus data prioritas (DELETE /api/master-data/priorities/{id})
        /// </summary>
        [HttpDelete("priorities/{id:int}")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeletePriority(int id)
        {
            var item = await _db.MasterPriorities.FirstOrDefaultAsync(p => p.Id == id);
            if (item == null)
            {
                return NotFound(ApiResponse<object>.Fail($"Prioritas dengan ID {id} tidak ditemukan."));
            }

            _db.MasterPriorities.Remove(item);
            await _db.SaveChangesAsync();

            return Ok(ApiResponse<object>.Ok(new { id = id }, $"Prioritas '{item.Name}' berhasil dihapus."));
        }
        #endregion

        #region Status (Statuses)
        /// <summary>
        /// Mengambil daftar alur status tugas (GET /api/master-data/statuses)
        /// </summary>
        [HttpGet("statuses")]
        [ProducesResponseType(typeof(ApiResponse<List<MasterStatusResponseDto>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetStatuses()
        {
            var statuses = await _db.MasterStatuses
                .AsNoTracking()
                .OrderBy(s => s.OrderIndex)
                .Select(s => new MasterStatusResponseDto
                {
                    Id = s.Id,
                    Name = s.Name,
                    Color = s.Color,
                    IsDoneState = s.IsDoneState,
                    OrderIndex = s.OrderIndex,
                    Description = s.Description,
                    IsDefault = s.IsDefault,
                    TasksCount = _db.Tasks.Count(t => t.Status.ToString() == s.Name)
                })
                .ToListAsync();

            return Ok(ApiResponse<List<MasterStatusResponseDto>>.Ok(statuses, "Daftar status berhasil diambil."));
        }

        /// <summary>
        /// Menambahkan status tugas baru (POST /api/master-data/statuses)
        /// </summary>
        [HttpPost("statuses")]
        [ProducesResponseType(typeof(ApiResponse<MasterStatusResponseDto>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CreateStatus([FromBody] CreateMasterStatusRequestDto dto)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                return BadRequest(ApiResponse<object>.Fail("Validasi gagal.", errors));
            }

            var item = new MasterStatus
            {
                Name = dto.Name.Trim(),
                Color = string.IsNullOrWhiteSpace(dto.Color) ? "#6366F1" : dto.Color.Trim(),
                IsDoneState = dto.IsDoneState,
                OrderIndex = dto.OrderIndex,
                Description = dto.Description?.Trim(),
                IsDefault = dto.IsDefault
            };

            _db.MasterStatuses.Add(item);
            await _db.SaveChangesAsync();

            var res = new MasterStatusResponseDto
            {
                Id = item.Id,
                Name = item.Name,
                Color = item.Color,
                IsDoneState = item.IsDoneState,
                OrderIndex = item.OrderIndex,
                Description = item.Description,
                IsDefault = item.IsDefault,
                TasksCount = 0
            };

            return CreatedAtAction(nameof(GetStatuses), new { id = item.Id }, ApiResponse<MasterStatusResponseDto>.Ok(res, "Status berhasil ditambahkan."));
        }

        /// <summary>
        /// Memperbarui data status tugas (PUT /api/master-data/statuses/{id})
        /// </summary>
        [HttpPut("statuses/{id:int}")]
        [ProducesResponseType(typeof(ApiResponse<MasterStatusResponseDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateMasterStatusRequestDto dto)
        {
            var item = await _db.MasterStatuses.FirstOrDefaultAsync(s => s.Id == id);
            if (item == null)
            {
                return NotFound(ApiResponse<object>.Fail($"Status dengan ID {id} tidak ditemukan."));
            }

            item.Name = dto.Name.Trim();
            if (!string.IsNullOrWhiteSpace(dto.Color)) item.Color = dto.Color.Trim();
            item.IsDoneState = dto.IsDoneState;
            item.OrderIndex = dto.OrderIndex;
            item.Description = dto.Description?.Trim();
            item.IsDefault = dto.IsDefault;

            await _db.SaveChangesAsync();

            var res = new MasterStatusResponseDto
            {
                Id = item.Id,
                Name = item.Name,
                Color = item.Color,
                IsDoneState = item.IsDoneState,
                OrderIndex = item.OrderIndex,
                Description = item.Description,
                IsDefault = item.IsDefault,
                TasksCount = await _db.Tasks.CountAsync(t => t.Status.ToString() == item.Name)
            };

            return Ok(ApiResponse<MasterStatusResponseDto>.Ok(res, "Status berhasil diperbarui."));
        }

        /// <summary>
        /// Menghapus data status (DELETE /api/master-data/statuses/{id})
        /// </summary>
        [HttpDelete("statuses/{id:int}")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteStatus(int id)
        {
            var item = await _db.MasterStatuses.FirstOrDefaultAsync(s => s.Id == id);
            if (item == null)
            {
                return NotFound(ApiResponse<object>.Fail($"Status dengan ID {id} tidak ditemukan."));
            }

            _db.MasterStatuses.Remove(item);
            await _db.SaveChangesAsync();

            return Ok(ApiResponse<object>.Ok(new { id = id }, $"Status '{item.Name}' berhasil dihapus."));
        }
        #endregion

        #region Milestone SDLC Waterfall
        /// <summary>
        /// Mengambil daftar Milestone SDLC Waterfall (GET /api/master-data/milestones)
        /// </summary>
        [HttpGet("milestones")]
        [ProducesResponseType(typeof(ApiResponse<List<MasterMilestoneResponseDto>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetMilestones()
        {
            var milestones = await _db.MasterMilestones
                .AsNoTracking()
                .OrderBy(m => m.OrderIndex)
                .Select(m => new MasterMilestoneResponseDto
                {
                    Id = m.Id,
                    Name = m.Name,
                    Phase = m.Phase,
                    Color = m.Color,
                    Icon = m.Icon,
                    OrderIndex = m.OrderIndex,
                    Description = m.Description,
                    IsDefault = m.IsDefault,
                    TasksCount = _db.Tasks.Count(t => t.Milestone == m.Name)
                })
                .ToListAsync();

            return Ok(ApiResponse<List<MasterMilestoneResponseDto>>.Ok(milestones, "Daftar Milestone SDLC Waterfall berhasil diambil."));
        }

        /// <summary>
        /// Menambahkan Milestone SDLC Waterfall baru (POST /api/master-data/milestones)
        /// </summary>
        [HttpPost("milestones")]
        [ProducesResponseType(typeof(ApiResponse<MasterMilestoneResponseDto>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CreateMilestone([FromBody] CreateMasterMilestoneRequestDto dto)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                return BadRequest(ApiResponse<object>.Fail("Validasi gagal.", errors));
            }

            var item = new MasterMilestone
            {
                Name = dto.Name.Trim(),
                Phase = string.IsNullOrWhiteSpace(dto.Phase) ? dto.Name.Trim() : dto.Phase.Trim(),
                Color = string.IsNullOrWhiteSpace(dto.Color) ? "#6366F1" : dto.Color.Trim(),
                Icon = string.IsNullOrWhiteSpace(dto.Icon) ? "fa-code" : dto.Icon.Trim(),
                OrderIndex = dto.OrderIndex,
                Description = dto.Description?.Trim(),
                IsDefault = dto.IsDefault
            };

            _db.MasterMilestones.Add(item);
            await _db.SaveChangesAsync();

            var res = new MasterMilestoneResponseDto
            {
                Id = item.Id,
                Name = item.Name,
                Phase = item.Phase,
                Color = item.Color,
                Icon = item.Icon,
                OrderIndex = item.OrderIndex,
                Description = item.Description,
                IsDefault = item.IsDefault,
                TasksCount = 0
            };

            return CreatedAtAction(nameof(GetMilestones), new { id = item.Id }, ApiResponse<MasterMilestoneResponseDto>.Ok(res, "Milestone SDLC berhasil ditambahkan."));
        }

        /// <summary>
        /// Memperbarui Milestone SDLC Waterfall (PUT /api/master-data/milestones/{id})
        /// </summary>
        [HttpPut("milestones/{id:int}")]
        [ProducesResponseType(typeof(ApiResponse<MasterMilestoneResponseDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateMilestone(int id, [FromBody] UpdateMasterMilestoneRequestDto dto)
        {
            var item = await _db.MasterMilestones.FirstOrDefaultAsync(m => m.Id == id);
            if (item == null)
            {
                return NotFound(ApiResponse<object>.Fail($"Milestone dengan ID {id} tidak ditemukan."));
            }

            item.Name = dto.Name.Trim();
            if (!string.IsNullOrWhiteSpace(dto.Phase)) item.Phase = dto.Phase.Trim();
            if (!string.IsNullOrWhiteSpace(dto.Color)) item.Color = dto.Color.Trim();
            if (!string.IsNullOrWhiteSpace(dto.Icon)) item.Icon = dto.Icon.Trim();
            item.OrderIndex = dto.OrderIndex;
            item.Description = dto.Description?.Trim();
            item.IsDefault = dto.IsDefault;

            await _db.SaveChangesAsync();

            var res = new MasterMilestoneResponseDto
            {
                Id = item.Id,
                Name = item.Name,
                Phase = item.Phase,
                Color = item.Color,
                Icon = item.Icon,
                OrderIndex = item.OrderIndex,
                Description = item.Description,
                IsDefault = item.IsDefault,
                TasksCount = await _db.Tasks.CountAsync(t => t.Milestone == item.Name)
            };

            return Ok(ApiResponse<MasterMilestoneResponseDto>.Ok(res, "Milestone SDLC berhasil diperbarui."));
        }

        /// <summary>
        /// Menghapus Milestone SDLC Waterfall (DELETE /api/master-data/milestones/{id})
        /// </summary>
        [HttpDelete("milestones/{id:int}")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteMilestone(int id)
        {
            var item = await _db.MasterMilestones.FirstOrDefaultAsync(m => m.Id == id);
            if (item == null)
            {
                return NotFound(ApiResponse<object>.Fail($"Milestone dengan ID {id} tidak ditemukan."));
            }

            _db.MasterMilestones.Remove(item);
            await _db.SaveChangesAsync();

            return Ok(ApiResponse<object>.Ok(new { id = id }, $"Milestone '{item.Name}' berhasil dihapus."));
        }
        #endregion
    }
}
