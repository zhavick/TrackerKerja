using System.ComponentModel.DataAnnotations;

namespace TrackerKerja.Models
{
    public class JsonHistory
    {
        public int Id { get; set; }

        [Required, MaxLength(200)]
        public string Name { get; set; } = "Untitled";

        public string Content { get; set; } = string.Empty;

        public int? TaskId { get; set; }
        public WorkTask? Task { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
