using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using RealEstateAPI.Domain.Entities;
using RealEstateAPI.Domain.Interfaces.Repositories;
using RealEstateAPI.Infrastructure.Services;
using System.Security.Claims;

namespace RealEstateAPI.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ContactController : ControllerBase
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IEmailService _emailService;

        public ContactController(IUnitOfWork unitOfWork, IEmailService emailService)
        {
            _unitOfWork = unitOfWork;
            _emailService = emailService;
        }

        /// <summary>
        /// İletişim mesajı gönder (Public - misafir kullanıcılar da gönderebilir)
        /// POST: api/contact
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> SendContactMessage([FromBody] ContactMessageDto dto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                // Kullanıcı login olmuş mu kontrol et (opsiyonel)
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                int? userId = null;
                if (!string.IsNullOrEmpty(userIdClaim) && int.TryParse(userIdClaim, out int uid))
                {
                    userId = uid;
                }

                // ContactMessage oluştur
                var message = new ContactMessage
                {
                    UserId = userId,
                    PropertyId = dto.PropertyId,
                    Name = dto.Name,
                    Email = dto.Email,
                    PhoneNumber = dto.PhoneNumber,
                    Subject = dto.Subject,
                    Message = dto.Message
                };

                await _unitOfWork.ContactMessages.AddAsync(message);
                await _unitOfWork.SaveChangesAsync();

                // Admin'e email bildirim gönder
                try
                {
                    // TODO: Admin email'i config'den al
                    await _emailService.SendContactMessageNotificationAsync(
                        "admin@realestate.com",
                        dto.Name,
                        dto.Email,
                        dto.Message
                    );
                }
                catch
                {
                    // Email gönderimi başarısız olsa bile mesaj kaydedildi
                }

                return Ok(new
                {
                    success = true,
                    message = "Your message has been sent successfully. We will contact you soon.",
                    messageId = message.Id
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error sending message", error = ex.Message });
            }
        }

        /// <summary>
        /// Kullanıcının mesajlarını getir
        /// GET: api/contact/my-messages
        /// </summary>
        [Authorize]
        [HttpGet("my-messages")]
        public async Task<IActionResult> GetMyMessages()
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
                {
                    return Unauthorized(new { message = "Invalid token" });
                }

                var messages = await _unitOfWork.ContactMessages.GetMessagesByUserIdAsync(userId);
                return Ok(messages);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error retrieving messages", error = ex.Message });
            }
        }

        /// <summary>
        /// Okunmamış mesajları getir (Admin)
        /// GET: api/contact/unread
        /// </summary>
        [Authorize(Roles = "Admin")]
        [HttpGet("unread")]
        public async Task<IActionResult> GetUnreadMessages()
        {
            try
            {
                var messages = await _unitOfWork.ContactMessages.GetUnreadMessageAsync();
                return Ok(messages);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error retrieving messages", error = ex.Message });
            }
        }

        /// <summary>
        /// Mesajı okundu olarak işaretle (Admin)
        /// PUT: api/contact/{id}/mark-read
        /// </summary>
        [Authorize(Roles = "Admin")]
        [HttpPut("{id}/mark-read")]
        public async Task<IActionResult> MarkAsRead(int id)
        {
            try
            {
                await _unitOfWork.ContactMessages.MarkAsReadAsync(id);
                await _unitOfWork.SaveChangesAsync();

                return Ok(new { message = "Message marked as read" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error marking message", error = ex.Message });
            }
        }

        /// <summary>
        /// Mesaja cevap ver (Admin)
        /// POST: api/contact/{id}/reply
        /// </summary>
        [Authorize(Roles = "Admin")]
        [HttpPost("{id}/reply")]
        public async Task<IActionResult> ReplyToMessage(int id, [FromBody] ReplyMessageDto dto)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
                {
                    return Unauthorized(new { message = "Invalid token" });
                }

                await _unitOfWork.ContactMessages.ReplyToMessageAsync(id, dto.ReplyMessage, userId);
                await _unitOfWork.SaveChangesAsync();

                // Kullanıcıya email gönder
                var message = await _unitOfWork.ContactMessages.GetByIdAsync(id);
                if (message != null)
                {
                    try
                    {
                        await _emailService.SendEmailAsync(
                            message.Email,
                            $"Re: {message.Subject}",
                            dto.ReplyMessage
                        );
                    }
                    catch
                    {
                        // Email hatası
                    }
                }

                return Ok(new { message = "Reply sent successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error replying to message", error = ex.Message });
            }
        }
    }

    // ============================================================
    // DTOs
    // ============================================================

    public class ContactMessageDto
    {
        [System.ComponentModel.DataAnnotations.Required]
        [System.ComponentModel.DataAnnotations.StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [System.ComponentModel.DataAnnotations.Required]
        [System.ComponentModel.DataAnnotations.EmailAddress]
        public string Email { get; set; } = string.Empty;

        [System.ComponentModel.DataAnnotations.Phone]
        public string? PhoneNumber { get; set; }

        [System.ComponentModel.DataAnnotations.Required]
        [System.ComponentModel.DataAnnotations.StringLength(200)]
        public string Subject { get; set; } = string.Empty;

        [System.ComponentModel.DataAnnotations.Required]
        [System.ComponentModel.DataAnnotations.StringLength(2000)]
        public string Message { get; set; } = string.Empty;

        public int? PropertyId { get; set; }
    }

    public class ReplyMessageDto
    {
        [System.ComponentModel.DataAnnotations.Required]
        public string ReplyMessage { get; set; } = string.Empty;
    }

}
