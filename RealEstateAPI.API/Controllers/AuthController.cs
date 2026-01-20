using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using RealEstateAPI.Application.DTOs.Auth;
using RealEstateAPI.Domain.Entities;
using RealEstateAPI.Domain.Interfaces.Repositories;
using RealEstateAPI.Infrastructure.Helpers;

namespace RealEstateAPI.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly JwtHelper _jwtHelper;

        public AuthController(
            IUnitOfWork unitOfWork,
            IMapper mapper,
            JwtHelper jwtHelper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _jwtHelper = jwtHelper;
        }

        /// <summary>
        /// Kullanıcı kaydı
        /// POST: api/auth/register
        /// </summary>
        [HttpPost("register")]
        public async Task<ActionResult<AuthResponseDto>> Register([FromBody] RegisterDto registerDto)
        {
            try
            {
                // 1. Model validation (otomatik)
                if (!ModelState.IsValid)
                {
                    return BadRequest(new AuthResponseDto
                    {
                        Success = false,
                        Message = "Validation failed",
                        Errors = ModelState.Values
                            .SelectMany(v => v.Errors)
                            .Select(e => e.ErrorMessage)
                            .ToList()
                    });
                }

                // 2. Email zaten kayıtlı mı kontrol et
                var existingUser = await _unitOfWork.Users.GetByEmailAsync(registerDto.Email);
                if (existingUser != null)
                {
                    return BadRequest(new AuthResponseDto
                    {
                        Success = false,
                        Message = "Email already registered",
                        Errors = new List<string> { "This email address is already in use" }
                    });
                }

                // 3. DTO → Entity dönüşümü
                var user = _mapper.Map<User>(registerDto);

                // 4. Şifreyi hash'le
                user.PasswordHash = PasswordHelper.HashPassword(registerDto.Password);

                // 5. Email verification token oluştur
                user.EmailVerificationToken = PasswordHelper.GenerateRandomToken();
                user.IsEmailVerified = false; // Email doğrulanmamış

                // 6. Database'e kaydet
                await _unitOfWork.Users.AddAsync(user);
                await _unitOfWork.SaveChangesAsync();

                // 7. TODO: Email gönder (Email verification link)
                // await _emailService.SendVerificationEmailAsync(user.Email, user.EmailVerificationToken);

                // 8. JWT token oluştur
                var token = _jwtHelper.GenerateToken(user, rememberMe: false);

                // 9. Response oluştur
                var userDto = _mapper.Map<UserDto>(user);

                return Ok(new AuthResponseDto
                {
                    Success = true,
                    Message = "Registration successful. Please verify your email.",
                    Token = token,
                    TokenExpiry = DateTime.UtcNow.AddDays(1),
                    User = userDto
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new AuthResponseDto
                {
                    Success = false,
                    Message = "An error occurred during registration",
                    Errors = new List<string> { ex.Message }
                });
            }
        }

        /// <summary>
        /// Kullanıcı girişi
        /// POST: api/auth/login
        /// </summary>
        [HttpPost("login")]
        public async Task<ActionResult<AuthResponseDto>> Login([FromBody] LoginDto loginDto)
        {
            try
            {
                // 1. Model validation
                if (!ModelState.IsValid)
                {
                    return BadRequest(new AuthResponseDto
                    {
                        Success = false,
                        Message = "Validation failed",
                        Errors = ModelState.Values
                            .SelectMany(v => v.Errors)
                            .Select(e => e.ErrorMessage)
                            .ToList()
                    });
                }

                // 2. Email ile kullanıcıyı bul
                var user = await _unitOfWork.Users.GetByEmailAsync(loginDto.Email);
                if (user == null)
                {
                    return Unauthorized(new AuthResponseDto
                    {
                        Success = false,
                        Message = "Invalid email or password",
                        Errors = new List<string> { "The email or password you entered is incorrect" }
                    });
                }

                // 3. Şifre kontrolü
                if (!PasswordHelper.VerifyPassword(loginDto.Password, user.PasswordHash))
                {
                    return Unauthorized(new AuthResponseDto
                    {
                        Success = false,
                        Message = "Invalid email or password",
                        Errors = new List<string> { "The email or password you entered is incorrect" }
                    });
                }

                // 4. Son giriş tarihini güncelle
                user.LastLoginAt = DateTime.UtcNow;
                _unitOfWork.Users.Update(user);
                await _unitOfWork.SaveChangesAsync();

                // 5. JWT token oluştur
                var token = _jwtHelper.GenerateToken(user, loginDto.RememberMe);

                // 6. Response oluştur
                var userDto = _mapper.Map<UserDto>(user);

                var tokenExpiry = loginDto.RememberMe
                    ? DateTime.UtcNow.AddDays(30)
                    : DateTime.UtcNow.AddDays(1);

                return Ok(new AuthResponseDto
                {
                    Success = true,
                    Message = "Login successful",
                    Token = token,
                    TokenExpiry = tokenExpiry,
                    User = userDto
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new AuthResponseDto
                {
                    Success = false,
                    Message = "An error occurred during login",
                    Errors = new List<string> { ex.Message }
                });
            }
        }

        /// <summary>
        /// Email doğrulama
        /// GET: api/auth/verify-email?token=xxx
        /// </summary>
        [HttpGet("verify-email")]
        public async Task<IActionResult> VerifyEmail([FromQuery] string token)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(token))
                {
                    return BadRequest(new { success = false, message = "Token is required" });
                }

                // Token ile kullanıcıyı bul
                var user = await _unitOfWork.Users.GetByEmailVerificationTokenAsync(token);
                if (user == null)
                {
                    return BadRequest(new { success = false, message = "Invalid or expired token" });
                }

                // Email'i doğrula
                user.IsEmailVerified = true;
                user.EmailVerificationToken = null; // Token'ı temizle
                _unitOfWork.Users.Update(user);
                await _unitOfWork.SaveChangesAsync();

                return Ok(new { success = true, message = "Email verified successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Şifre sıfırlama isteği (Email gönder)
        /// POST: api/auth/forgot-password
        /// </summary>
        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordDto dto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                // Kullanıcıyı bul
                var user = await _unitOfWork.Users.GetByEmailAsync(dto.Email);
                if (user == null)
                {
                    // Güvenlik: Email'in kayıtlı olup olmadığını söyleme
                    return Ok(new { success = true, message = "If the email exists, a reset link has been sent" });
                }

                // Reset token oluştur
                user.PasswordResetToken = PasswordHelper.GenerateRandomToken();
                user.PasswordResetTokenExpiry = DateTime.UtcNow.AddHours(24); // 24 saat geçerli

                _unitOfWork.Users.Update(user);
                await _unitOfWork.SaveChangesAsync();

                // TODO: Email gönder (Password reset link)
                // await _emailService.SendPasswordResetEmailAsync(user.Email, user.PasswordResetToken);

                return Ok(new { success = true, message = "If the email exists, a reset link has been sent" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Şifre sıfırlama (Token ile)
        /// POST: api/auth/reset-password
        /// </summary>
        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto dto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                // Token ile kullanıcıyı bul (ve token expire olmamış olmalı)
                var user = await _unitOfWork.Users.GetByPasswordResetTokenAsync(dto.Token);
                if (user == null)
                {
                    return BadRequest(new { success = false, message = "Invalid or expired token" });
                }

                // Yeni şifreyi hash'le
                user.PasswordHash = PasswordHelper.HashPassword(dto.NewPassword);

                // Token'ı temizle
                user.PasswordResetToken = null;
                user.PasswordResetTokenExpiry = null;

                _unitOfWork.Users.Update(user);
                await _unitOfWork.SaveChangesAsync();

                return Ok(new { success = true, message = "Password reset successful" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Test endpoint - Token'ı test et
        /// GET: api/auth/test-token
        /// [Authorize] attribute ekleyince sadece login olmuş kullanıcılar erişebilir
        /// </summary>
        [HttpGet("test-token")]
        public IActionResult TestToken()
        {
            // Authorization header'ından token alınabilir
            var authHeader = Request.Headers["Authorization"].ToString();

            if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer "))
            {
                return Unauthorized(new { message = "No token provided" });
            }

            var token = authHeader.Substring("Bearer ".Length).Trim();

            // Token'ı validate et
            var isValid = _jwtHelper.ValidateToken(token);
            var userId = _jwtHelper.GetUserIdFromToken(token);

            return Ok(new
            {
                tokenValid = isValid,
                userId = userId,
                message = isValid ? "Token is valid" : "Token is invalid"
            });
        }
    }
}
