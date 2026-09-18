using System.Diagnostics;
using System.Net;
using System.Net.Mail;
using Microsoft.EntityFrameworkCore;
using TrackerKerja.Data;
using TrackerKerja.Models;
using TrackerKerja.ViewModels;

namespace TrackerKerja.Services
{
    public class EmailService : IEmailService
    {
        private readonly AppDbContext _db;
        private readonly ILogger<EmailService> _logger;

        public EmailService(AppDbContext db, ILogger<EmailService> logger)
        {
            _db = db;
            _logger = logger;
        }

        // ── 1. SMTP CONFIGURATION ──────────────────────────────
        public async Task<EmailConfigDto> GetEmailConfigAsync()
        {
            var settings = await _db.SystemSettings
                .Where(s => s.Key.StartsWith("Email_"))
                .ToDictionaryAsync(s => s.Key, s => s.Value);

            var host = settings.GetValueOrDefault("Email_SmtpHost", "smtp.gmail.com");
            var portStr = settings.GetValueOrDefault("Email_SmtpPort", "587");
            int.TryParse(portStr, out var port);
            if (port <= 0) port = 587;

            var senderEmail = settings.GetValueOrDefault("Email_SenderEmail", "notifications@trackerkerja.com");
            var senderName = settings.GetValueOrDefault("Email_SenderName", "Work Tracker Pro");
            var senderPassword = settings.GetValueOrDefault("Email_SenderPassword", "");
            var enableSslStr = settings.GetValueOrDefault("Email_EnableSsl", "true");
            var requireAuthStr = settings.GetValueOrDefault("Email_RequireAuth", "true");
            var isEnabledStr = settings.GetValueOrDefault("Email_IsEnabled", "true");
            var updatedAtStr = settings.GetValueOrDefault("Email_UpdatedAt", "");

            DateTime? updatedAt = null;
            if (DateTime.TryParse(updatedAtStr, out var dt))
                updatedAt = dt;

            return new EmailConfigDto
            {
                SmtpHost = host,
                SmtpPort = port,
                SenderEmail = senderEmail,
                SenderName = senderName,
                SenderPassword = senderPassword,
                EnableSsl = !string.Equals(enableSslStr, "false", StringComparison.OrdinalIgnoreCase),
                RequireAuth = !string.Equals(requireAuthStr, "false", StringComparison.OrdinalIgnoreCase),
                IsEnabled = !string.Equals(isEnabledStr, "false", StringComparison.OrdinalIgnoreCase),
                UpdatedAt = updatedAt
            };
        }

        public async Task<bool> SaveEmailConfigAsync(EmailConfigDto config, string? userId)
        {
            try
            {
                var dict = new Dictionary<string, (string Value, string Description)>
                {
                    { "Email_SmtpHost", (config.SmtpHost.Trim(), "Host / server SMTP untuk pengiriman email") },
                    { "Email_SmtpPort", (config.SmtpPort.ToString(), "Port SMTP (contoh: 587 untuk STARTTLS, 465 untuk SSL)") },
                    { "Email_SenderEmail", (config.SenderEmail.Trim(), "Alamat email pengirim default") },
                    { "Email_SenderName", (config.SenderName.Trim(), "Nama tampilan pengirim (Display Name)") },
                    { "Email_EnableSsl", (config.EnableSsl ? "true" : "false", "Aktifkan enkripsi SSL / TLS") },
                    { "Email_RequireAuth", (config.RequireAuth ? "true" : "false", "Memerlukan autentikasi username & password") },
                    { "Email_IsEnabled", (config.IsEnabled ? "true" : "false", "Status aktif integrasi pengiriman email") },
                    { "Email_UpdatedAt", (DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), "Waktu terakhir konfigurasi email diperbarui") }
                };

                // Only update password if provided (do not overwrite with empty)
                if (!string.IsNullOrEmpty(config.SenderPassword))
                {
                    dict["Email_SenderPassword"] = (config.SenderPassword, "Password / App Password email pengirim");
                }

                foreach (var (key, (val, desc)) in dict)
                {
                    var setting = await _db.SystemSettings.FirstOrDefaultAsync(s => s.Key == key);
                    if (setting == null)
                    {
                        _db.SystemSettings.Add(new SystemSetting
                        {
                            Key = key,
                            Value = val,
                            Description = desc,
                            UpdatedAt = DateTime.Now
                        });
                    }
                    else
                    {
                        setting.Value = val;
                        setting.Description = desc;
                        setting.UpdatedAt = DateTime.Now;
                    }
                }

                await _db.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save Email SMTP configuration");
                return false;
            }
        }

        // ── 2. TEST SMTP CONNECTION ────────────────────────────
        public async Task<(bool Success, string Message, string? Details)> TestSmtpConnectionAsync(string testRecipient, string? customSubject = null, string? customMessage = null)
        {
            var res = await TestConnectionAsync(testRecipient);
            return (res.IsSuccess, res.Message, res.Diagnostics);
        }

        public async Task<(bool IsSuccess, string Message, long LatencyMs, string Diagnostics)> TestConnectionAsync(string recipientEmail)
        {
            if (string.IsNullOrWhiteSpace(recipientEmail))
            {
                return (false, "Email penerima test tidak boleh kosong.", 0, "No recipient provided");
            }

            var config = await GetEmailConfigAsync();
            if (string.IsNullOrWhiteSpace(config.SmtpHost))
            {
                return (false, "Host SMTP belum dikonfigurasi.", 0, "SmtpHost is empty");
            }

            var sw = Stopwatch.StartNew();
            try
            {
                var subject = $"[Test Koneksi] Uji Integrasi Email Work Tracker Pro — {DateTime.Now:HH:mm:ss}";
                var appBaseUrlSetting = await _db.SystemSettings.FirstOrDefaultAsync(s => s.Key == "GlobalBaseUrl");
                var baseUrl = appBaseUrlSetting?.Value ?? "http://localhost:5148";

                var bodyHtml = $@"
<div style=""font-family: 'Inter', -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif; max-width: 600px; margin: 0 auto; padding: 24px; background: #ffffff; border-radius: 16px; border: 1px solid #e2e8f0; color: #1e293b;"">
    <div style=""text-align: center; margin-bottom: 24px;"">
        <div style=""display: inline-block; background: linear-gradient(135deg, #6366F1, #8B5CF6); color: #ffffff; padding: 12px 20px; border-radius: 12px; font-weight: 900; font-size: 16px;"">
            🚀 Work Tracker Pro
        </div>
        <h2 style=""font-size: 20px; font-weight: 800; color: #0f172a; margin-top: 16px; margin-bottom: 4px;"">Uji Koneksi Email Berhasil!</h2>
        <p style=""font-size: 13px; color: #64748b; margin: 0;"">Pesan ini dikirim sebagai pengujian konfigurasi SMTP server sistem.</p>
    </div>

    <div style=""background: #f8fafc; border: 1px solid #e2e8f0; border-radius: 12px; padding: 16px; margin-bottom: 20px;"">
        <h3 style=""font-size: 13px; font-weight: 700; color: #334155; margin-top: 0; margin-bottom: 12px; text-transform: uppercase; letter-spacing: 0.5px;"">📋 Rincian Parameter SMTP:</h3>
        <table style=""width: 100%; font-size: 12px; border-collapse: collapse;"">
            <tr>
                <td style=""padding: 6px 0; color: #64748b; width: 140px;"">SMTP Host:</td>
                <td style=""padding: 6px 0; font-weight: 600; color: #0f172a;""><code>{config.SmtpHost}</code></td>
            </tr>
            <tr>
                <td style=""padding: 6px 0; color: #64748b;"">Port:</td>
                <td style=""padding: 6px 0; font-weight: 600; color: #0f172a;"">{config.SmtpPort}</td>
            </tr>
            <tr>
                <td style=""padding: 6px 0; color: #64748b;"">Pengirim:</td>
                <td style=""padding: 6px 0; font-weight: 600; color: #0f172a;"">{config.SenderName} &lt;{config.SenderEmail}&gt;</td>
            </tr>
            <tr>
                <td style=""padding: 6px 0; color: #64748b;"">Enkripsi SSL/TLS:</td>
                <td style=""padding: 6px 0; font-weight: 600; color: #10b981;"">{(config.EnableSsl ? "Aktif (SSL/TLS)" : "Non-Aktif")}</td>
            </tr>
            <tr>
                <td style=""padding: 6px 0; color: #64748b;"">Waktu Uji:</td>
                <td style=""padding: 6px 0; font-weight: 600; color: #0f172a;"">{DateTime.Now:dd MMM yyyy HH:mm:ss}</td>
            </tr>
        </table>
    </div>

    <div style=""text-align: center; margin-top: 24px; padding-top: 16px; border-top: 1px solid #e2e8f0; font-size: 11px; color: #94a3b8;"">
        <p style=""margin: 0;"">Email otomatis dari sistem <strong>Work Tracker Pro</strong> &bull; <a href=""{baseUrl}"" style=""color: #6366f1; text-decoration: none;"">{baseUrl}</a></p>
    </div>
</div>";

                var result = await SendRawEmailAsync(recipientEmail.Trim(), "Administrator Test", subject, bodyHtml, true);
                sw.Stop();

                var diag = $"Host: {config.SmtpHost}:{config.SmtpPort} | SSL: {config.EnableSsl} | Auth: {config.RequireAuth} | Latency: {sw.ElapsedMilliseconds}ms | Status: {(result.Success ? "250 OK" : "FAILED")}";

                if (result.Success)
                {
                    return (true, $"Koneksi SMTP berhasil! Email percobaan sukses terkirim ke '{recipientEmail}' dalam {sw.ElapsedMilliseconds} ms.", sw.ElapsedMilliseconds, diag);
                }
                else
                {
                    return (false, $"Gagal mengirim email: {result.Message}", sw.ElapsedMilliseconds, $"{diag}\nError: {result.Message}");
                }
            }
            catch (Exception ex)
            {
                sw.Stop();
                _logger.LogError(ex, "SMTP test connection error to {Host}:{Port}", config.SmtpHost, config.SmtpPort);
                return (false, $"Kesalahan koneksi SMTP: {ex.Message}", sw.ElapsedMilliseconds, $"Exception: {ex.Message}\nStackTrace: {ex.StackTrace}");
            }
        }

        // ── 3. DIRECT RAW EMAIL DISPATCH ───────────────────────
        public async Task<(bool Success, string Message)> SendRawEmailAsync(string toEmail, string toName, string subject, string bodyHtml, bool isHtml = true)
        {
            try
            {
                var config = await GetEmailConfigAsync();
                if (!config.IsEnabled)
                {
                    _logger.LogInformation("Email sending skipped because Email_IsEnabled is set to false.");
                    return (false, "Pengiriman email dinonaktifkan di pengaturan sistem.");
                }

                if (string.IsNullOrWhiteSpace(config.SmtpHost) || string.IsNullOrWhiteSpace(config.SenderEmail))
                {
                    return (false, "Konfigurasi Host atau Email Pengirim SMTP belum lengkap.");
                }

                using var mailMessage = new MailMessage();
                mailMessage.From = new MailAddress(config.SenderEmail, config.SenderName);
                mailMessage.To.Add(new MailAddress(toEmail.Trim(), string.IsNullOrWhiteSpace(toName) ? toEmail : toName.Trim()));
                mailMessage.Subject = subject;
                mailMessage.Body = bodyHtml;
                mailMessage.IsBodyHtml = isHtml;

                using var smtpClient = new SmtpClient(config.SmtpHost, config.SmtpPort);
                smtpClient.EnableSsl = config.EnableSsl;
                smtpClient.Timeout = config.TimeoutSeconds > 0 ? config.TimeoutSeconds * 1000 : 15000;

                if (config.RequireAuth && !string.IsNullOrEmpty(config.SenderPassword))
                {
                    smtpClient.UseDefaultCredentials = false;
                    smtpClient.Credentials = new NetworkCredential(config.SenderEmail, config.SenderPassword);
                }
                else
                {
                    smtpClient.UseDefaultCredentials = true;
                }

                await smtpClient.SendMailAsync(mailMessage);
                _logger.LogInformation("Email sent successfully to {ToEmail} with subject '{Subject}'", toEmail, subject);
                return (true, "Email berhasil dikirim.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send email to {ToEmail}", toEmail);
                return (false, ex.Message);
            }
        }

        // ── 4. EVENT-DRIVEN TEMPLATED EMAIL DISPATCH ───────────
        public async Task<(bool Success, string Message)> SendEventEmailAsync(string eventCode, string toEmail, Dictionary<string, string> placeholders)
        {
            return await SendEventEmailAsync(eventCode, toEmail, toEmail, placeholders);
        }

        public async Task<(bool Success, string Message)> SendEventEmailAsync(string eventCode, string toEmail, string toName, Dictionary<string, string> placeholders)
        {
            try
            {
                var template = await GetTemplateByEventCodeAsync(eventCode);
                if (template == null || !template.IsActive)
                {
                    _logger.LogWarning("Email template for event code '{EventCode}' not found or inactive.", eventCode);
                    return (false, $"Template email untuk event '{eventCode}' tidak ditemukan atau sedang dinonaktifkan.");
                }

                // Global placeholders
                var appBaseUrlSetting = await _db.SystemSettings.FirstOrDefaultAsync(s => s.Key == "GlobalBaseUrl");
                var baseUrl = appBaseUrlSetting?.Value ?? "http://localhost:5148";

                var mergedPlaceholders = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    { "AppName", "Work Tracker Pro" },
                    { "AppUrl", baseUrl },
                    { "CurrentYear", DateTime.Now.Year.ToString() },
                    { "CurrentDate", DateTime.Now.ToString("dd MMMM yyyy") },
                    { "CurrentTime", DateTime.Now.ToString("HH:mm:ss") },
                    { "RecipientEmail", toEmail },
                    { "RecipientName", toName }
                };

                foreach (var kv in placeholders)
                {
                    mergedPlaceholders[kv.Key] = kv.Value;
                }

                var renderedSubject = RenderContent(template.Subject, mergedPlaceholders);
                var renderedBody = RenderContent(template.BodyHtml, mergedPlaceholders);

                return await SendRawEmailAsync(toEmail, toName, renderedSubject, renderedBody, true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send event email for event '{EventCode}' to {ToEmail}", eventCode, toEmail);
                return (false, ex.Message);
            }
        }

        // ── 5. EMAIL TEMPLATES CRUD ────────────────────────────
        public async Task<List<EmailTemplate>> GetAllTemplatesAsync()
        {
            return await _db.EmailTemplates
                .OrderBy(t => t.Category)
                .ThenBy(t => t.EventName)
                .ToListAsync();
        }

        public async Task<EmailTemplate?> GetTemplateByIdAsync(int id)
        {
            return await _db.EmailTemplates.FindAsync(id);
        }

        public async Task<EmailTemplate?> GetTemplateByEventCodeAsync(string eventCode)
        {
            return await _db.EmailTemplates
                .FirstOrDefaultAsync(t => t.EventCode.ToLower() == eventCode.Trim().ToLower());
        }

        public async Task<bool> SaveTemplateAsync(EmailTemplate template, string? userId)
        {
            try
            {
                template.UpdatedAt = DateTime.Now;
                template.UpdatedByUserId = userId;

                if (template.Id == 0)
                {
                    template.CreatedAt = DateTime.Now;
                    _db.EmailTemplates.Add(template);
                }
                else
                {
                    _db.EmailTemplates.Update(template);
                }

                await _db.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save email template with event code '{EventCode}'", template.EventCode);
                return false;
            }
        }

        public async Task<EmailTemplate?> SaveTemplateAsync(CreateOrUpdateEmailTemplateDto dto, string? userId)
        {
            try
            {
                EmailTemplate? template;
                if (dto.Id > 0)
                {
                    template = await _db.EmailTemplates.FindAsync(dto.Id);
                    if (template == null) return null;

                    template.EventName = dto.EventName.Trim();
                    template.Category = dto.Category.Trim();
                    template.Subject = dto.Subject.Trim();
                    template.BodyHtml = dto.BodyHtml;
                    template.AvailableVariables = dto.AvailableVariables?.Trim();
                    template.IsActive = dto.IsActive;
                    template.UpdatedAt = DateTime.Now;
                    template.UpdatedByUserId = userId;
                }
                else
                {
                    template = new EmailTemplate
                    {
                        EventCode = dto.EventCode.Trim().ToUpperInvariant(),
                        EventName = dto.EventName.Trim(),
                        Category = dto.Category.Trim(),
                        Subject = dto.Subject.Trim(),
                        BodyHtml = dto.BodyHtml,
                        AvailableVariables = dto.AvailableVariables?.Trim(),
                        IsActive = dto.IsActive,
                        CreatedAt = DateTime.Now,
                        UpdatedAt = DateTime.Now,
                        UpdatedByUserId = userId
                    };
                    _db.EmailTemplates.Add(template);
                }

                await _db.SaveChangesAsync();
                return template;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save email template from DTO with event code '{EventCode}'", dto.EventCode);
                return null;
            }
        }

        public async Task<bool> DeleteTemplateAsync(int id)
        {
            try
            {
                var template = await _db.EmailTemplates.FindAsync(id);
                if (template == null) return false;

                _db.EmailTemplates.Remove(template);
                await _db.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete email template with ID {Id}", id);
                return false;
            }
        }

        // ── 6. TEMPLATE RENDERING HELPER ───────────────────────
        public string RenderContent(string template, Dictionary<string, string> placeholders)
        {
            if (string.IsNullOrEmpty(template)) return string.Empty;

            var result = template;
            foreach (var (key, value) in placeholders)
            {
                // Support both {Key} and {{Key}}
                result = result.Replace($"{{{key}}}", value ?? "", StringComparison.OrdinalIgnoreCase);
                result = result.Replace($"{{{{{key}}}}}", value ?? "", StringComparison.OrdinalIgnoreCase);
            }
            return result;
        }

        public Dictionary<string, string> GetSamplePlaceholdersForEvent(string eventCode)
        {
            var code = eventCode?.ToUpperInvariant() ?? "";
            var sample = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { "FullName", "Budi Santoso" },
                { "Email", "budi.santoso@example.com" },
                { "JobTitle", "Lead Backend Developer" },
                { "CompanyName", "PT Elistec Teknologi" },
                { "AppName", "Work Tracker Pro" },
                { "AppUrl", "http://localhost:5148" },
                { "ActionUrl", "http://localhost:5148/Account/Login" },
                { "CurrentDate", DateTime.Now.ToString("dd MMMM yyyy") },
                { "CurrentTime", DateTime.Now.ToString("HH:mm:ss") },
                { "CurrentYear", DateTime.Now.Year.ToString() },
                { "AdminName", "Administrator" },
                { "RejectionReason", "Data kelengkapan profil belum sesuai standar perusahaan." },
                { "NewPassword", "P@ssw0rd2026!" },
                { "TaskTitle", "Implementasi REST API Webhook Notifikasi" },
                { "TaskCode", "TSK-0104" },
                { "ProjectName", "Work Tracker Pro Core" },
                { "Priority", "High" },
                { "Status", "In Progress" },
                { "Milestone", "Implementation" },
                { "DueDate", DateTime.Now.AddDays(7).ToString("dd MMM yyyy") },
                { "AssigneeName", "Budi Santoso" },
                { "CreatedByName", "Glenn Hakim" }
            };

            return sample;
        }

        public (string RenderedSubject, string RenderedHtml) RenderTemplate(EmailTemplate template, Dictionary<string, string>? sampleVariables)
        {
            var merged = GetSamplePlaceholdersForEvent(template.EventCode);
            if (sampleVariables != null)
            {
                foreach (var (k, v) in sampleVariables)
                {
                    merged[k] = v;
                }
            }
            var sub = RenderContent(template.Subject, merged);
            var body = RenderContent(template.BodyHtml, merged);
            return (sub, body);
        }
    }
}
