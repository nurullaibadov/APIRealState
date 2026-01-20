using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RealEstateAPI.Application.DTOs.Auth
{
    public class RegisterDto
    {
        /// <summary>
        /// Kullanıcı adı (zorunlu)
        /// </summary>
        [Required(ErrorMessage = "First name is required")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "First name must be between 2 and 100 characters")]
        public string FirstName { get; set; } = string.Empty;

        /// <summary>
        /// Kullanıcı soyadı (zorunlu)
        /// </summary>
        [Required(ErrorMessage = "Last name is required")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "Last name must be between 2 and 100 characters")]
        public string LastName { get; set; } = string.Empty;

        /// <summary>
        /// Email adresi (zorunlu, unique)
        /// </summary>
        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email address")]
        [StringLength(255)]
        public string Email { get; set; } = string.Empty;

        /// <summary>
        /// Şifre (zorunlu)
        /// Min 8 karakter, büyük harf, küçük harf, rakam
        /// </summary>
        [Required(ErrorMessage = "Password is required")]
        [StringLength(100, MinimumLength = 8, ErrorMessage = "Password must be at least 8 characters")]
        [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).{8,}$",
            ErrorMessage = "Password must contain at least one uppercase letter, one lowercase letter, and one number")]
        public string Password { get; set; } = string.Empty;

        /// <summary>
        /// Şifre tekrar (zorunlu, Password ile eşleşmeli)
        /// </summary>
        [Required(ErrorMessage = "Confirm password is required")]
        [Compare("Password", ErrorMessage = "Passwords do not match")]
        public string ConfirmPassword { get; set; } = string.Empty;

        /// <summary>
        /// Telefon numarası (opsiyonel)
        /// </summary>
        [Phone(ErrorMessage = "Invalid phone number")]
        [StringLength(20)]
        public string? PhoneNumber { get; set; }
    }
}
