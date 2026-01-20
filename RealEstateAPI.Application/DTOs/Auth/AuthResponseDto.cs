using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RealEstateAPI.Application.DTOs.Auth
{
    public class AuthResponseDto
    {
        /// <summary>
        /// İşlem başarılı mı?
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Mesaj (başarı veya hata)
        /// </summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// JWT Token (başarılı login'de dolu)
        /// </summary>
        public string? Token { get; set; }

        /// <summary>
        /// Token geçerlilik süresi (UTC)
        /// </summary>
        public DateTime? TokenExpiry { get; set; }

        /// <summary>
        /// Kullanıcı bilgileri
        /// </summary>
        public UserDto? User { get; set; }

        /// <summary>
        /// Hata listesi (validation errors)
        /// </summary>
        public List<string>? Errors { get; set; }
    }
}
