using System.ComponentModel.DataAnnotations;

namespace TrackerKerja.Models
{
    public class NoteAttachment
    {
        public int Id { get; set; }

        public int NoteId { get; set; }
        public WorkNote? Note { get; set; }

        [Required]
        [MaxLength(255)]
        public string FileName { get; set; } = string.Empty;

        [Required]
        [MaxLength(500)]
        public string FilePath { get; set; } = string.Empty;

        public long FileSize { get; set; }

        [MaxLength(100)]
        public string? ContentType { get; set; }

        [MaxLength(20)]
        public string? FileExtension { get; set; }

        public DateTime UploadedAt { get; set; } = DateTime.Now;

        public string? UploadedByUserId { get; set; }
        public AppUser? UploadedByUser { get; set; }

        // Helper computed property: Formatted size string
        public string FormattedSize
        {
            get
            {
                if (FileSize < 1024) return $"{FileSize} B";
                if (FileSize < 1024 * 1024) return $"{FileSize / 1024.0:F1} KB";
                return $"{FileSize / (1024.0 * 1024.0):F2} MB";
            }
        }

        // Helper computed property: FontAwesome icon class
        public string IconClass => FileExtension?.ToLower() switch
        {
            ".pdf" => "fas fa-file-pdf text-red-500",
            ".doc" or ".docx" => "fas fa-file-word text-blue-500",
            ".xls" or ".xlsx" or ".csv" => "fas fa-file-excel text-emerald-500",
            ".ppt" or ".pptx" => "fas fa-file-powerpoint text-orange-500",
            ".jpg" or ".jpeg" or ".png" or ".webp" or ".gif" or ".svg" => "fas fa-file-image text-purple-500",
            ".zip" or ".rar" or ".7z" or ".tar" or ".gz" => "fas fa-file-archive text-amber-500",
            ".txt" or ".md" or ".json" or ".log" => "fas fa-file-alt text-slate-500",
            ".mp4" or ".mov" or ".avi" or ".mkv" => "fas fa-file-video text-pink-500",
            ".mp3" or ".wav" or ".ogg" => "fas fa-file-audio text-teal-500",
            _ => "fas fa-file text-indigo-500"
        };

        // Helper computed property: Is Image Previewable
        public bool IsImage => FileExtension?.ToLower() is ".jpg" or ".jpeg" or ".png" or ".webp" or ".gif" or ".svg";
    }
}
