using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace TrackerKerja.Models
{
    public class WorkNote
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Judul catatan wajib diisi")]
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        public string ContentHtml { get; set; } = string.Empty;

        [MaxLength(50)]
        public string Category { get; set; } = "General";

        [MaxLength(7)]
        public string Color { get; set; } = "#6366F1";

        public bool IsPinned { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime UpdatedAt { get; set; } = DateTime.Now;

        // Author (Creator)
        public string? AuthorUserId { get; set; }
        public AppUser? AuthorUser { get; set; }

        // Optional Linked Task (Null if Standalone)
        public int? TaskId { get; set; }
        public WorkTask? Task { get; set; }

        // Uploaded File Attachments
        public List<NoteAttachment> Attachments { get; set; } = new();

        // Computed plain text excerpt
        public string PlainTextPreview
        {
            get
            {
                if (string.IsNullOrEmpty(ContentHtml)) return string.Empty;
                var text = Regex.Replace(ContentHtml, "<.*?>", string.Empty);
                text = System.Net.WebUtility.HtmlDecode(text).Trim();
                return text.Length > 160 ? text.Substring(0, 160) + "..." : text;
            }
        }

        // Computed flag: is standalone or linked to task
        public bool IsStandalone => !TaskId.HasValue;
    }
}
