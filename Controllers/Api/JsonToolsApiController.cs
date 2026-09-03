using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TrackerKerja.Data;
using TrackerKerja.Models;
using TrackerKerja.ViewModels;

namespace TrackerKerja.Controllers.Api
{
    /// <summary>
    /// Modul API Utilitas Developer dan JSON Processor (Format, Minify, Validasi, dan Simpan Snippet)
    /// </summary>
    [ApiController]
    [Route("api/json-tools")]
    [Produces("application/json")]
    public class JsonToolsApiController : ControllerBase
    {
        private readonly AppDbContext _db;

        public JsonToolsApiController(AppDbContext db)
        {
            _db = db;
        }





        /// <summary>
        /// Melakukan formatting / pretty-print pada string JSON (POST /api/json-tools/format)
        /// </summary>
        /// <param name="dto">Payload string JSON yang akan diformat</param>
        [HttpPost("format")]
        [ProducesResponseType(typeof(ApiResponse<FormatJsonResponseDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        public IActionResult Format([FromBody] FormatJsonRequestDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Content))
            {
                return BadRequest(ApiResponse<object>.Fail("Konten JSON tidak boleh kosong."));
            }

            try
            {
                var doc = JsonDocument.Parse(dto.Content);
                var formatted = JsonSerializer.Serialize(doc, new JsonSerializerOptions { WriteIndented = true });
                var originalSize = System.Text.Encoding.UTF8.GetByteCount(dto.Content);
                var formattedSize = System.Text.Encoding.UTF8.GetByteCount(formatted);

                var response = new FormatJsonResponseDto
                {
                    FormattedContent = formatted,
                    IsValid = true,
                    OriginalSizeBytes = originalSize,
                    FormattedSizeBytes = formattedSize
                };

                return Ok(ApiResponse<FormatJsonResponseDto>.Ok(response, "JSON berhasil diformat."));
            }
            catch (JsonException ex)
            {
                return BadRequest(ApiResponse<FormatJsonResponseDto>.Fail($"Format JSON tidak valid: {ex.Message}"));
            }
        }

        /// <summary>
        /// Melakukan minifikasi / kompresi whitespace pada string JSON (POST /api/json-tools/minify)
        /// </summary>
        /// <param name="dto">Payload string JSON yang akan dikompres</param>
        [HttpPost("minify")]
        [ProducesResponseType(typeof(ApiResponse<MinifyJsonResponseDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        public IActionResult Minify([FromBody] MinifyJsonRequestDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Content))
            {
                return BadRequest(ApiResponse<object>.Fail("Konten JSON tidak boleh kosong."));
            }

            try
            {
                var doc = JsonDocument.Parse(dto.Content);
                var minified = JsonSerializer.Serialize(doc);
                var originalSize = System.Text.Encoding.UTF8.GetByteCount(dto.Content);
                var minifiedSize = System.Text.Encoding.UTF8.GetByteCount(minified);
                var ratio = originalSize > 0 ? Math.Round((1.0 - (double)minifiedSize / originalSize) * 100.0, 1) : 0;

                var response = new MinifyJsonResponseDto
                {
                    MinifiedContent = minified,
                    IsValid = true,
                    OriginalSizeBytes = originalSize,
                    MinifiedSizeBytes = minifiedSize,
                    CompressionRatioPercent = ratio
                };

                return Ok(ApiResponse<MinifyJsonResponseDto>.Ok(response, "JSON berhasil diminifikasi."));
            }
            catch (JsonException ex)
            {
                return BadRequest(ApiResponse<MinifyJsonResponseDto>.Fail($"Format JSON tidak valid: {ex.Message}"));
            }
        }

        /// <summary>
        /// Memvalidasi sintaks struktur string JSON (POST /api/json-tools/validate)
        /// </summary>
        /// <param name="dto">Payload string JSON yang akan divalidasi</param>
        [HttpPost("validate")]
        [ProducesResponseType(typeof(ApiResponse<ValidateJsonResponseDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<ValidateJsonResponseDto>), StatusCodes.Status400BadRequest)]
        public IActionResult Validate([FromBody] ValidateJsonRequestDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Content))
            {
                return BadRequest(ApiResponse<ValidateJsonResponseDto>.Fail("Konten JSON tidak boleh kosong."));
            }

            try
            {
                using var doc = JsonDocument.Parse(dto.Content);
                var response = new ValidateJsonResponseDto
                {
                    IsValid = true,
                    ErrorMessage = null
                };

                return Ok(ApiResponse<ValidateJsonResponseDto>.Ok(response, "Sintaks JSON valid."));
            }
            catch (JsonException ex)
            {
                var response = new ValidateJsonResponseDto
                {
                    IsValid = false,
                    ErrorMessage = ex.Message,
                    LineNumber = ex.LineNumber,
                    BytePosition = ex.BytePositionInLine
                };

                return BadRequest(ApiResponse<ValidateJsonResponseDto>.Fail($"Sintaks JSON tidak valid (Baris: {ex.LineNumber}): {ex.Message}", new List<string> { ex.Message }));
            }
        }

        /// <summary>
        /// Menyimpan snippet JSON ke riwayat database (POST /api/json-tools/save)
        /// </summary>
        /// <param name="dto">Payload nama dan konten snippet JSON</param>
        [HttpPost("save")]
        [ProducesResponseType(typeof(ApiResponse<JsonHistoryResponseDto>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Save([FromBody] SaveJsonSnippetRequestDto dto)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                return BadRequest(ApiResponse<object>.Fail("Validasi payload gagal.", errors));
            }

            var item = new JsonHistory
            {
                Name = dto.Name.Trim(),
                Content = dto.Content,
                CreatedAt = DateTime.Now
            };

            _db.JsonHistories.Add(item);
            await _db.SaveChangesAsync();

            var size = System.Text.Encoding.UTF8.GetByteCount(item.Content);
            var sizeFormatted = size > 1024 ? $"{Math.Round(size / 1024.0, 1)} KB" : $"{size} B";

            var response = new JsonHistoryResponseDto
            {
                Id = item.Id,
                Name = item.Name,
                Content = item.Content,
                CreatedAt = item.CreatedAt,
                SizeFormatted = sizeFormatted
            };

            return CreatedAtAction(nameof(GetHistoryById), new { id = item.Id }, ApiResponse<JsonHistoryResponseDto>.Ok(response, "Snippet JSON berhasil disimpan ke riwayat."));
        }

        /// <summary>
        /// Mengambil daftar riwayat snippet JSON yang tersimpan (GET /api/json-tools/history)
        /// </summary>
        [HttpGet("history")]
        [ProducesResponseType(typeof(ApiResponse<List<JsonHistoryResponseDto>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetHistory()
        {
            var history = await _db.JsonHistories
                .AsNoTracking()
                .OrderByDescending(j => j.CreatedAt)
                .Take(50)
                .ToListAsync();

            var response = history.Select(j =>
            {
                var size = System.Text.Encoding.UTF8.GetByteCount(j.Content);
                var sizeFormatted = size > 1024 ? $"{Math.Round(size / 1024.0, 1)} KB" : $"{size} B";
                return new JsonHistoryResponseDto
                {
                    Id = j.Id,
                    Name = j.Name,
                    Content = j.Content,
                    CreatedAt = j.CreatedAt,
                    SizeFormatted = sizeFormatted
                };
            }).ToList();

            return Ok(ApiResponse<List<JsonHistoryResponseDto>>.Ok(response, $"Berhasil mengambil {response.Count} riwayat JSON."));
        }

        /// <summary>
        /// Mengambil detail satu snippet JSON dari riwayat (GET /api/json-tools/history/{id})
        /// </summary>
        /// <param name="id">ID Riwayat JSON</param>
        [HttpGet("history/{id:int}")]
        [ProducesResponseType(typeof(ApiResponse<JsonHistoryResponseDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetHistoryById(int id)
        {
            var item = await _db.JsonHistories.AsNoTracking().FirstOrDefaultAsync(j => j.Id == id);
            if (item == null)
            {
                return NotFound(ApiResponse<object>.Fail($"Riwayat JSON dengan ID {id} tidak ditemukan."));
            }

            var size = System.Text.Encoding.UTF8.GetByteCount(item.Content);
            var sizeFormatted = size > 1024 ? $"{Math.Round(size / 1024.0, 1)} KB" : $"{size} B";

            var response = new JsonHistoryResponseDto
            {
                Id = item.Id,
                Name = item.Name,
                Content = item.Content,
                CreatedAt = item.CreatedAt,
                SizeFormatted = sizeFormatted
            };

            return Ok(ApiResponse<JsonHistoryResponseDto>.Ok(response, "Detail riwayat JSON berhasil diambil."));
        }

        /// <summary>
        /// Menghapus satu snippet JSON dari riwayat (DELETE /api/json-tools/history/{id})
        /// </summary>
        /// <param name="id">ID Riwayat JSON yang akan dihapus</param>
        [HttpDelete("history/{id:int}")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteHistory(int id)
        {
            var item = await _db.JsonHistories.FirstOrDefaultAsync(j => j.Id == id);
            if (item == null)
            {
                return NotFound(ApiResponse<object>.Fail($"Riwayat JSON dengan ID {id} tidak ditemukan."));
            }

            _db.JsonHistories.Remove(item);
            await _db.SaveChangesAsync();

            return Ok(ApiResponse<object>.Ok(new { id = id }, $"Riwayat JSON '{item.Name}' berhasil dihapus."));
        }
    }
}
