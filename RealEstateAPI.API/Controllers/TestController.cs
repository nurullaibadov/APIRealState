using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using RealEstateAPI.Domain.Entities;
using RealEstateAPI.Domain.Enums;
using RealEstateAPI.Domain.Interfaces.Repositories;

namespace RealEstateAPI.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TestController : ControllerBase
    {
        private readonly IUnitOfWork _unitOfWork;

        public TestController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        /// <summary>
        /// Test 1: Örnek kullanıcı oluştur
        /// GET: api/test/create-user
        /// </summary>
        [HttpGet("create-user")]
        public async Task<IActionResult> CreateUser()
        {
            try
            {
                // Email zaten var mı kontrol et
                var existingUser = await _unitOfWork.Users.GetByEmailAsync("test@test.com");
                if (existingUser != null)
                {
                    return Ok(new { message = "User already exists", user = existingUser });
                }

                // Yeni kullanıcı oluştur
                var user = new User
                {
                    FirstName = "Test",
                    LastName = "User",
                    Email = "test@test.com",
                    PasswordHash = "HashedPassword123", // Gerçekte BCrypt ile hash'lenecek
                    PhoneNumber = "+905551234567",
                    Role = UserRole.User,
                    IsEmailVerified = true
                };

                // Database'e ekle
                await _unitOfWork.Users.AddAsync(user);
                await _unitOfWork.SaveChangesAsync();

                return Ok(new
                {
                    message = "User created successfully",
                    userId = user.Id,
                    user = user
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        /// <summary>
        /// Test 2: Tüm kullanıcıları getir
        /// GET: api/test/users
        /// </summary>
        [HttpGet("users")]
        public async Task<IActionResult> GetAllUsers()
        {
            try
            {
                var users = await _unitOfWork.Users.GetAllAsync();
                return Ok(new
                {
                    count = users.Count(),
                    users = users
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        /// <summary>
        /// Test 3: Örnek mülk oluştur
        /// GET: api/test/create-property
        /// </summary>
        [HttpGet("create-property")]
        public async Task<IActionResult> CreateProperty()
        {
            try
            {
                // İlk kullanıcıyı al
                var users = await _unitOfWork.Users.GetAllAsync();
                var user = users.FirstOrDefault();

                if (user == null)
                {
                    return BadRequest(new { error = "No users found. Create a user first." });
                }

                // Yeni mülk oluştur
                var property = new Property
                {
                    Title = "Deniz Manzaralı 3+1 Lüks Daire",
                    Description = "Merkezi konumda, deniz manzaralı, yeni yapılı lüks daire. Havuzlu site içinde.",
                    Type = PropertyType.Apartment,
                    Status = PropertyStatus.ForSale,
                    Price = 5000000,
                    Currency = "TRY",
                    Area = 150,
                    BedRooms = 3,
                    BathRooms = 2,
                    LivingRooms = 1,
                    Floor = 5,
                    TotalFloor = 10,
                    BuildYear = 2023,
                    HasBalcony = true,
                    HasElevator = true,
                    HasParking = true,
                    IsFurnished = false,
                    Country = "Turkey",
                    City = "İstanbul",
                    District = "Kadıköy",
                    Neighborhood = "Acıbadem",
                    Address = "Acıbadem Mahallesi, Çeçen Sokak No:5 Daire:12",
                    PostalCode = "34718",
                    Latitude = 41.0082m,
                    Longitude = 28.9784m,
                    IsPublished = true,
                    IsFeatured = true,
                    UserId = user.Id
                };

                // Database'e ekle
                await _unitOfWork.Properties.AddAsync(property);
                await _unitOfWork.SaveChangesAsync();

                return Ok(new
                {
                    message = "Property created successfully",
                    propertyId = property.Id,
                    property = property
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        /// <summary>
        /// Test 4: Tüm mülkleri getir
        /// GET: api/test/properties
        /// </summary>
        [HttpGet("properties")]
        public async Task<IActionResult> GetAllProperties()
        {
            try
            {
                var properties = await _unitOfWork.Properties.GetAllAsync();
                return Ok(new
                {
                    count = properties.Count(),
                    properties = properties
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        /// <summary>
        /// Test 5: Transaction testi
        /// GET: api/test/transaction-test
        /// </summary>
        [HttpGet("transaction-test")]
        public async Task<IActionResult> TransactionTest()
        {
            try
            {
                await _unitOfWork.BeginTransactionAsync();

                // User oluştur
                var user = new User
                {
                    FirstName = "Transaction",
                    LastName = "Test",
                    Email = $"transaction{DateTime.UtcNow.Ticks}@test.com",
                    PasswordHash = "HashedPassword",
                    Role = UserRole.User,
                    IsEmailVerified = true
                };

                await _unitOfWork.Users.AddAsync(user);
                await _unitOfWork.SaveChangesAsync();

                // Property oluştur
                var property = new Property
                {
                    Title = "Transaction Test Property",
                    Description = "Test",
                    Type = PropertyType.Apartment,
                    Status = PropertyStatus.ForSale,
                    Price = 1000000,
                    Area = 100,
                    BedRooms = 2,
                    BathRooms = 1,
                    City = "İstanbul",
                    District = "Test",
                    Address = "Test Address",
                    UserId = user.Id
                };

                await _unitOfWork.Properties.AddAsync(property);
                await _unitOfWork.SaveChangesAsync();

                // Transaction'ı commit et
                await _unitOfWork.CommitTransactionAsync();

                return Ok(new
                {
                    message = "Transaction successful",
                    userId = user.Id,
                    propertyId = property.Id
                });
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync();
                return BadRequest(new { error = ex.Message });
            }
        }

        /// <summary>
        /// Test 6: Database bağlantısını kontrol et
        /// GET: api/test/health
        /// </summary>
        [HttpGet("health")]
        public async Task<IActionResult> HealthCheck()
        {
            try
            {
                // Basit bir count sorgusu çalıştır
                var userCount = await _unitOfWork.Users.CountAsync();
                var propertyCount = await _unitOfWork.Properties.CountAsync();

                return Ok(new
                {
                    status = "Healthy",
                    database = "Connected",
                    userCount = userCount,
                    propertyCount = propertyCount,
                    timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    status = "Unhealthy",
                    database = "Disconnected",
                    error = ex.Message
                });
            }
        }
    }
}
