using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RealEstateAPI.Application.DTOs.Auth
{
    public class LoginDto
    {
        /// <summary>
        /// Email adresi
        /// </summary>
        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email address")]
        public string Email { get; set; } = string.Empty;

        /// <summary>
        /// Şifre
        /// </summary>
        [Required(ErrorMessage = "Password is required")]
        public string Password { get; set; } = string.Empty;

        /// <summary>
        /// Beni hatırla (opsiyonel)
        /// true ise token süresi uzar
        /// </summary>
        public bool RememberMe { get; set; } = false;
    }
}
