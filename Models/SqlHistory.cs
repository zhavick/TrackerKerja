using System.ComponentModel.DataAnnotations;

namespace TrackerKerja.Models
{
    public class SqlHistory
    {
        public int Id { get; set; }

        [Required, MaxLength(200)]
        public string Name { get; set; } = "SQL Query";

        public string Content { get; set; } = string.Empty;

        [MaxLength(50)]
        public string Dialect { get; set; } = "sql";

        public int? TaskId { get; set; }
        public WorkTask? Task { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
