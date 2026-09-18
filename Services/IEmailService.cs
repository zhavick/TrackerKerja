using TrackerKerja.Models;
using TrackerKerja.ViewModels;

namespace TrackerKerja.Services
{
    public interface IEmailService
    {
        // ── SMTP Server Configuration & Diagnostics ────────────
        Task<EmailConfigDto> GetEmailConfigAsync();
        Task<bool> SaveEmailConfigAsync(EmailConfigDto config, string? userId = null);
        Task<(bool Success, string Message, string? Details)> TestSmtpConnectionAsync(string testRecipient, string? customSubject = null, string? customMessage = null);
        Task<(bool IsSuccess, string Message, long LatencyMs, string Diagnostics)> TestConnectionAsync(string recipientEmail);

        // ── Direct & Raw Email Dispatch ────────────────────────
        Task<(bool Success, string Message)> SendRawEmailAsync(string toEmail, string toName, string subject, string bodyHtml, bool isHtml = true);

        // ── Event-Driven Templated Email Dispatch ──────────────
        Task<(bool Success, string Message)> SendEventEmailAsync(string eventCode, string toEmail, string toName, Dictionary<string, string> placeholders);
        Task<(bool Success, string Message)> SendEventEmailAsync(string eventCode, string toEmail, Dictionary<string, string> placeholders);

        // ── Email Template Management ──────────────────────────
        Task<List<EmailTemplate>> GetAllTemplatesAsync();
        Task<EmailTemplate?> GetTemplateByIdAsync(int id);
        Task<EmailTemplate?> GetTemplateByEventCodeAsync(string eventCode);
        Task<bool> SaveTemplateAsync(EmailTemplate template, string? userId);
        Task<EmailTemplate?> SaveTemplateAsync(CreateOrUpdateEmailTemplateDto dto, string? userId);
        Task<bool> DeleteTemplateAsync(int id);

        // ── Template Parsing & Rendering Helper ────────────────
        string RenderContent(string template, Dictionary<string, string> placeholders);
        (string RenderedSubject, string RenderedHtml) RenderTemplate(EmailTemplate template, Dictionary<string, string>? sampleVariables);
        Dictionary<string, string> GetSamplePlaceholdersForEvent(string eventCode);
    }
}
