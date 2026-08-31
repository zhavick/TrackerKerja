using TrackerKerja.Models;

namespace TrackerKerja.Services
{
    public static class TaskPermissionHelper
    {
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
        /// - System Analyst dan Technical Writer: Dapat mengubah tugas siapa saja.
        /// - Pengguna Lainnya (Developer, QA, dsb.): Hanya dapat mengubah tugas miliknya sendiri atau tugas tanpa penugasan.
        /// </summary>
        public static bool CanEditTask(AppUser? user, bool isAdmin, WorkTask? task)
        {
            if (user == null) return false;
            if (isAdmin) return true;
            if (IsSpecialRole(user)) return true;
            if (task == null) return true;

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

            // SA, TW, dan pengguna biasa hanya boleh menghapus tugas miliknya sendiri
            return string.IsNullOrEmpty(task.AssignedToUserId) || task.AssignedToUserId == user.Id;
        }
    }
}
