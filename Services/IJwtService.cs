using System.Security.Claims;
using TrackerKerja.Models;

namespace TrackerKerja.Services
{
    /// <summary>
    /// Layanan pembuatan dan validasi token JWT untuk autentikasi API dan Web
    /// </summary>
    public interface IJwtService
    {
        /// <summary>
        /// Menghasilkan token JWT yang valid untuk pengguna dan daftar peran (roles)
        /// </summary>
        /// <param name="user">Entitas pengguna</param>
        /// <param name="roles">Daftar peran pengguna (misal: Admin, User)</param>
        /// <param name="expiresAt">Waktu kedaluwarsa token</param>
        /// <returns>String token JWT terenkripsi</returns>
        string GenerateToken(AppUser user, IList<string> roles, out DateTime expiresAt);

        /// <summary>
        /// Memvalidasi token JWT dan mengembalikan ClaimsPrincipal jika valid
        /// </summary>
        /// <param name="token">String token JWT</param>
        /// <returns>ClaimsPrincipal atau null jika tidak valid/kedaluwarsa</returns>
        ClaimsPrincipal? ValidateToken(string token);
    }
}
