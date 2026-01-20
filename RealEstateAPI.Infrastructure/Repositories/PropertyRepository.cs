using Microsoft.EntityFrameworkCore;
using RealEstateAPI.Domain.Entities;
using RealEstateAPI.Domain.Enums;
using RealEstateAPI.Domain.Interfaces.Repositories;
using RealEstateAPI.Infrastructure.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RealEstateAPI.Infrastructure.Repositories
{
    public class PropertyRepository : GenericRepository<Property>, IPropertyRepository
    {
        public PropertyRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<IEnumerable<Property>> GetPropertiesByUserIdAsync(int userId)
        {
            return await _dbSet
                .Include(p => p.PropertyImages) // Resimleri de getir
                .Where(p => p.UserId == userId)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<Property>> GetPropertiesByCityAsync(string city)
        {
            return await _dbSet
                .Include(p => p.PropertyImages)
                .Where(p => p.City.ToLower() == city.ToLower() && p.IsPublished)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<Property>> GetPropertiesByTypeAndStatusAsync(
            PropertyType type,
            PropertyStatus status)
        {
            return await _dbSet
                .Include(p => p.PropertyImages)
                .Where(p => p.Type == type && p.Status == status && p.IsPublished)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<Property>> GetPropertiesByPriceRangeAsync(
            decimal minPrice,
            decimal maxPrice,
            string? city = null)
        {
            var query = _dbSet
                .Include(p => p.PropertyImages)
                .Where(p => p.Price >= minPrice && p.Price <= maxPrice && p.IsPublished);

            if (!string.IsNullOrWhiteSpace(city))
            {
                query = query.Where(p => p.City.ToLower() == city.ToLower());
            }

            return await query
                .OrderBy(p => p.Price)
                .ToListAsync();
        }

        public async Task<IEnumerable<Property>> GetFeaturedPropertiesAsync(int count = 10)
        {
            return await _dbSet
                .Include(p => p.PropertyImages)
                .Where(p => p.IsFeatured && p.IsPublished)
                .OrderByDescending(p => p.CreatedAt)
                .Take(count)
                .ToListAsync();
        }

        public async Task<IEnumerable<Property>> GetMostViewedPropertiesAsync(int count = 10)
        {
            return await _dbSet
                .Include(p => p.PropertyImages)
                .Where(p => p.IsPublished)
                .OrderByDescending(p => p.ViewCount)
                .Take(count)
                .ToListAsync();
        }

        public async Task<IEnumerable<Property>> GetLatestPropertiesAsync(int count = 10)
        {
            return await _dbSet
                .Include(p => p.PropertyImages)
                .Where(p => p.IsPublished)
                .OrderByDescending(p => p.CreatedAt)
                .Take(count)
                .ToListAsync();
        }

        public async Task IncrementViewCountAsync(int propertyId)
        {
            var property = await _dbSet.FindAsync(propertyId);
            if (property != null)
            {
                property.ViewCount++;
                // Update çağırmaya gerek yok, EF Core track ediyor
            }
        }

        public async Task<(IEnumerable<Property> Items, int TotalCount)> GetPublishedPropertiesAsync(
            int pageNumber,
            int pageSize,
            string? city = null,
            PropertyType? type = null,
            PropertyStatus? status = null,
            decimal? minPrice = null,
            decimal? maxPrice = null)
        {
            // Query oluştur
            var query = _dbSet
                .Include(p => p.PropertyImages)
                .Where(p => p.IsPublished);

            // Filtreleri uygula
            if (!string.IsNullOrWhiteSpace(city))
            {
                query = query.Where(p => p.City.ToLower() == city.ToLower());
            }

            if (type.HasValue)
            {
                query = query.Where(p => p.Type == type.Value);
            }

            if (status.HasValue)
            {
                query = query.Where(p => p.Status == status.Value);
            }

            if (minPrice.HasValue)
            {
                query = query.Where(p => p.Price >= minPrice.Value);
            }

            if (maxPrice.HasValue)
            {
                query = query.Where(p => p.Price <= maxPrice.Value);
            }

            // Toplam sayı
            var totalCount = await query.CountAsync();

            // Sayfalama
            var items = await query
                .OrderByDescending(p => p.CreatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (items, totalCount);
        }

        public async Task<IEnumerable<Property>> GetSimilarPropertiesAsync(
            int propertyId,
            int count = 5)
        {
            // Önce mevcut mülkü al
            var property = await _dbSet.FindAsync(propertyId);
            if (property == null) return Enumerable.Empty<Property>();

            // Fiyat aralığı hesapla (±20%)
            var minPrice = property.Price * 0.8m;
            var maxPrice = property.Price * 1.2m;

            return await _dbSet
                .Include(p => p.PropertyImages)
                .Where(p =>
                    p.Id != propertyId && // Kendisi hariç
                    p.City == property.City && // Aynı şehir
                    p.Type == property.Type && // Aynı tip
                    p.Price >= minPrice && // Benzer fiyat
                    p.Price <= maxPrice &&
                    p.IsPublished)
                .OrderBy(p => Math.Abs(p.Price - property.Price)) // Fiyata en yakın önce
                .Take(count)
                .ToListAsync();
        }
    }
}
