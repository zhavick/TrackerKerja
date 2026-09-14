using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace TrackerKerja.ViewModels
{
    /// <summary>
    /// Status response saat pengujian koneksi (ping) ke Host Induk
    /// </summary>
    public class SyncPingResponseDto
    {
        public bool IsOnline { get; set; } = true;
        public string HostName { get; set; } = "Work Tracker Pro Host";
        public string AppVersion { get; set; } = "v3.1";
        public string DatabaseType { get; set; } = "SQLite";
        public DateTime ServerTime { get; set; } = DateTime.Now;
        public int TotalTasks { get; set; }
        public int TotalSessions { get; set; }
        public int TotalProjects { get; set; }
        public int TotalUsers { get; set; }
        public int TotalUploadFiles { get; set; }
        public long TotalUploadsSizeBytes { get; set; }
        public string TotalUploadsFormatted { get; set; } = "0 B";
        public string Message { get; set; } = "Koneksi ke Host Induk berhasil diverifikasi.";
    }

    /// <summary>
    /// Struktur paket data sinkronisasi yang dikirim antar instance
    /// </summary>
    public class SyncPayloadDto
    {
        public string SourceInstanceUrl { get; set; } = string.Empty;
        public string SourceLabel { get; set; } = "Child Instance";
        public DateTime ExportedAt { get; set; } = DateTime.Now;
        public string Version { get; set; } = "v3.1";
        public Dictionary<string, int> RecordCounts { get; set; } = new();
        public string SqlScript { get; set; } = string.Empty;

        // File synchronization payload (Zip archive base64)
        public string? FilesZipBase64 { get; set; }
        public bool IncludeFiles { get; set; } = true;
        public int FilesCount { get; set; }
        public long FilesSizeBytes { get; set; }
    }

    /// <summary>
    /// Payload request saat Child melakukan push/submit data ke Host Induk
    /// </summary>
    public class SyncPushRequestDto
    {
        [Required(ErrorMessage = "Target Host URL wajib diisi.")]
        public string TargetHostUrl { get; set; } = string.Empty;

        public string? ApiKey { get; set; }

        public bool CleanBeforeSync { get; set; } = true;

        public bool BackupBeforeSync { get; set; } = true;

        public bool SyncFiles { get; set; } = true;

        public string? SourceLabel { get; set; }
    }

    /// <summary>
    /// Payload request saat Child melakukan pull data &amp; berkas dari Host Induk
    /// </summary>
    public class SyncPullRequestDto
    {
        [Required(ErrorMessage = "Target Host URL wajib diisi.")]
        public string TargetHostUrl { get; set; } = string.Empty;

        public string? ApiKey { get; set; }

        public bool CleanBeforeSync { get; set; } = true;

        public bool BackupBeforeSync { get; set; } = true;

        public bool SyncFiles { get; set; } = true;
    }

    /// <summary>
    /// Request langsung ke endpoint receive Host Induk
    /// </summary>
    public class SyncReceiveRequestDto
    {
        public bool CleanBeforeSync { get; set; } = true;
        public bool BackupBeforeSync { get; set; } = true;
        public string SourceInstanceUrl { get; set; } = string.Empty;
        public string SourceLabel { get; set; } = "Child Application";
        public DateTime Timestamp { get; set; } = DateTime.Now;
        public string SqlScript { get; set; } = string.Empty;
        public Dictionary<string, int>? RecordCounts { get; set; }

        // File synchronization payload
        public string? FilesZipBase64 { get; set; }
        public bool IncludeFiles { get; set; } = true;
        public int FilesCount { get; set; }
        public long FilesSizeBytes { get; set; }
    }

    /// <summary>
    /// Hasil eksekusi proses sinkronisasi
    /// </summary>
    public class SyncResultDto
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public int ExecutedStatementsCount { get; set; }
        public List<string> AffectedTables { get; set; } = new();
        public Dictionary<string, int> FinalTableStats { get; set; } = new();
        public int SyncedFilesCount { get; set; }
        public long SyncedFilesSizeBytes { get; set; }
        public string SyncedFilesSizeFormatted
        {
            get
            {
                if (SyncedFilesSizeBytes < 1024) return $"{SyncedFilesSizeBytes} B";
                if (SyncedFilesSizeBytes < 1024 * 1024) return $"{SyncedFilesSizeBytes / 1024.0:F1} KB";
                return $"{SyncedFilesSizeBytes / (1024.0 * 1024.0):F2} MB";
            }
        }
        public long ExecutionDurationMs { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.Now;
        public string? BackupFileName { get; set; }
        public string? ErrorDetails { get; set; }
    }

    /// <summary>
    /// Pengaturan konfigurasi sinkronisasi yang tersimpan di sistem
    /// </summary>
    public class SyncSettingsDto
    {
        public string TargetHostUrl { get; set; } = "http://localhost:5000";
        public string ApiKey { get; set; } = string.Empty;
        public string Role { get; set; } = "child"; // "child", "host", "standalone"
        public DateTime? LastSyncAt { get; set; }
        public string? LastSyncStatus { get; set; }
        public bool CleanBeforeSyncDefault { get; set; } = true;
        public bool BackupBeforeSyncDefault { get; set; } = true;
        public bool SyncFilesDefault { get; set; } = true;
    }

    /// <summary>
    /// Model upload file SQL manual pada Host Induk (Legacy &amp; Fallback)
    /// </summary>
    public class SyncSqlUploadRequest
    {
        [Required(ErrorMessage = "File SQL wajib dipilih.")]
        public IFormFile? SqlFile { get; set; }

        public bool CleanBeforeSync { get; set; } = true;

        public bool BackupBeforeSync { get; set; } = true;
    }

    /// <summary>
    /// Model upload paket sinkronisasi lengkap (.zip atau .sql) pada Host Induk
    /// </summary>
    public class SyncPackageUploadRequest
    {
        [Required(ErrorMessage = "Paket Sinkronisasi (.zip atau .sql) wajib dipilih.")]
        public IFormFile? PackageFile { get; set; }

        public bool CleanBeforeSync { get; set; } = true;

        public bool BackupBeforeSync { get; set; } = true;
    }
}
