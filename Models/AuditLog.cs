using System.ComponentModel.DataAnnotations;

namespace TrackerKerja.Models
{
    public class AuditLog
    {
        public int Id { get; set; }

        [MaxLength(100)]
        public string? UserId { get; set; }

        [MaxLength(150)]
        public string? UserEmail { get; set; }

        [MaxLength(150)]
        public string? UserName { get; set; }

        [MaxLength(100)]
        public string ControllerName { get; set; } = string.Empty;

        [MaxLength(100)]
        public string ActionName { get; set; } = string.Empty;

        [MaxLength(10)]
        public string HttpMethod { get; set; } = string.Empty;

        [MaxLength(300)]
        public string Path { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? QueryString { get; set; }

        [MaxLength(50)]
        public string? IpAddress { get; set; }

        public int StatusCode { get; set; }

        public long DurationMs { get; set; }

        public DateTime Timestamp { get; set; } = DateTime.Now;

        public string? Details { get; set; }
    }
}
