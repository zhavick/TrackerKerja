using System.ComponentModel.DataAnnotations;

namespace TrackerKerja.Models
{
    /// <summary>
    /// Entitas Template Pengiriman Email Berbasis Event
    /// </summary>
    public class EmailTemplate
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string EventCode { get; set; } = string.Empty;

        [Required]
        [MaxLength(200)]
        public string EventName { get; set; } = string.Empty;

        [MaxLength(50)]
        public string Category { get; set; } = "General"; // "Account", "Tasks", "System"

        [Required]
        [MaxLength(300)]
        public string Subject { get; set; } = string.Empty;

        [Required]
        public string BodyHtml { get; set; } = string.Empty;

        [MaxLength(1000)]
        public string? AvailableVariables { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public DateTime UpdatedAt { get; set; } = DateTime.Now;

        [MaxLength(450)]
        public string? UpdatedByUserId { get; set; }
    }
}
