using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TrackerKerja.Data;
using TrackerKerja.Models;
using TrackerKerja.ViewModels;

namespace TrackerKerja.Controllers.Api
{
    /// <summary>
    /// Modul API Utilitas Developer dan SQL Processor (Format, Beautify, Minify, Validasi, dan Simpan Snippet Query)
    /// </summary>
    [ApiController]
    [Route("api/sql-tools")]
    [Produces("application/json")]
    public class SqlToolsApiController : ControllerBase
    {
        private readonly AppDbContext _db;

        public SqlToolsApiController(AppDbContext db)
        {
            _db = db;
        }

        /// <summary>
        /// Melakukan formatting / pretty-print pada string SQL query (POST /api/sql-tools/format)
        /// </summary>
        /// <param name="dto">Payload konfigurasi dan query SQL yang akan diformat</param>
        [HttpPost("format")]
        [ProducesResponseType(typeof(ApiResponse<FormatSqlResponseDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        public IActionResult Format([FromBody] FormatSqlRequestDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Content))
            {
                return BadRequest(ApiResponse<object>.Fail("Konten SQL tidak boleh kosong."));
            }

            try
            {
                var req = new SqlFormatRequest
                {
                    Content = dto.Content,
                    Dialect = dto.Dialect,
                    IndentSize = dto.IndentSize,
                    UseTabs = dto.UseTabs,
                    KeywordCase = dto.KeywordCase,
                    IdentifierCase = dto.IdentifierCase,
                    LinesBetweenQueries = dto.LinesBetweenQueries
                };

                var formatted = SqlFormatterEngine.Format(dto.Content, req);
                var origBytes = Encoding.UTF8.GetByteCount(dto.Content);
                var formBytes = Encoding.UTF8.GetByteCount(formatted);
                var lines = formatted.Split('\n').Length;

                var response = new FormatSqlResponseDto
                {
                    FormattedContent = formatted,
                    IsValid = true,
                    Dialect = dto.Dialect,
                    OriginalSizeBytes = origBytes,
                    FormattedSizeBytes = formBytes,
                    LineCount = lines
                };

                return Ok(ApiResponse<FormatSqlResponseDto>.Ok(response, "SQL berhasil diformat dan dirapikan."));
            }
            catch (Exception ex)
            {
                return BadRequest(ApiResponse<FormatSqlResponseDto>.Fail($"Format SQL gagal: {ex.Message}"));
            }
        }

        /// <summary>
        /// Melakukan minifikasi / kompresi whitespace pada query SQL (POST /api/sql-tools/minify)
        /// </summary>
        /// <param name="dto">Payload string query SQL yang akan dikompres</param>
        [HttpPost("minify")]
        [ProducesResponseType(typeof(ApiResponse<MinifySqlResponseDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        public IActionResult Minify([FromBody] MinifySqlRequestDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Content))
            {
                return BadRequest(ApiResponse<object>.Fail("Konten SQL tidak boleh kosong."));
            }

            try
            {
                var minified = SqlFormatterEngine.Minify(dto.Content);
                var origBytes = Encoding.UTF8.GetByteCount(dto.Content);
                var minBytes = Encoding.UTF8.GetByteCount(minified);
                var ratio = origBytes > 0 ? Math.Round((1.0 - (double)minBytes / origBytes) * 100.0, 1) : 0;

                var response = new MinifySqlResponseDto
                {
                    MinifiedContent = minified,
                    IsValid = true,
                    OriginalSizeBytes = origBytes,
                    MinifiedSizeBytes = minBytes,
                    CompressionRatioPercent = ratio
                };

                return Ok(ApiResponse<MinifySqlResponseDto>.Ok(response, "SQL berhasil diminifikasi."));
            }
            catch (Exception ex)
            {
                return BadRequest(ApiResponse<MinifySqlResponseDto>.Fail($"Minify SQL gagal: {ex.Message}"));
            }
        }

        /// <summary>
        /// Memvalidasi struktur dasar sintaks query SQL (POST /api/sql-tools/validate)
        /// </summary>
        /// <param name="dto">Payload string SQL yang akan divalidasi</param>
        [HttpPost("validate")]
        [ProducesResponseType(typeof(ApiResponse<ValidateSqlResponseDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<ValidateSqlResponseDto>), StatusCodes.Status400BadRequest)]
        public IActionResult Validate([FromBody] ValidateSqlRequestDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Content))
            {
                return BadRequest(ApiResponse<ValidateSqlResponseDto>.Fail("Konten SQL tidak boleh kosong."));
            }

            var result = SqlFormatterEngine.Validate(dto.Content);
            if (result.IsValid)
            {
                var response = new ValidateSqlResponseDto
                {
                    IsValid = true,
                    Dialect = dto.Dialect,
                    ErrorMessage = null
                };
                return Ok(ApiResponse<ValidateSqlResponseDto>.Ok(response, "Sintaks dasar SQL valid."));
            }
            else
            {
                var response = new ValidateSqlResponseDto
                {
                    IsValid = false,
                    Dialect = dto.Dialect,
                    ErrorMessage = result.Error,
                    LineNumber = result.LineNumber
                };
                return BadRequest(ApiResponse<ValidateSqlResponseDto>.Fail($"Sintaks SQL tidak valid: {result.Error}", new List<string> { result.Error ?? "Error sintaks" }));
            }
        }

        /// <summary>
        /// Menyimpan snippet SQL ke riwayat database (POST /api/sql-tools/save)
        /// </summary>
        /// <param name="dto">Payload nama, konten, dialek, dan task opsional</param>
        [HttpPost("save")]
        [ProducesResponseType(typeof(ApiResponse<SqlHistoryResponseDto>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Save([FromBody] SaveSqlSnippetRequestDto dto)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                return BadRequest(ApiResponse<object>.Fail("Validasi payload gagal.", errors));
            }

            var item = new SqlHistory
            {
                Name = dto.Name.Trim(),
                Content = dto.Content,
                Dialect = string.IsNullOrWhiteSpace(dto.Dialect) ? "sql" : dto.Dialect.Trim(),
                TaskId = dto.TaskId,
                CreatedAt = DateTime.Now
            };

            _db.SqlHistories.Add(item);
            await _db.SaveChangesAsync();

            var size = Encoding.UTF8.GetByteCount(item.Content);
            var sizeFormatted = size > 1024 ? $"{Math.Round(size / 1024.0, 1)} KB" : $"{size} B";

            var response = new SqlHistoryResponseDto
            {
                Id = item.Id,
                Name = item.Name,
                Content = item.Content,
                Dialect = item.Dialect,
                TaskId = item.TaskId,
                CreatedAt = item.CreatedAt,
                SizeFormatted = sizeFormatted
            };

            return CreatedAtAction(nameof(GetHistoryById), new { id = item.Id }, ApiResponse<SqlHistoryResponseDto>.Ok(response, "Snippet SQL berhasil disimpan ke riwayat."));
        }

        /// <summary>
        /// Mengambil daftar riwayat snippet SQL yang tersimpan (GET /api/sql-tools/history)
        /// </summary>
        [HttpGet("history")]
        [ProducesResponseType(typeof(ApiResponse<List<SqlHistoryResponseDto>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetHistory()
        {
            var history = await _db.SqlHistories
                .Include(s => s.Task)
                .AsNoTracking()
                .OrderByDescending(s => s.CreatedAt)
                .Take(50)
                .ToListAsync();

            var response = history.Select(s =>
            {
                var size = Encoding.UTF8.GetByteCount(s.Content);
                var sizeFormatted = size > 1024 ? $"{Math.Round(size / 1024.0, 1)} KB" : $"{size} B";
                return new SqlHistoryResponseDto
                {
                    Id = s.Id,
                    Name = s.Name,
                    Content = s.Content,
                    Dialect = s.Dialect,
                    TaskId = s.TaskId,
                    TaskTitle = s.Task?.Title,
                    CreatedAt = s.CreatedAt,
                    SizeFormatted = sizeFormatted
                };
            }).ToList();

            return Ok(ApiResponse<List<SqlHistoryResponseDto>>.Ok(response, $"Berhasil mengambil {response.Count} riwayat SQL."));
        }

        /// <summary>
        /// Mengambil detail satu snippet SQL dari riwayat (GET /api/sql-tools/history/{id})
        /// </summary>
        /// <param name="id">ID Riwayat SQL</param>
        [HttpGet("history/{id:int}")]
        [ProducesResponseType(typeof(ApiResponse<SqlHistoryResponseDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetHistoryById(int id)
        {
            var item = await _db.SqlHistories
                .Include(s => s.Task)
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.Id == id);

            if (item == null)
            {
                return NotFound(ApiResponse<object>.Fail($"Riwayat SQL dengan ID {id} tidak ditemukan."));
            }

            var size = Encoding.UTF8.GetByteCount(item.Content);
            var sizeFormatted = size > 1024 ? $"{Math.Round(size / 1024.0, 1)} KB" : $"{size} B";

            var response = new SqlHistoryResponseDto
            {
                Id = item.Id,
                Name = item.Name,
                Content = item.Content,
                Dialect = item.Dialect,
                TaskId = item.TaskId,
                TaskTitle = item.Task?.Title,
                CreatedAt = item.CreatedAt,
                SizeFormatted = sizeFormatted
            };

            return Ok(ApiResponse<SqlHistoryResponseDto>.Ok(response, "Detail riwayat SQL berhasil diambil."));
        }

        /// <summary>
        /// Menghapus satu snippet SQL dari riwayat (DELETE /api/sql-tools/history/{id})
        /// </summary>
        /// <param name="id">ID Riwayat SQL yang akan dihapus</param>
        [HttpDelete("history/{id:int}")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteHistory(int id)
        {
            var item = await _db.SqlHistories.FirstOrDefaultAsync(s => s.Id == id);
            if (item == null)
            {
                return NotFound(ApiResponse<object>.Fail($"Riwayat SQL dengan ID {id} tidak ditemukan."));
            }

            _db.SqlHistories.Remove(item);
            await _db.SaveChangesAsync();

            return Ok(ApiResponse<object>.Ok(new { id = id }, $"Riwayat SQL '{item.Name}' berhasil dihapus."));
        }
    }
}
