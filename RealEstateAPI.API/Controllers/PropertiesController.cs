using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using RealEstateAPI.Application.DTOs.Property;
using RealEstateAPI.Domain.Entities;
using RealEstateAPI.Domain.Interfaces.Repositories;
using RealEstateAPI.Infrastructure.Services;
using System.Security.Claims;

namespace RealEstateAPI.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PropertiesController : ControllerBase
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IFileService _fileService;

        public PropertiesController(
            IUnitOfWork unitOfWork,
            IMapper mapper,
            IFileService fileService)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _fileService = fileService;
        }

        // ============================================================
        // PUBLIC ENDPOINTS (Authentication gerekmez)
        // ============================================================

        /// <summary>
        /// Tüm mülkleri getir (filtreleme + pagination)
        /// GET: api/properties
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<PaginatedResponse<PropertyDto>>> GetProperties(
            [FromQuery] PropertyFilterDto filter)
        {
            try
            {
                var (items, totalCount) = await _unitOfWork.Properties.GetPublishedPropertiesAsync(
                    pageNumber: filter.PageNumber,
                    pageSize: filter.PageSize,
                    city: filter.City,
                    type: filter.Type,
                    status: filter.Status,
                    minPrice: filter.MinPrice,
                    maxPrice: filter.MaxPrice
                );

                var propertyDtos = _mapper.Map<List<PropertyDto>>(items);

                var response = new PaginatedResponse<PropertyDto>
                {
                    Items = propertyDtos,
                    TotalCount = totalCount,
                    PageNumber = filter.PageNumber,
                    PageSize = filter.PageSize
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error retrieving properties", error = ex.Message });
            }
        }

        /// <summary>
        /// Mülk detayını getir
        /// GET: api/properties/{id}
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<PropertyDto>> GetProperty(int id)
        {
            try
            {
                var property = await _unitOfWork.Properties.GetByIdAsync(id);

                if (property == null)
                {
                    return NotFound(new { message = "Property not found" });
                }

                // ViewCount artır
                await _unitOfWork.Properties.IncrementViewCountAsync(id);
                await _unitOfWork.SaveChangesAsync();

                var propertyDto = _mapper.Map<PropertyDto>(property);
                return Ok(propertyDto);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error retrieving property", error = ex.Message });
            }
        }

        /// <summary>
        /// Öne çıkan mülkleri getir
        /// GET: api/properties/featured
        /// </summary>
        [HttpGet("featured")]
        public async Task<ActionResult<List<PropertyDto>>> GetFeaturedProperties([FromQuery] int count = 10)
        {
            try
            {
                var properties = await _unitOfWork.Properties.GetFeaturedPropertiesAsync(count);
                var propertyDtos = _mapper.Map<List<PropertyDto>>(properties);
                return Ok(propertyDtos);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error retrieving featured properties", error = ex.Message });
            }
        }

        /// <summary>
        /// En çok görüntülenen mülkleri getir
        /// GET: api/properties/most-viewed
        /// </summary>
        [HttpGet("most-viewed")]
        public async Task<ActionResult<List<PropertyDto>>> GetMostViewedProperties([FromQuery] int count = 10)
        {
            try
            {
                var properties = await _unitOfWork.Properties.GetMostViewedPropertiesAsync(count);
                var propertyDtos = _mapper.Map<List<PropertyDto>>(properties);
                return Ok(propertyDtos);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error retrieving most viewed properties", error = ex.Message });
            }
        }

        /// <summary>
        /// Son eklenen mülkleri getir
        /// GET: api/properties/latest
        /// </summary>
        [HttpGet("latest")]
        public async Task<ActionResult<List<PropertyDto>>> GetLatestProperties([FromQuery] int count = 10)
        {
            try
            {
                var properties = await _unitOfWork.Properties.GetLatestPropertiesAsync(count);
                var propertyDtos = _mapper.Map<List<PropertyDto>>(properties);
                return Ok(propertyDtos);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error retrieving latest properties", error = ex.Message });
            }
        }

        /// <summary>
        /// Benzer mülkleri getir
        /// GET: api/properties/{id}/similar
        /// </summary>
        [HttpGet("{id}/similar")]
        public async Task<ActionResult<List<PropertyDto>>> GetSimilarProperties(int id, [FromQuery] int count = 5)
        {
            try
            {
                var properties = await _unitOfWork.Properties.GetSimilarPropertiesAsync(id, count);
                var propertyDtos = _mapper.Map<List<PropertyDto>>(properties);
                return Ok(propertyDtos);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error retrieving similar properties", error = ex.Message });
            }
        }

        // ============================================================
        // PROTECTED ENDPOINTS (Authentication gerekli)
        // ============================================================

        /// <summary>
        /// Yeni mülk oluştur
        /// POST: api/properties
        /// [Authorize] → JWT token gerekli
        /// </summary>
        [Authorize]
        [HttpPost]
        public async Task<ActionResult<PropertyDto>> CreateProperty([FromBody] PropertyCreateDto createDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                // Token'dan userId al
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
                {
                    return Unauthorized(new { message = "Invalid token" });
                }

                // DTO → Entity
                var property = _mapper.Map<Property>(createDto);
                property.UserId = userId;
                property.IsPublished = false; // Admin onayı bekliyor

                // Database'e ekle
                await _unitOfWork.Properties.AddAsync(property);
                await _unitOfWork.SaveChangesAsync();

                // Entity → DTO
                var propertyDto = _mapper.Map<PropertyDto>(property);
                return CreatedAtAction(nameof(GetProperty), new { id = property.Id }, propertyDto);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error creating property", error = ex.Message });
            }
        }

        /// <summary>
        /// Mülk güncelle
        /// PUT: api/properties/{id}
        /// Sadece mülk sahibi veya admin güncelleyebilir
        /// </summary>
        [Authorize]
        [HttpPut("{id}")]
        public async Task<ActionResult<PropertyDto>> UpdateProperty(int id, [FromBody] PropertyUpdateDto updateDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                // Mevcut mülkü al
                var property = await _unitOfWork.Properties.GetByIdAsync(id);
                if (property == null)
                {
                    return NotFound(new { message = "Property not found" });
                }

                // Yetki kontrolü
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var userRole = User.FindFirst(ClaimTypes.Role)?.Value;

                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
                {
                    return Unauthorized(new { message = "Invalid token" });
                }

                // Sadece mülk sahibi veya admin güncelleyebilir
                if (property.UserId != userId && userRole != "Admin")
                {
                    return Forbid(); // 403 Forbidden
                }

                // DTO → Entity (conditional mapping)
                _mapper.Map(updateDto, property);

                // Güncelle
                _unitOfWork.Properties.Update(property);
                await _unitOfWork.SaveChangesAsync();

                var propertyDto = _mapper.Map<PropertyDto>(property);
                return Ok(propertyDto);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error updating property", error = ex.Message });
            }
        }

        /// <summary>
        /// Mülk sil
        /// DELETE: api/properties/{id}
        /// Sadece mülk sahibi veya admin silebilir
        /// </summary>
        [Authorize]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteProperty(int id)
        {
            try
            {
                var property = await _unitOfWork.Properties.GetByIdAsync(id);
                if (property == null)
                {
                    return NotFound(new { message = "Property not found" });
                }

                // Yetki kontrolü
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var userRole = User.FindFirst(ClaimTypes.Role)?.Value;

                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
                {
                    return Unauthorized(new { message = "Invalid token" });
                }

                if (property.UserId != userId && userRole != "Admin")
                {
                    return Forbid();
                }

                // Soft delete
                _unitOfWork.Properties.Delete(property);
                await _unitOfWork.SaveChangesAsync();

                return NoContent(); // 204 No Content
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error deleting property", error = ex.Message });
            }
        }

        /// <summary>
        /// Kullanıcının mülklerini getir
        /// GET: api/properties/my-properties
        /// </summary>
        [Authorize]
        [HttpGet("my-properties")]
        public async Task<ActionResult<List<PropertyDto>>> GetMyProperties()
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
                {
                    return Unauthorized(new { message = "Invalid token" });
                }

                var properties = await _unitOfWork.Properties.GetPropertiesByUserIdAsync(userId);
                var propertyDtos = _mapper.Map<List<PropertyDto>>(properties);
                return Ok(propertyDtos);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error retrieving properties", error = ex.Message });
            }
        }

        // ============================================================
        // IMAGE UPLOAD
        // ============================================================

        /// <summary>
        /// Mülke resim yükle
        /// POST: api/properties/{id}/upload-images
        /// </summary>
        [Authorize]
        [HttpPost("{id}/upload-images")]
        public async Task<IActionResult> UploadImages(int id, [FromForm] List<IFormFile> files)
        {
            try
            {
                // Property kontrolü
                var property = await _unitOfWork.Properties.GetByIdAsync(id);
                if (property == null)
                {
                    return NotFound(new { message = "Property not found" });
                }

                // Yetki kontrolü
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
                {
                    return Unauthorized(new { message = "Invalid token" });
                }

                if (property.UserId != userId)
                {
                    return Forbid();
                }

                // Resimleri yükle
                var uploadedUrls = await _fileService.UploadMultipleImagesAsync(files, "properties");

                // PropertyImage kayıtları oluştur
                var displayOrder = await _unitOfWork.PropertyImages.CountAsync(i => i.PropertyId == id);

                foreach (var url in uploadedUrls)
                {
                    var image = new PropertyImage
                    {
                        PropertyId = id,
                        ImageUrl = url,
                        DisplayOrder = ++displayOrder,
                        IsCover = displayOrder == 1 // İlk resim cover olsun
                    };

                    await _unitOfWork.PropertyImages.AddAsync(image);
                }

                await _unitOfWork.SaveChangesAsync();

                return Ok(new { message = "Images uploaded successfully", urls = uploadedUrls });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error uploading images", error = ex.Message });
            }
        }
    }
}
