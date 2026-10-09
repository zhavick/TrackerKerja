using System.ComponentModel.DataAnnotations;

namespace TrackerKerja.Models
{
    public class Announcement
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Judul pengumuman wajib diisi")]
        [StringLength(200, ErrorMessage = "Judul maksimal 200 karakter")]
        [Display(Name = "Judul Pengumuman")]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "Isi pengumuman wajib diisi")]
        [Display(Name = "Isi Pengumuman")]
        public string Content { get; set; } = string.Empty;

        [Required(ErrorMessage = "Tanggal pemberitahuan wajib diisi")]
        [Display(Name = "Tanggal Pemberitahuan / Pengumuman")]
        [DataType(DataType.Date)]
        public DateTime AnnouncementDate { get; set; } = DateTime.Today;

        [Required(ErrorMessage = "Tanggal berakhir wajib diisi")]
        [Display(Name = "Tanggal Berakhir")]
        [DataType(DataType.Date)]
        public DateTime EndDate { get; set; } = DateTime.Today.AddDays(7);

        [Required(ErrorMessage = "Status pengumuman wajib dipilih")]
        [StringLength(50)]
        [Display(Name = "Status")]
        public string Status { get; set; } = "Pengumuman Umum"; // "Informasi Penting" | "Pengumuman Umum"

        [Display(Name = "Status Aktif")]
        public bool IsActive { get; set; } = true;

        public string? CreatedByUserId { get; set; }
        public AppUser? CreatedByUser { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime UpdatedAt { get; set; } = DateTime.Now;
    }
}
