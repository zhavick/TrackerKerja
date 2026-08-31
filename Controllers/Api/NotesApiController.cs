using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TrackerKerja.Data;
using TrackerKerja.Models;
using TrackerKerja.ViewModels;

namespace TrackerKerja.Controllers.Api
{
    [ApiController]
    [Route("api/notes")]
    [Produces("application/json")]
    public class NotesApiController : ControllerBase
    {
        private readonly AppDbContext _db;

        public NotesApiController(AppDbContext db)
        {
            _db = db;
        }

        /// <summary>
        /// Mengambil daftar seluruh catatan kerja dengan filter (GET /api/notes)
        /// </summary>
        /// <param name="category">Filter kategori (Meeting, Technical, Task Note, General, dll)</param>
        /// <param name="isPinned">Filter catatan yang di-pin (true / false)</param>
        /// <param name="taskId">Filter catatan yang terhubung ke Task ID tertentu</param>
        /// <param name="search">Pencarian judul atau konten catatan</param>
        [HttpGet]
        [ProducesResponseType(typeof(ApiResponse<List<NoteResponseDto>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAll(
            [FromQuery] string? category,
            [FromQuery] bool? isPinned,
            [FromQuery] int? taskId,
            [FromQuery] string? search)
        {
            var query = _db.Notes
                .Include(n => n.AuthorUser)
                .Include(n => n.Task)
                .Include(n => n.Attachments)
                .AsNoTracking()
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(category))
                query = query.Where(n => n.Category == category);

            if (isPinned.HasValue)
                query = query.Where(n => n.IsPinned == isPinned.Value);

            if (taskId.HasValue)
                query = query.Where(n => n.TaskId == taskId.Value);

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim().ToLower();
                query = query.Where(n => n.Title.ToLower().Contains(s) || n.ContentHtml.ToLower().Contains(s));
            }

            var notes = await query
                .OrderByDescending(n => n.IsPinned)
                .ThenByDescending(n => n.UpdatedAt)
                .ToListAsync();

            var dtos = notes.Select(MapToResponseDto).ToList();
            return Ok(ApiResponse<List<NoteResponseDto>>.Ok(dtos, $"Berhasil mengambil {dtos.Count} catatan."));
        }

        /// <summary>
        /// Mengambil detail satu catatan berdasarkan ID (GET /api/notes/{id})
        /// </summary>
        /// <param name="id">ID Catatan</param>
        [HttpGet("{id:int}")]
        [ProducesResponseType(typeof(ApiResponse<NoteResponseDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetById(int id)
        {
            var note = await _db.Notes
                .Include(n => n.AuthorUser)
                .Include(n => n.Task)
                .Include(n => n.Attachments)
                .AsNoTracking()
                .FirstOrDefaultAsync(n => n.Id == id);

            if (note == null)
            {
                return NotFound(ApiResponse<NoteResponseDto>.Fail($"Catatan dengan ID {id} tidak ditemukan."));
            }

            return Ok(ApiResponse<NoteResponseDto>.Ok(MapToResponseDto(note), "Detail catatan berhasil diambil."));
        }

        /// <summary>
        /// Membuat catatan kerja baru (POST /api/notes)
        /// </summary>
        /// <param name="dto">Payload catatan baru</param>
        [HttpPost]
        [ProducesResponseType(typeof(ApiResponse<NoteResponseDto>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Create([FromBody] CreateNoteRequestDto dto)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                return BadRequest(ApiResponse<NoteResponseDto>.Fail("Validasi gagal.", errors));
            }

            if (dto.TaskId.HasValue && !await _db.Tasks.AnyAsync(t => t.Id == dto.TaskId.Value))
            {
                return BadRequest(ApiResponse<NoteResponseDto>.Fail($"Tugas dengan ID {dto.TaskId.Value} tidak ditemukan."));
            }

            if (!string.IsNullOrWhiteSpace(dto.AuthorUserId) && !await _db.Users.AnyAsync(u => u.Id == dto.AuthorUserId))
            {
                return BadRequest(ApiResponse<NoteResponseDto>.Fail($"Pengguna dengan ID '{dto.AuthorUserId}' tidak ditemukan."));
            }

            var note = new WorkNote
            {
                Title = dto.Title.Trim(),
                ContentHtml = dto.ContentHtml,
                Category = string.IsNullOrWhiteSpace(dto.Category) ? "General" : dto.Category.Trim(),
                Color = string.IsNullOrWhiteSpace(dto.Color) ? "#6366F1" : dto.Color.Trim(),
                IsPinned = dto.IsPinned,
                TaskId = dto.TaskId,
                AuthorUserId = dto.AuthorUserId,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };

            _db.Notes.Add(note);
            await _db.SaveChangesAsync();

            var created = await _db.Notes
                .Include(n => n.AuthorUser)
                .Include(n => n.Task)
                .Include(n => n.Attachments)
                .AsNoTracking()
                .FirstAsync(n => n.Id == note.Id);

            return CreatedAtAction(nameof(GetById), new { id = note.Id }, ApiResponse<NoteResponseDto>.Ok(MapToResponseDto(created), "Catatan berhasil dibuat."));
        }

        /// <summary>
        /// Memperbarui catatan kerja (PUT /api/notes/{id})
        /// </summary>
        /// <param name="id">ID Catatan</param>
        /// <param name="dto">Payload pembaruan catatan</param>
        [HttpPut("{id:int}")]
        [ProducesResponseType(typeof(ApiResponse<NoteResponseDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateNoteRequestDto dto)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                return BadRequest(ApiResponse<NoteResponseDto>.Fail("Validasi gagal.", errors));
            }

            var note = await _db.Notes.FirstOrDefaultAsync(n => n.Id == id);
            if (note == null)
            {
                return NotFound(ApiResponse<NoteResponseDto>.Fail($"Catatan dengan ID {id} tidak ditemukan."));
            }

            if (dto.TaskId.HasValue && !await _db.Tasks.AnyAsync(t => t.Id == dto.TaskId.Value))
            {
                return BadRequest(ApiResponse<NoteResponseDto>.Fail($"Tugas dengan ID {dto.TaskId.Value} tidak ditemukan."));
            }

            note.Title = dto.Title.Trim();
            note.ContentHtml = dto.ContentHtml;
            note.Category = string.IsNullOrWhiteSpace(dto.Category) ? "General" : dto.Category.Trim();
            note.Color = string.IsNullOrWhiteSpace(dto.Color) ? "#6366F1" : dto.Color.Trim();
            note.IsPinned = dto.IsPinned;
            note.TaskId = dto.TaskId;
            note.UpdatedAt = DateTime.Now;

            await _db.SaveChangesAsync();

            var updated = await _db.Notes
                .Include(n => n.AuthorUser)
                .Include(n => n.Task)
                .Include(n => n.Attachments)
                .AsNoTracking()
                .FirstAsync(n => n.Id == id);

            return Ok(ApiResponse<NoteResponseDto>.Ok(MapToResponseDto(updated), "Catatan berhasil diperbarui."));
        }

        /// <summary>
        /// Mengubah status Pin catatan (PUT / POST /api/notes/{id}/pin)
        /// </summary>
        /// <param name="id">ID Catatan</param>
        [HttpPut("{id:int}/pin")]
        [HttpPost("{id:int}/pin")]
        [ProducesResponseType(typeof(ApiResponse<NoteResponseDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> TogglePin(int id)
        {
            var note = await _db.Notes.FirstOrDefaultAsync(n => n.Id == id);
            if (note == null)
            {
                return NotFound(ApiResponse<NoteResponseDto>.Fail($"Catatan dengan ID {id} tidak ditemukan."));
            }

            note.IsPinned = !note.IsPinned;
            note.UpdatedAt = DateTime.Now;
            await _db.SaveChangesAsync();

            var updated = await _db.Notes
                .Include(n => n.AuthorUser)
                .Include(n => n.Task)
                .Include(n => n.Attachments)
                .AsNoTracking()
                .FirstAsync(n => n.Id == id);

            return Ok(ApiResponse<NoteResponseDto>.Ok(MapToResponseDto(updated), $"Catatan berhasil {(note.IsPinned ? "disematkan (pinned)" : "dilepas sematan")}."));
        }

        /// <summary>
        /// Mengambil daftar seluruh kategori unik catatan (GET /api/notes/categories)
        /// </summary>
        [HttpGet("categories")]
        [ProducesResponseType(typeof(ApiResponse<List<string>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetCategories()
        {
            var cats = await _db.Notes
                .AsNoTracking()
                .Select(n => n.Category)
                .Distinct()
                .OrderBy(c => c)
                .ToListAsync();

            if (!cats.Contains("General")) cats.Insert(0, "General");
            if (!cats.Contains("Meeting")) cats.Add("Meeting");
            if (!cats.Contains("Technical")) cats.Add("Technical");
            if (!cats.Contains("Task Note")) cats.Add("Task Note");

            return Ok(ApiResponse<List<string>>.Ok(cats.Distinct().ToList(), "Daftar kategori catatan berhasil diambil."));
        }

        /// <summary>
        /// Mengunggah file lampiran ke suatu catatan kerja (POST /api/notes/{id}/attachments)
        /// </summary>
        /// <param name="id">ID Catatan</param>
        /// <param name="upload">Payload file yang akan dilampirkan (maks 10MB)</param>
        [HttpPost("{id:int}/attachments")]
        [Consumes("multipart/form-data")]
        [ProducesResponseType(typeof(ApiResponse<NoteAttachmentDto>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UploadAttachment(int id, [FromForm] FileUploadDto upload)
        {
            var file = upload?.File;
            if (file == null || file.Length == 0)
            {
                return BadRequest(ApiResponse<object>.Fail("Silakan pilih file yang valid untuk diunggah."));
            }

            var note = await _db.Notes.FirstOrDefaultAsync(n => n.Id == id);
            if (note == null)
            {
                return NotFound(ApiResponse<object>.Fail($"Catatan dengan ID {id} tidak ditemukan."));
            }

            var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "notes");
            if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

            var ext = Path.GetExtension(file.FileName);
            var uniqueFileName = $"{Guid.NewGuid():N}_{Path.GetFileName(file.FileName)}";
            var fullPath = Path.Combine(uploadsFolder, uniqueFileName);

            using (var stream = new FileStream(fullPath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            var attachment = new NoteAttachment
            {
                NoteId = id,
                FileName = file.FileName,
                FilePath = $"/uploads/notes/{uniqueFileName}",
                FileSize = file.Length,
                ContentType = file.ContentType,
                FileExtension = ext,
                UploadedAt = DateTime.Now
            };

            _db.NoteAttachments.Add(attachment);
            await _db.SaveChangesAsync();

            var dto = new NoteAttachmentDto
            {
                Id = attachment.Id,
                FileName = attachment.FileName,
                FilePath = attachment.FilePath,
                FileSize = attachment.FileSize,
                ContentType = attachment.ContentType,
                FileExtension = attachment.FileExtension,
                UploadedAt = attachment.UploadedAt
            };

            return CreatedAtAction(nameof(GetById), new { id = id }, ApiResponse<NoteAttachmentDto>.Ok(dto, "File lampiran berhasil diunggah."));
        }

        /// <summary>
        /// Menghapus file lampiran dari catatan (DELETE /api/notes/{id}/attachments/{attachmentId})
        /// </summary>
        /// <param name="id">ID Catatan</param>
        /// <param name="attachmentId">ID Lampiran</param>
        [HttpDelete("{id:int}/attachments/{attachmentId:int}")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteAttachment(int id, int attachmentId)
        {
            var attachment = await _db.NoteAttachments.FirstOrDefaultAsync(a => a.Id == attachmentId && a.NoteId == id);
            if (attachment == null)
            {
                return NotFound(ApiResponse<object>.Fail($"Lampiran dengan ID {attachmentId} pada catatan {id} tidak ditemukan."));
            }

            try
            {
                var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", attachment.FilePath.TrimStart('/'));
                if (System.IO.File.Exists(filePath)) System.IO.File.Delete(filePath);
            }
            catch { }

            _db.NoteAttachments.Remove(attachment);
            await _db.SaveChangesAsync();

            return Ok(ApiResponse<object>.Ok(new { attachmentId = attachmentId, noteId = id }, "File lampiran berhasil dihapus."));
        }

        /// <summary>
        /// Menghapus catatan (DELETE /api/notes/{id})
        /// </summary>
        /// <param name="id">ID Catatan</param>
        [HttpDelete("{id:int}")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Delete(int id)
        {
            var note = await _db.Notes.Include(n => n.Attachments).FirstOrDefaultAsync(n => n.Id == id);
            if (note == null)
            {
                return NotFound(ApiResponse<object>.Fail($"Catatan dengan ID {id} tidak ditemukan."));
            }

            _db.Notes.Remove(note);
            await _db.SaveChangesAsync();

            return Ok(ApiResponse<object>.Ok(new { id = id }, $"Catatan '{note.Title}' berhasil dihapus."));
        }

        private static NoteResponseDto MapToResponseDto(WorkNote n)
        {
            return new NoteResponseDto
            {
                Id = n.Id,
                Title = n.Title,
                ContentHtml = n.ContentHtml,
                PlainTextPreview = n.PlainTextPreview,
                Category = n.Category,
                Color = n.Color,
                IsPinned = n.IsPinned,
                IsStandalone = n.IsStandalone,
                CreatedAt = n.CreatedAt,
                UpdatedAt = n.UpdatedAt,
                AuthorUserId = n.AuthorUserId,
                AuthorUser = n.AuthorUser != null ? new UserShortDto
                {
                    Id = n.AuthorUser.Id,
                    FullName = n.AuthorUser.FullName,
                    Email = n.AuthorUser.Email ?? "",
                    JobTitle = n.AuthorUser.JobTitle,
                    AvatarColor = n.AuthorUser.AvatarColor
                } : null,
                TaskId = n.TaskId,
                TaskTitle = n.Task?.Title,
                TaskCode = n.Task?.TaskCode,
                AttachmentsCount = n.Attachments?.Count ?? 0,
                Attachments = n.Attachments?.Select(a => new NoteAttachmentDto
                {
                    Id = a.Id,
                    FileName = a.FileName,
                    FilePath = a.FilePath,
                    FileSize = a.FileSize,
                    ContentType = a.ContentType,
                    FileExtension = a.FileExtension,
                    UploadedAt = a.UploadedAt
                }).ToList() ?? new List<NoteAttachmentDto>()
            };
        }
    }
}
