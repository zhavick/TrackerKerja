using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace TrackerKerja.Models
{
    public class AppUser : IdentityUser
    {
        [MaxLength(150)]
        public string FullName { get; set; } = string.Empty;

        [MaxLength(100)]
        public string JobTitle { get; set; } = string.Empty;

        [MaxLength(7)]
        public string AvatarColor { get; set; } = "#6366F1";

        [MaxLength(300)]
        public string? ProfilePictureUrl { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Computed: initials from FullName
        public string Initials
        {
            get
            {
                if (string.IsNullOrWhiteSpace(FullName)) return "?";
                var parts = FullName.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length == 1) return parts[0][0].ToString().ToUpper();
                return $"{parts[0][0]}{parts[^1][0]}".ToUpper();
            }
        }
    }
}
