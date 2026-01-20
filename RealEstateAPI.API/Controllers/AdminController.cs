using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using RealEstateAPI.Application.DTOs.Property;
using RealEstateAPI.Domain.Enums;
using RealEstateAPI.Domain.Interfaces.Repositories;

namespace RealEstateAPI.API.Controllers
{
    [Authorize(Roles = "Admin")]
    [Route("api/[controller]")]
    [ApiController]
    public class AdminController : ControllerBase
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public AdminController(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        // ============================================================
        // USER MANAGEMENT
        // ============================================================

        /// <summary>
        /// Tüm kullanıcıları getir
        /// GET: api/admin/users
        /// </summary>
        [HttpGet("users")]
        public async Task<ActionResult<List<UserDto>>> GetAllUsers()
        {
            try
            {
                var users = await _unitOfWork.Users.GetAllAsync();
                var userDtos = _mapper.Map<List<UserDto>>(users);
                return Ok(userDtos);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error retrieving users", error = ex.Message });
            }
        }

        /// <summary>
        /// Kullanıcı detayını getir
        /// GET: api/admin/users/{id}
        /// </summary>
        [HttpGet("users/{id}")]
        public async Task<ActionResult<UserDto>> GetUser(int id)
        {
            try
            {
                var user = await _unitOfWork.Users.GetByIdAsync(id);
                if (user == null)
                {
                    return NotFound(new { message = "User not found" });
                }

                var userDto = _mapper.Map<UserDto>(user);
                return Ok(userDto);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error retrieving user", error = ex.Message });
            }
        }

        /// <summary>
        /// Kullanıcı rolünü değiştir
        /// PUT: api/admin/users/{id}/change-role
        /// </summary>
        [HttpPut("users/{id}/change-role")]
        public async Task<IActionResult> ChangeUserRole(int id, [FromBody] ChangeRoleDto dto)
        {
            try
            {
                var user = await _unitOfWork.Users.GetByIdAsync(id);
                if (user == null)
                {
                    return NotFound(new { message = "User not found" });
                }

                user.Role = dto.Role;
                _unitOfWork.Users.Update(user);
                await _unitOfWork.SaveChangesAsync();

                return Ok(new { message = $"User role changed to {dto.Role}", userId = id });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error changing role", error = ex.Message });
            }
        }

        /// <summary>
        /// Kullanıcıyı sil (soft delete)
        /// DELETE: api/admin/users/{id}
        /// </summary>
        [HttpDelete("users/{id}")]
        public async Task<IActionResult> DeleteUser(int id)
        {
            try
            {
                var user = await _unitOfWork.Users.GetByIdAsync(id);
                if (user == null)
                {
                    return NotFound(new { message = "User not found" });
                }

                _unitOfWork.Users.Delete(user);
                await _unitOfWork.SaveChangesAsync();

                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error deleting user", error = ex.Message });
            }
        }

        // ============================================================
        // PROPERTY MANAGEMENT
        // ============================================================

        /// <summary>
        /// Onay bekleyen mülkleri getir
        /// GET: api/admin/properties/pending
        /// </summary>
        [HttpGet("properties/pending")]
        public async Task<ActionResult<List<PropertyDto>>> GetPendingProperties()
        {
            try
            {
                var properties = await _unitOfWork.Properties.GetAsync(p => !p.IsPublished);
                var propertyDtos = _mapper.Map<List<PropertyDto>>(properties);
                return Ok(propertyDtos);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error retrieving pending properties", error = ex.Message });
            }
        }

        /// <summary>
        /// Mülkü onayla (publish)
        /// PUT: api/admin/properties/{id}/approve
        /// </summary>
        [HttpPut("properties/{id}/approve")]
        public async Task<IActionResult> ApproveProperty(int id)
        {
            try
            {
                var property = await _unitOfWork.Properties.GetByIdAsync(id);
                if (property == null)
                {
                    return NotFound(new { message = "Property not found" });
                }

                property.IsPublished = true;
                _unitOfWork.Properties.Update(property);
                await _unitOfWork.SaveChangesAsync();

                return Ok(new { message = "Property approved", propertyId = id });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error approving property", error = ex.Message });
            }
        }

        /// <summary>
        /// Mülkü reddet (unpublish)
        /// PUT: api/admin/properties/{id}/reject
        /// </summary>
        [HttpPut("properties/{id}/reject")]
        public async Task<IActionResult> RejectProperty(int id)
        {
            try
            {
                var property = await _unitOfWork.Properties.GetByIdAsync(id);
                if (property == null)
                {
                    return NotFound(new { message = "Property not found" });
                }

                property.IsPublished = false;
                _unitOfWork.Properties.Update(property);
                await _unitOfWork.SaveChangesAsync();

                return Ok(new { message = "Property rejected", propertyId = id });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error rejecting property", error = ex.Message });
            }
        }

        /// <summary>
        /// Mülkü öne çıkar (feature)
        /// PUT: api/admin/properties/{id}/feature
        /// </summary>
        [HttpPut("properties/{id}/feature")]
        public async Task<IActionResult> FeatureProperty(int id, [FromBody] FeaturePropertyDto dto)
        {
            try
            {
                var property = await _unitOfWork.Properties.GetByIdAsync(id);
                if (property == null)
                {
                    return NotFound(new { message = "Property not found" });
                }

                property.IsFeatured = dto.IsFeatured;
                _unitOfWork.Properties.Update(property);
                await _unitOfWork.SaveChangesAsync();

                var message = dto.IsFeatured ? "Property featured" : "Property unfeatured";
                return Ok(new { message, propertyId = id });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error featuring property", error = ex.Message });
            }
        }

        /// <summary>
        /// Herhangi bir mülkü sil (admin yetkisi)
        /// DELETE: api/admin/properties/{id}
        /// </summary>
        [HttpDelete("properties/{id}")]
        public async Task<IActionResult> DeleteProperty(int id)
        {
            try
            {
                var property = await _unitOfWork.Properties.GetByIdAsync(id);
                if (property == null)
                {
                    return NotFound(new { message = "Property not found" });
                }

                _unitOfWork.Properties.Delete(property);
                await _unitOfWork.SaveChangesAsync();

                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error deleting property", error = ex.Message });
            }
        }

        // ============================================================
        // STATISTICS
        // ============================================================

        /// <summary>
        /// Dashboard istatistikleri
        /// GET: api/admin/statistics
        /// </summary>
        [HttpGet("statistics")]
        public async Task<IActionResult> GetStatistics()
        {
            try
            {
                var totalUsers = await _unitOfWork.Users.CountAsync();
                var totalProperties = await _unitOfWork.Properties.CountAsync();
                var pendingProperties = await _unitOfWork.Properties.CountAsync(p => !p.IsPublished);
                var totalPayments = await _unitOfWork.Payments.CountAsync();

                var statistics = new
                {
                    totalUsers,
                    totalProperties,
                    pendingProperties,
                    totalPayments,
                    generatedAt = DateTime.UtcNow
                };

                return Ok(statistics);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error retrieving statistics", error = ex.Message });
            }
        }
    }

    // ============================================================
    // DTOs
    // ============================================================

    public class ChangeRoleDto
    {
        public UserRole Role { get; set; }
    }

    public class FeaturePropertyDto
    {
        public bool IsFeatured { get; set; }
    }

}
