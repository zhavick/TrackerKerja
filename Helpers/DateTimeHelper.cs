using System;

namespace TrackerKerja.Helpers
{
    /// <summary>
    /// Helper for handling date and time in GMT+7 (WIB - Waktu Indonesia Barat).
    /// Works consistently across Windows (SE Asia Standard Time), Linux/macOS (Asia/Jakarta), and Docker containers.
    /// </summary>
    public static class DateTimeHelper
    {
        private static readonly TimeZoneInfo WibZone;

        static DateTimeHelper()
        {
            try
            {
                // Try Windows TimeZone ID
                WibZone = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
            }
            catch
            {
                try
                {
                    // Try IANA TimeZone ID (Linux, macOS, Docker)
                    WibZone = TimeZoneInfo.FindSystemTimeZoneById("Asia/Jakarta");
                }
                catch
                {
                    // Fallback to custom UTC+7 TimeZone
                    WibZone = TimeZoneInfo.CreateCustomTimeZone("WIB", TimeSpan.FromHours(7), "WIB (UTC+7)", "WIB");
                }
            }
        }

        /// <summary>
        /// Gets the TimeZoneInfo for GMT+7 (WIB).
        /// </summary>
        public static TimeZoneInfo TimeZone => WibZone;

        /// <summary>
        /// Gets current date and time in GMT+7 (WIB).
        /// </summary>
        public static DateTime Now => TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, WibZone);

        /// <summary>
        /// Gets current date (00:00:00) in GMT+7 (WIB).
        /// </summary>
        public static DateTime Today => Now.Date;

        /// <summary>
        /// Converts any DateTime to GMT+7 (WIB).
        /// </summary>
        public static DateTime ToGmt7(DateTime dt)
        {
            if (dt.Kind == DateTimeKind.Utc)
            {
                return TimeZoneInfo.ConvertTimeFromUtc(dt, WibZone);
            }
            else if (dt.Kind == DateTimeKind.Local)
            {
                return TimeZoneInfo.ConvertTime(dt, WibZone);
            }
            else
            {
                // Unspecified kind - assume it was recorded as UTC or convert assuming UTC
                return TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(dt, DateTimeKind.Utc), WibZone);
            }
        }

        /// <summary>
        /// Gets human-readable relative time string (e.g. "Baru saja", "5 menit yang lalu").
        /// </summary>
        public static string TimeAgo(DateTime dt)
        {
            var span = DateTime.Now - dt;
            if (span.TotalSeconds < 60) return "Baru saja";
            if (span.TotalMinutes < 60) return $"{(int)span.TotalMinutes} menit yang lalu";
            if (span.TotalHours < 24) return $"{(int)span.TotalHours} jam yang lalu";
            if (span.TotalDays < 30) return $"{(int)span.TotalDays} hari yang lalu";
            if (span.TotalDays < 365) return $"{(int)(span.TotalDays / 30)} bulan yang lalu";
            return $"{(int)(span.TotalDays / 365)} tahun yang lalu";
        }
    }
}
