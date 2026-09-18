using TrackerKerja.Models;

namespace TrackerKerja.Services
{
    public static class TaskPermissionHelper
    {
        /// <summary>
        /// Mengecek apakah pengguna berhak mengakses data milik perusahaan/tim tertentu.
        /// Admin dapat mengakses seluruh perusahaan. Pengguna biasa hanya perusahaannya sendiri.
        /// </summary>
        public static bool CanAccessCompany(AppUser? user, bool isAdmin, int? targetCompanyId)
        {
            if (isAdmin) return true;
            if (user == null || !user.CompanyId.HasValue) return false;
            if (!targetCompanyId.HasValue) return true;
            return user.CompanyId.Value == targetCompanyId.Value;
        }

        /// <summary>
        /// Mengecek apakah pengguna berhak melihat tugas tertentu berdasarkan isolasi tim/perusahaan.
        /// </summary>
        public static bool CanViewTask(AppUser? user, bool isAdmin, WorkTask? task)
        {
            if (isAdmin) return true;
            if (task == null) return false;
            if (user == null || !user.CompanyId.HasValue) return false;

            // Jika task memiliki CompanyId
            if (task.CompanyId.HasValue)
                return task.CompanyId.Value == user.CompanyId.Value;

            // Jika task terhubung ke Project dengan CompanyId
            if (task.Project != null && task.Project.CompanyId.HasValue)
                return task.Project.CompanyId.Value == user.CompanyId.Value;

            // Jika ditugaskan ke user di company yang sama
            if (task.AssignedToUser != null && task.AssignedToUser.CompanyId.HasValue)
                return task.AssignedToUser.CompanyId.Value == user.CompanyId.Value;

            return true;
        }

        /// <summary>
        /// Mengecek apakah pengguna berhak melihat proyek tertentu berdasarkan isolasi tim/perusahaan.
        /// </summary>
        public static bool CanViewProject(AppUser? user, bool isAdmin, Project? project)
        {
            if (isAdmin) return true;
            if (project == null) return false;
            if (user == null || !user.CompanyId.HasValue) return false;
            if (!project.CompanyId.HasValue) return true;
            return project.CompanyId.Value == user.CompanyId.Value;
        }

        /// <summary>
        /// Mengecek apakah pengguna memiliki posisi/jabatan khusus: System Analyst (SA) atau Technical Writer (TW).
        /// </summary>
        public static bool IsSpecialRole(AppUser? user)
        {
            if (user == null || string.IsNullOrWhiteSpace(user.JobTitle))
                return false;

            var job = user.JobTitle.Trim();

            // 1. System Analyst (SA)
            bool isSystemAnalyst = job.Contains("System Analyst", StringComparison.OrdinalIgnoreCase) ||
                                   job.Contains("Systems Analyst", StringComparison.OrdinalIgnoreCase) ||
                                   job.Contains("Analyst", StringComparison.OrdinalIgnoreCase) ||
                                   job.Equals("SA", StringComparison.OrdinalIgnoreCase);

            // 2. Technical Writer (TW)
            bool isTechnicalWriter = job.Contains("Technical Writer", StringComparison.OrdinalIgnoreCase) ||
                                     job.Contains("Tech Writer", StringComparison.OrdinalIgnoreCase) ||
                                     job.Contains("Writer", StringComparison.OrdinalIgnoreCase) ||
                                     job.Equals("TW", StringComparison.OrdinalIgnoreCase);

            return isSystemAnalyst || isTechnicalWriter;
        }

        /// <summary>
        /// Menentukan apakah pengguna berhak mengubah (Edit, Ubah Status, Kanban, Timer, dsb.) suatu tugas:
        /// - Administrator: Dapat mengubah tugas siapa saja.
        /// - System Analyst dan Technical Writer: Dapat mengubah tugas siapa saja (dalam perusahaannya).
        /// - Pengguna Lainnya (Developer, QA, dsb.): Hanya dapat mengubah tugas miliknya sendiri atau tugas tanpa penugasan.
        /// </summary>
        public static bool CanEditTask(AppUser? user, bool isAdmin, WorkTask? task)
        {
            if (user == null) return false;
            if (isAdmin) return true;
            if (task == null) return true;

            // Strict Company Isolation
            if (task.CompanyId.HasValue && user.CompanyId.HasValue && task.CompanyId.Value != user.CompanyId.Value)
                return false;

            if (IsSpecialRole(user)) return true;

            return string.IsNullOrEmpty(task.AssignedToUserId) || task.AssignedToUserId == user.Id;
        }

        /// <summary>
        /// Menentukan apakah pengguna berhak menghapus suatu tugas:
        /// - Administrator: Dapat menghapus tugas siapa saja (termasuk Clear All dan Bulk Delete).
        /// - System Analyst dan Technical Writer: TIDAK DAPAT menghapus tugas milik orang lain (hanya milik sendiri).
        /// - Pengguna Lainnya: Hanya dapat menghapus tugas miliknya sendiri atau tugas tanpa penugasan.
        /// </summary>
        public static bool CanDeleteTask(AppUser? user, bool isAdmin, WorkTask? task)
        {
            if (user == null) return false;
            if (isAdmin) return true;
            if (task == null) return false;

            // Strict Company Isolation
            if (task.CompanyId.HasValue && user.CompanyId.HasValue && task.CompanyId.Value != user.CompanyId.Value)
                return false;

            // SA, TW, dan pengguna biasa hanya boleh menghapus tugas miliknya sendiri
            return string.IsNullOrEmpty(task.AssignedToUserId) || task.AssignedToUserId == user.Id;
        }

        /// <summary>
        /// Mengecek apakah pengguna berhak melihat catatan tertentu berdasarkan isolasi tim/perusahaan.
        /// </summary>
        public static bool CanViewNote(AppUser? user, bool isAdmin, WorkNote? note)
        {
            if (isAdmin) return true;
            if (note == null) return false;
            if (user == null || !user.CompanyId.HasValue) return false;

            if (note.CompanyId.HasValue)
                return note.CompanyId.Value == user.CompanyId.Value;

            if (note.AuthorUser != null && note.AuthorUser.CompanyId.HasValue)
                return note.AuthorUser.CompanyId.Value == user.CompanyId.Value;

            if (note.Task != null && note.Task.CompanyId.HasValue)
                return note.Task.CompanyId.Value == user.CompanyId.Value;

            return true;
        }

        /// <summary>
        /// Mengecek apakah pengguna berhak mengedit / menghapus catatan tertentu.
        /// </summary>
        public static bool CanEditNote(AppUser? user, bool isAdmin, WorkNote? note)
        {
            if (isAdmin) return true;
            if (user == null || note == null) return false;
            if (!CanViewNote(user, isAdmin, note)) return false;

            return note.AuthorUserId == user.Id || IsSpecialRole(user);
        }
    }
}
