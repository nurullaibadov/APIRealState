using Microsoft.IdentityModel.Tokens;
using Org.BouncyCastle.Crypto.Generators;
using RealEstateAPI.Domain.Entities;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace RealEstateAPI.Infrastructure.Helpers
{
    public class JwtHelper
    {
        private readonly string _secretKey;
        private readonly string _issuer;
        private readonly string _audience;
        private readonly int _expirationInMinutes;

        public JwtHelper(string secretKey, string issuer, string audience, int expirationInMinutes)
        {
            _secretKey = secretKey;
            _issuer = issuer;
            _audience = audience;
            _expirationInMinutes = expirationInMinutes;
        }

        /// <summary>
        /// JWT Token oluştur
        /// </summary>
        public string GenerateToken(User user, bool rememberMe = false)
        {
            // Security key oluştur
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secretKey));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            // Claims (Token içindeki veriler)
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Name, $"{user.FirstName} {user.LastName}"),
                new Claim(ClaimTypes.Role, user.Role.ToString()),
                new Claim("EmailVerified", user.IsEmailVerified.ToString())
            };

            // Token geçerlilik süresi
            var expiration = rememberMe
                ? DateTime.UtcNow.AddDays(30)  // Remember me: 30 gün
                : DateTime.UtcNow.AddMinutes(_expirationInMinutes); // Normal: 1 gün

            // Token oluştur
            var token = new JwtSecurityToken(
                issuer: _issuer,
                audience: _audience,
                claims: claims,
                expires: expiration,
                signingCredentials: credentials
            );

            // String'e çevir
            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        /// <summary>
        /// Token'dan userId çıkar
        /// </summary>
        public int? GetUserIdFromToken(string token)
        {
            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                var jwtToken = tokenHandler.ReadJwtToken(token);

                var userIdClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier);
                if (userIdClaim != null && int.TryParse(userIdClaim.Value, out int userId))
                {
                    return userId;
                }
            }
            catch
            {
                // Token invalid
            }

            return null;
        }

        /// <summary>
        /// Token'ı validate et
        /// </summary>
        public bool ValidateToken(string token)
        {
            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secretKey));

                var validationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = _issuer,
                    ValidAudience = _audience,
                    IssuerSigningKey = securityKey,
                    ClockSkew = TimeSpan.Zero // Token tam süresinde expire olsun
                };

                tokenHandler.ValidateToken(token, validationParameters, out _);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }

    /// <summary>
    /// Password Hashing Helper - BCrypt kullanarak şifre hash'leme
    /// </summary>
    public static class PasswordHelper
    {
        /// <summary>
        /// Şifreyi hash'le
        /// </summary>
        public static string HashPassword(string password)
        {
            // BCrypt ile hash (workFactor: 11 - güvenlik seviyesi)
            return BCrypt.Net.BCrypt.HashPassword(password, workFactor: 11);
        }

        /// <summary>
        /// Şifreyi doğrula
        /// </summary>
        public static bool VerifyPassword(string password, string hashedPassword)
        {
            try
            {
                // BCrypt verify (constant time comparison - timing attack'a karşı güvenli)
                return BCrypt.Net.BCrypt.Verify(password, hashedPassword);
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Random token oluştur (email verification, password reset için)
        /// </summary>
        public static string GenerateRandomToken()
        {
            // Güvenli random string oluştur (32 byte = 256 bit)
            return Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32))
                .Replace("+", "")
                .Replace("/", "")
                .Replace("=", "");
        }
    }
}
