using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TrackerKerja.Data;
using TrackerKerja.Models;
using System.Text.Json;

namespace TrackerKerja.Controllers
{
    [Authorize]
    public class JsonToolsController : Controller
    {
        private readonly AppDbContext _db;
        public JsonToolsController(AppDbContext db) { _db = db; }

        public async Task<IActionResult> Index()
        {
            var history = await _db.JsonHistories
                .Include(j => j.Task)
                .OrderByDescending(j => j.CreatedAt)
                .Take(20)
                .ToListAsync();
            return View(history);
        }

        [HttpPost]
        public IActionResult Format([FromBody] JsonRequest req)
        {
            if (string.IsNullOrWhiteSpace(req.Content))
                return Json(new { success = false, error = "Input kosong" });
            try
            {
                var doc = JsonDocument.Parse(req.Content);
                var formatted = JsonSerializer.Serialize(doc, new JsonSerializerOptions { WriteIndented = true });
                return Json(new { success = true, result = formatted });
            }
            catch (JsonException ex)
            {
                return Json(new { success = false, error = $"JSON tidak valid: {ex.Message}" });
            }
        }

        [HttpPost]
        public IActionResult Minify([FromBody] JsonRequest req)
        {
            if (string.IsNullOrWhiteSpace(req.Content))
                return Json(new { success = false, error = "Input kosong" });
            try
            {
                var doc = JsonDocument.Parse(req.Content);
                var minified = JsonSerializer.Serialize(doc);
                return Json(new { success = true, result = minified });
            }
            catch (JsonException ex)
            {
                return Json(new { success = false, error = $"JSON tidak valid: {ex.Message}" });
            }
        }

        [HttpPost]
        public IActionResult Validate([FromBody] JsonRequest req)
        {
            if (string.IsNullOrWhiteSpace(req.Content))
                return Json(new { success = false, error = "Input kosong" });
            try
            {
                JsonDocument.Parse(req.Content);
                return Json(new { success = true, message = "JSON valid!" });
            }
            catch (JsonException ex)
            {
                return Json(new { success = false, error = ex.Message, line = ex.LineNumber });
            }
        }

        [HttpPost]
        public async Task<IActionResult> Save([FromBody] SaveJsonRequest req)
        {
            var history = new JsonHistory
            {
                Name = string.IsNullOrWhiteSpace(req.Name) ? $"JSON {DateTime.Now:dd/MM HH:mm}" : req.Name,
                Content = req.Content,
                TaskId = req.TaskId,
                CreatedAt = DateTime.Now
            };
            _db.JsonHistories.Add(history);
            await _db.SaveChangesAsync();
            return Json(new { success = true, id = history.Id });
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var item = await _db.JsonHistories.FindAsync(id);
            if (item != null)
            {
                _db.JsonHistories.Remove(item);
                await _db.SaveChangesAsync();
            }
            return Json(new { success = true });
        }

        [HttpGet]
        public async Task<IActionResult> GetHistory(int id)
        {
            var item = await _db.JsonHistories.FindAsync(id);
            if (item == null) return Json(new { success = false });
            return Json(new { success = true, content = item.Content, name = item.Name });
        }
    }

    public class JsonRequest { public string Content { get; set; } = string.Empty; }
    public class SaveJsonRequest { public string Name { get; set; } = string.Empty; public string Content { get; set; } = string.Empty; public int? TaskId { get; set; } }
}
