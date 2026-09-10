using TrackerKerja.ViewModels;

namespace TrackerKerja.Services
{
    public class SyncExecutionResult
    {
        public bool Success { get; set; } = true;
        public int InsertedCount { get; set; }
        public int UpdatedCount { get; set; }
        public int UnchangedCount { get; set; }
        public int FailedCount { get; set; }
        public int TotalProcessed => InsertedCount + UpdatedCount + UnchangedCount + FailedCount;
        public string Message { get; set; } = string.Empty;
        public List<string> Errors { get; set; } = new();
        public List<int> AffectedTaskIds { get; set; } = new();
    }

    public interface IExcelSyncService
    {
        /// <summary>
        /// Mengambil path default file lokal di server untuk sinkronisasi cepat
        /// </summary>
        string GetDefaultLocalSyncPath();

        /// <summary>
        /// Membaca dan menganalisis (diff &amp; preview) file Excel dari Stream
        /// </summary>
        Task<ImportResultViewModel> ParseFromStreamAsync(
            Stream stream, 
            string fileName, 
            string sourceType = "Upload", 
            string? sourceUrl = null, 
            List<string>? specificSheets = null);

        /// <summary>
        /// Mengunduh dari link/URL (Google Sheets, OneDrive, direct link) lalu menganalisisnya.
        /// Jika gagal, melemparkan exception dengan pesan ramah agar diarahkan ke upload file.
        /// </summary>
        Task<ImportResultViewModel> ParseFromUrlAsync(string url);

        /// <summary>
        /// Membaca file lokal server (misal: "C:\Users\WAHANA 24\Downloads\Task Tracker - Update (1).xlsx") lalu menganalisisnya
        /// </summary>
        Task<ImportResultViewModel> ParseFromLocalPathAsync(string? localPath = null);

        /// <summary>
        /// Mengeksekusi sinkronisasi: update data yang sudah ada (Case 1) dan menambah task baru (Case 2)
        /// </summary>
        Task<SyncExecutionResult> ExecuteSyncAsync(
            ImportResultViewModel model, 
            Dictionary<int, string>? rowPicOverrides = null, 
            string? currentUserName = null);
    }
}
