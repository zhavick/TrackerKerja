using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TrackerKerja.Data;
using TrackerKerja.Models;

namespace TrackerKerja.Controllers
{
    [Authorize]
    public class NoteController : Controller
    {
        private readonly AppDbContext _db;
        private readonly UserManager<AppUser> _userManager;
        private readonly IWebHostEnvironment _env;

        public NoteController(AppDbContext db, UserManager<AppUser> userManager, IWebHostEnvironment env)
        {
            _db = db;
            _userManager = userManager;
            _env = env;
        }

        // ── USER FOLDER HELPER ─────────────────────────────────
        private string GetUserFolderName(AppUser? user)
        {
            if (user == null) return "general";

            // Prioritize FullName, then UserName, then Email prefix
            string raw = !string.IsNullOrWhiteSpace(user.FullName)
                ? user.FullName
                : (!string.IsNullOrWhiteSpace(user.UserName) ? user.UserName : "user");

            // Sanitize string to lowercase letters, numbers, and underscores only
            string clean = Regex.Replace(raw.ToLower().Trim(), @"[^a-z0-9]+", "_").Trim('_');
            return string.IsNullOrEmpty(clean) ? "general" : clean;
        }

        private async Task SaveAttachmentsAsync(List<IFormFile> files, int noteId, AppUser? currentUser)
        {
            if (files == null || files.Count == 0) return;

            var userFolder = GetUserFolderName(currentUser);
            var targetDirectory = Path.Combine(_env.WebRootPath, "uploads", "notes", userFolder);

            if (!Directory.Exists(targetDirectory))
            {
                Directory.CreateDirectory(targetDirectory);
            }

            foreach (var file in files)
            {
                if (file.Length == 0) continue;

                var origFileName = Path.GetFileName(file.FileName);
                var ext = Path.GetExtension(origFileName).ToLower();
                var safeBaseName = Regex.Replace(Path.GetFileNameWithoutExtension(origFileName), @"[^a-zA-Z0-9_\-\.]+", "_");
                
                // Format: 20260820_141520_a1b2c3d4_OriginalName.pdf
                var uniquePrefix = $"{DateTime.Now:yyyyMMdd_HHmmss}_{Guid.NewGuid().ToString("N").Substring(0, 8)}";
                var storedFileName = $"{uniquePrefix}_{safeBaseName}{ext}";
                var physicalPath = Path.Combine(targetDirectory, storedFileName);
                var relativePath = $"/uploads/notes/{userFolder}/{storedFileName}";

                using (var stream = new FileStream(physicalPath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                var attachment = new NoteAttachment
                {
                    NoteId = noteId,
                    FileName = origFileName,
                    FilePath = relativePath,
                    FileSize = file.Length,
                    ContentType = file.ContentType,
                    FileExtension = ext,
                    UploadedAt = DateTime.Now,
                    UploadedByUserId = currentUser?.Id
                };

                _db.NoteAttachments.Add(attachment);
            }

            await _db.SaveChangesAsync();
        }

        // ── INDEX: LIST ALL NOTES ──────────────────────────────
        public async Task<IActionResult> Index(string? filter, string? category, string? authorId, int? taskId, string? search)
        {
            var query = _db.Notes
                .Include(n => n.AuthorUser)
                .Include(n => n.Attachments)
                .Include(n => n.Task)
                    .ThenInclude(t => t!.Project)
                .AsQueryable();

            if (filter == "standalone")
                query = query.Where(n => n.TaskId == null);
            else if (filter == "linked")
                query = query.Where(n => n.TaskId != null);

            if (!string.IsNullOrEmpty(category) && category != "All")
                query = query.Where(n => n.Category == category);

            if (!string.IsNullOrEmpty(authorId))
                query = query.Where(n => n.AuthorUserId == authorId);

            if (taskId.HasValue)
                query = query.Where(n => n.TaskId == taskId);

            if (!string.IsNullOrEmpty(search))
                query = query.Where(n => n.Title.Contains(search) || (n.ContentHtml != null && n.ContentHtml.Contains(search)));

            var notes = await query
                .OrderByDescending(n => n.IsPinned)
                .ThenByDescending(n => n.UpdatedAt)
                .ToListAsync();

            ViewBag.Filter = filter;
            ViewBag.Category = category;
            ViewBag.AuthorId = authorId;
            ViewBag.TaskId = taskId;
            ViewBag.Search = search;

            ViewBag.Tasks = await _db.Tasks
                .Include(t => t.Project)
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync();

            ViewBag.Authors = await _db.Users
                .OrderBy(u => u.FullName)
                .ToListAsync();

            ViewBag.TotalCount = await _db.Notes.CountAsync();
            ViewBag.StandaloneCount = await _db.Notes.CountAsync(n => n.TaskId == null);
            ViewBag.LinkedCount = await _db.Notes.CountAsync(n => n.TaskId != null);

            return View(notes);
        }

        // ── CREATE NOTE ────────────────────────────────────────
        [HttpGet]
        public async Task<IActionResult> Create(int? taskId)
        {
            ViewBag.Tasks = await _db.Tasks
                .Include(t => t.Project)
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync();

            var model = new WorkNote
            {
                TaskId = taskId,
                Category = taskId.HasValue ? "Task Note" : "General"
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(WorkNote model, List<IFormFile>? attachments)
        {
            ModelState.Remove("AuthorUser");
            ModelState.Remove("Task");

            if (!ModelState.IsValid)
            {
                ViewBag.Tasks = await _db.Tasks
                    .Include(t => t.Project)
                    .OrderByDescending(t => t.CreatedAt)
                    .ToListAsync();
                return View(model);
            }

            var currentUser = await _userManager.GetUserAsync(User);
            model.AuthorUserId = currentUser?.Id;
            model.CreatedAt = DateTime.Now;
            model.UpdatedAt = DateTime.Now;

            _db.Notes.Add(model);
            await _db.SaveChangesAsync();

            // Save uploaded multiple attachments (if any)
            if (attachments != null && attachments.Count > 0)
            {
                await SaveAttachmentsAsync(attachments, model.Id, currentUser);
            }

            TempData["Success"] = "Catatan dan lampiran berhasil disimpan!";

            if (model.TaskId.HasValue)
                return RedirectToAction("Edit", "Task", new { id = model.TaskId.Value });

            return RedirectToAction(nameof(Index));
        }

        // ── EDIT NOTE ──────────────────────────────────────────
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var note = await _db.Notes
                .Include(n => n.AuthorUser)
                .Include(n => n.Task)
                .Include(n => n.Attachments)
                    .ThenInclude(a => a.UploadedByUser)
                .FirstOrDefaultAsync(n => n.Id == id);

            if (note == null) return NotFound();

            ViewBag.Tasks = await _db.Tasks
                .Include(t => t.Project)
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync();

            return View(note);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, WorkNote model, List<IFormFile>? newAttachments)
        {
            if (id != model.Id) return BadRequest();

            ModelState.Remove("AuthorUser");
            ModelState.Remove("Task");

            if (!ModelState.IsValid)
            {
                ViewBag.Tasks = await _db.Tasks
                    .Include(t => t.Project)
                    .OrderByDescending(t => t.CreatedAt)
                    .ToListAsync();

                var currentNote = await _db.Notes
                    .Include(n => n.AuthorUser)
                    .Include(n => n.Attachments)
                    .FirstOrDefaultAsync(n => n.Id == id);
                model.Attachments = currentNote?.Attachments ?? new();
                return View(model);
            }

            var existing = await _db.Notes
                .Include(n => n.Attachments)
                .FirstOrDefaultAsync(n => n.Id == id);

            if (existing == null) return NotFound();

            existing.Title = model.Title;
            existing.ContentHtml = model.ContentHtml;
            existing.Category = model.Category;
            existing.Color = model.Color;
            existing.IsPinned = model.IsPinned;
            existing.TaskId = model.TaskId;
            existing.UpdatedAt = DateTime.Now;

            await _db.SaveChangesAsync();

            // Save any newly added attachments
            if (newAttachments != null && newAttachments.Count > 0)
            {
                var currentUser = await _userManager.GetUserAsync(User);
                await SaveAttachmentsAsync(newAttachments, existing.Id, currentUser);
            }

            TempData["Success"] = "Catatan berhasil diperbarui!";
            return RedirectToAction(nameof(Details), new { id });
        }

        // ── DETAILS / VIEW NOTE ────────────────────────────────
        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var note = await _db.Notes
                .Include(n => n.AuthorUser)
                .Include(n => n.Attachments)
                    .ThenInclude(a => a.UploadedByUser)
                .Include(n => n.Task)
                    .ThenInclude(t => t!.Project)
                .FirstOrDefaultAsync(n => n.Id == id);

            if (note == null) return NotFound();

            return View(note);
        }

        // ── DOWNLOAD ATTACHMENT ────────────────────────────────
        [HttpGet]
        public async Task<IActionResult> DownloadAttachment(int id)
        {
            var attachment = await _db.NoteAttachments.FindAsync(id);
            if (attachment == null) return NotFound();

            var relativePath = attachment.FilePath.TrimStart('/', '\\');
            var physicalPath = Path.Combine(_env.WebRootPath, relativePath);

            if (!System.IO.File.Exists(physicalPath))
                return NotFound("File fisik tidak ditemukan di server.");

            var contentType = !string.IsNullOrEmpty(attachment.ContentType)
                ? attachment.ContentType
                : "application/octet-stream";

            return PhysicalFile(physicalPath, contentType, attachment.FileName);
        }

        // ── DELETE ATTACHMENT ──────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteAttachment(int id, int? returnNoteId)
        {
            var attachment = await _db.NoteAttachments.FindAsync(id);
            if (attachment != null)
            {
                var noteId = returnNoteId ?? attachment.NoteId;
                var relativePath = attachment.FilePath.TrimStart('/', '\\');
                var physicalPath = Path.Combine(_env.WebRootPath, relativePath);

                if (System.IO.File.Exists(physicalPath))
                {
                    try { System.IO.File.Delete(physicalPath); } catch { }
                }

                _db.NoteAttachments.Remove(attachment);
                await _db.SaveChangesAsync();

                TempData["Success"] = $"Lampiran '{attachment.FileName}' berhasil dihapus.";

                if (returnNoteId.HasValue)
                    return RedirectToAction(nameof(Edit), new { id = noteId });
            }

            return RedirectToAction(nameof(Index));
        }

        // ── TOGGLE PIN ─────────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> TogglePin(int id)
        {
            var note = await _db.Notes.FindAsync(id);
            if (note != null)
            {
                note.IsPinned = !note.IsPinned;
                note.UpdatedAt = DateTime.Now;
                await _db.SaveChangesAsync();
                TempData["Success"] = note.IsPinned ? "Catatan disematkan di atas." : "Semat catatan dilepas.";
            }
            return RedirectToAction(nameof(Index));
        }

        // ── DELETE NOTE ────────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id, int? returnTaskId)
        {
            var note = await _db.Notes
                .Include(n => n.Attachments)
                .FirstOrDefaultAsync(n => n.Id == id);

            if (note != null)
            {
                // Delete physical attachment files
                foreach (var att in note.Attachments)
                {
                    var relativePath = att.FilePath.TrimStart('/', '\\');
                    var physicalPath = Path.Combine(_env.WebRootPath, relativePath);
                    if (System.IO.File.Exists(physicalPath))
                    {
                        try { System.IO.File.Delete(physicalPath); } catch { }
                    }
                }

                _db.Notes.Remove(note);
                await _db.SaveChangesAsync();
                TempData["Success"] = "Catatan dan seluruh lampirannya berhasil dihapus!";
            }

            if (returnTaskId.HasValue)
                return RedirectToAction("Edit", "Task", new { id = returnTaskId.Value });

            return RedirectToAction(nameof(Index));
        }

        // ── BULK DELETE NOTES ──────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BulkDelete(List<int> ids)
        {
            if (ids == null || !ids.Any())
            {
                TempData["Error"] = "Pilih setidaknya satu catatan untuk dihapus.";
                return RedirectToAction(nameof(Index));
            }

            var notes = await _db.Notes
                .Include(n => n.Attachments)
                .Where(n => ids.Contains(n.Id))
                .ToListAsync();

            var count = notes.Count;

            // Delete physical attachment files
            foreach (var n in notes)
            {
                foreach (var att in n.Attachments)
                {
                    var relativePath = att.FilePath.TrimStart('/', '\\');
                    var physicalPath = Path.Combine(_env.WebRootPath, relativePath);
                    if (System.IO.File.Exists(physicalPath))
                    {
                        try { System.IO.File.Delete(physicalPath); } catch { }
                    }
                }
            }

            _db.Notes.RemoveRange(notes);
            await _db.SaveChangesAsync();

            TempData["Success"] = $"Sebanyak {count} catatan berhasil dihapus secara massal.";
            return RedirectToAction(nameof(Index));
        }

        // ── AJAX IMAGE UPLOAD FOR QUILL EDITOR ─────────────────
        [HttpPost]
        public async Task<IActionResult> UploadImage(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest(new { error = "File tidak boleh kosong" });

            var allowedExts = new[] { ".jpg", ".jpeg", ".png", ".webp", ".gif", ".svg" };
            var ext = Path.GetExtension(file.FileName).ToLower();

            if (!allowedExts.Contains(ext))
                return BadRequest(new { error = "Format gambar tidak didukung" });

            if (file.Length > 10 * 1024 * 1024) // 10MB limit
                return BadRequest(new { error = "Ukuran gambar maksimal 10MB" });

            var currentUser = await _userManager.GetUserAsync(User);
            var userFolder = GetUserFolderName(currentUser);
            var uploadsFolder = Path.Combine(_env.WebRootPath, "uploads", "notes", userFolder);
            
            if (!Directory.Exists(uploadsFolder))
                Directory.CreateDirectory(uploadsFolder);

            var uniqueFileName = $"{DateTime.Now:yyyyMMdd_HHmmss}_{Guid.NewGuid().ToString("N").Substring(0, 8)}{ext}";
            var filePath = Path.Combine(uploadsFolder, uniqueFileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            return Json(new { url = $"/uploads/notes/{userFolder}/{uniqueFileName}" });
        }
    }
}
