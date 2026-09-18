using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using TrackerKerja.Models;
using TrackerKerja.Services;
using TrackerKerja.ViewModels;

namespace TrackerKerja.Controllers.Api
{
    [ApiController]
    [Route("api/email-config")]
    [Produces("application/json")]
    [Authorize(Roles = "Admin")]
    public class EmailConfigApiController : ControllerBase
    {
        private readonly IEmailService _emailService;
        private readonly UserManager<AppUser> _userManager;

        public EmailConfigApiController(IEmailService emailService, UserManager<AppUser> userManager)
        {
            _emailService = emailService;
            _userManager = userManager;
        }

        /// <summary>
        /// Mengambil konfigurasi server SMTP email saat ini (GET /api/email-config)
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(ApiResponse<EmailConfigDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetConfig()
        {
            var config = await _emailService.GetEmailConfigAsync();
            // Mask password for security
            var safeConfig = new EmailConfigDto
            {
                IsEnabled = config.IsEnabled,
                MailHost = config.MailHost,
                MailPort = config.MailPort,
                SenderEmail = config.SenderEmail,
                SenderPassword = string.IsNullOrEmpty(config.SenderPassword) ? "" : "••••••••",
                SenderName = config.SenderName,
                EnableSsl = config.EnableSsl,
                TimeoutSeconds = config.TimeoutSeconds,
                UpdatedAt = config.UpdatedAt
            };

            return Ok(ApiResponse<EmailConfigDto>.Ok(safeConfig, "Konfigurasi server email SMTP berhasil diambil."));
        }

        /// <summary>
        /// Memperbarui pengaturan server SMTP email (PUT /api/email-config)
        /// </summary>
        /// <param name="dto">Data konfigurasi email SMTP</param>
        [HttpPut]
        [ProducesResponseType(typeof(ApiResponse<EmailConfigDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> UpdateConfig([FromBody] EmailConfigDto dto)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                return BadRequest(ApiResponse<object>.Fail("Validasi konfigurasi gagal.", errors));
            }

            try
            {
                await _emailService.SaveEmailConfigAsync(dto);
                var updated = await _emailService.GetEmailConfigAsync();
                return Ok(ApiResponse<EmailConfigDto>.Ok(updated, "Pengaturan server email SMTP berhasil diperbarui."));
            }
            catch (Exception ex)
            {
                return BadRequest(ApiResponse<object>.Fail($"Gagal menyimpan pengaturan email: {ex.Message}"));
            }
        }

        /// <summary>
        /// Menguji koneksi SMTP dan mengirim email percobaan (POST /api/email-config/test)
        /// </summary>
        /// <param name="dto">Payload email penerima</param>
        [HttpPost("test")]
        [ProducesResponseType(typeof(ApiResponse<TestEmailResponseDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<TestEmailResponseDto>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> TestConnection([FromBody] TestEmailRequestDto dto)
        {
            var recipient = dto?.RecipientEmail;
            if (string.IsNullOrWhiteSpace(recipient))
            {
                var currentUser = await _userManager.GetUserAsync(User);
                recipient = currentUser?.Email;
            }

            if (string.IsNullOrWhiteSpace(recipient))
            {
                return BadRequest(ApiResponse<TestEmailResponseDto>.Fail("Email penerima uji coba tidak boleh kosong."));
            }

            var result = await _emailService.TestConnectionAsync(recipient);
            var responseDto = new TestEmailResponseDto
            {
                IsSuccess = result.IsSuccess,
                Message = result.Message,
                LatencyMs = result.LatencyMs,
                Diagnostics = result.Diagnostics,
                Recipient = recipient
            };

            if (result.IsSuccess)
            {
                return Ok(ApiResponse<TestEmailResponseDto>.Ok(responseDto, result.Message));
            }

            return BadRequest(ApiResponse<TestEmailResponseDto>.Fail(result.Message, new List<string> { result.Diagnostics }));
        }

        /// <summary>
        /// Mengambil daftar seluruh template email event (GET /api/email-config/templates)
        /// </summary>
        [HttpGet("templates")]
        [ProducesResponseType(typeof(ApiResponse<List<EmailTemplateDto>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetTemplates()
        {
            var templates = await _emailService.GetAllTemplatesAsync();
            var dtos = templates.Select(t => new EmailTemplateDto
            {
                Id = t.Id,
                EventCode = t.EventCode,
                EventName = t.EventName,
                Category = t.Category,
                Subject = t.Subject,
                BodyHtml = t.BodyHtml,
                AvailableVariables = t.AvailableVariables,
                IsActive = t.IsActive,
                CreatedAt = t.CreatedAt,
                UpdatedAt = t.UpdatedAt
            }).ToList();

            return Ok(ApiResponse<List<EmailTemplateDto>>.Ok(dtos, "Daftar template email event berhasil diambil."));
        }

        /// <summary>
        /// Mengambil detail satu template email (GET /api/email-config/templates/{id})
        /// </summary>
        /// <param name="id">ID Template</param>
        [HttpGet("templates/{id:int}")]
        [ProducesResponseType(typeof(ApiResponse<EmailTemplateDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetTemplateById(int id)
        {
            var template = await _emailService.GetTemplateByIdAsync(id);
            if (template == null)
            {
                return NotFound(ApiResponse<object>.Fail($"Template email dengan ID {id} tidak ditemukan."));
            }

            var dto = new EmailTemplateDto
            {
                Id = template.Id,
                EventCode = template.EventCode,
                EventName = template.EventName,
                Category = template.Category,
                Subject = template.Subject,
                BodyHtml = template.BodyHtml,
                AvailableVariables = template.AvailableVariables,
                IsActive = template.IsActive,
                CreatedAt = template.CreatedAt,
                UpdatedAt = template.UpdatedAt
            };

            return Ok(ApiResponse<EmailTemplateDto>.Ok(dto, "Detail template email berhasil diambil."));
        }

        /// <summary>
        /// Membuat atau memperbarui template email event (POST /api/email-config/templates)
        /// </summary>
        /// <param name="dto">Payload data template</param>
        [HttpPost("templates")]
        [ProducesResponseType(typeof(ApiResponse<EmailTemplateDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> SaveTemplate([FromBody] CreateOrUpdateEmailTemplateDto dto)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                return BadRequest(ApiResponse<object>.Fail("Validasi template gagal.", errors));
            }

            var currentUser = await _userManager.GetUserAsync(User);
            var saved = await _emailService.SaveTemplateAsync(dto, currentUser?.Id);

            if (saved == null)
            {
                return BadRequest(ApiResponse<object>.Fail("Gagal menyimpan template email."));
            }

            var resDto = new EmailTemplateDto
            {
                Id = saved.Id,
                EventCode = saved.EventCode,
                EventName = saved.EventName,
                Category = saved.Category,
                Subject = saved.Subject,
                BodyHtml = saved.BodyHtml,
                AvailableVariables = saved.AvailableVariables,
                IsActive = saved.IsActive,
                CreatedAt = saved.CreatedAt,
                UpdatedAt = saved.UpdatedAt
            };

            return Ok(ApiResponse<EmailTemplateDto>.Ok(resDto, $"Template email '{saved.EventName}' berhasil disimpan."));
        }

        /// <summary>
        /// Menghapus template email kustom (DELETE /api/email-config/templates/{id})
        /// </summary>
        /// <param name="id">ID Template</param>
        [HttpDelete("templates/{id:int}")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteTemplate(int id)
        {
            var deleted = await _emailService.DeleteTemplateAsync(id);
            if (!deleted)
            {
                return NotFound(ApiResponse<object>.Fail($"Template email dengan ID {id} tidak ditemukan."));
            }

            return Ok(ApiResponse<object>.Ok(new { id }, "Template email berhasil dihapus."));
        }

        /// <summary>
        /// Pratinjau render template email dengan data placeholder kustom (POST /api/email-config/templates/{id}/preview)
        /// </summary>
        /// <param name="id">ID Template</param>
        /// <param name="req">Nilai variabel opsional</param>
        [HttpPost("templates/{id:int}/preview")]
        [ProducesResponseType(typeof(ApiResponse<TemplatePreviewResponseDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> PreviewTemplate(int id, [FromBody] TemplatePreviewRequestDto? req = null)
        {
            var template = await _emailService.GetTemplateByIdAsync(id);
            if (template == null)
            {
                return NotFound(ApiResponse<object>.Fail($"Template email dengan ID {id} tidak ditemukan."));
            }

            var rendered = _emailService.RenderTemplate(template, req?.SampleVariables);
            var res = new TemplatePreviewResponseDto
            {
                RenderedSubject = rendered.RenderedSubject,
                RenderedHtml = rendered.RenderedHtml,
                EventCode = template.EventCode,
                EventName = template.EventName
            };

            return Ok(ApiResponse<TemplatePreviewResponseDto>.Ok(res, "Pratinjau template berhasil dibuat."));
        }
    }
}
