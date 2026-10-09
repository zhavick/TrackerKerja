using System.ComponentModel.DataAnnotations;
using TrackerKerja.Models;

namespace TrackerKerja.ViewModels
{
    public class AnnouncementFormViewModel
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
        [Display(Name = "Status")]
        public string Status { get; set; } = "Pengumuman Umum"; // "Informasi Penting" | "Pengumuman Umum"

        [Display(Name = "Status Aktif")]
        public bool IsActive { get; set; } = true;
    }

    public class AnnouncementIndexViewModel
    {
        public List<Announcement> Announcements { get; set; } = new();
        public string? Search { get; set; }
        public string? StatusFilter { get; set; }
        public string? ActiveFilter { get; set; }
        public int TotalCount { get; set; }
        public int ActiveCount { get; set; }
        public int ImportantCount { get; set; }
        public int GeneralCount { get; set; }
        public int ExpiredCount { get; set; }
    }

    public class AnnouncementPopupDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public string AnnouncementDate { get; set; } = string.Empty;
        public string EndDate { get; set; } = string.Empty;
        public string Status { get; set; } = "Pengumuman Umum";
        public bool IsImportant { get; set; }
        public int DaysRemaining { get; set; }
        public string? CreatedByName { get; set; }
    }
}
