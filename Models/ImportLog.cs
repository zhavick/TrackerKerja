using System.ComponentModel.DataAnnotations;

namespace TrackerKerja.Models
{
    public class ImportLog
    {
        public int Id { get; set; }

        [MaxLength(200)]
        public string FileName { get; set; } = string.Empty;

        public int TotalRows { get; set; }
        public int SuccessRows { get; set; }
        public int FailedRows { get; set; }
        public string? Errors { get; set; }
        public DateTime ImportedAt { get; set; } = DateTime.Now;
        public string? ImportedBy { get; set; }
    }
}
