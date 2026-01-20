using Microsoft.EntityFrameworkCore;
using RealEstateAPI.Domain.Entities;
using RealEstateAPI.Domain.Interfaces.Repositories;
using RealEstateAPI.Infrastructure.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RealEstateAPI.Infrastructure.Repositories
{
    public class FavoriteRepository : GenericRepository<Favorite>, IFavoriteRepository
    {
        public FavoriteRepository(ApplicationDbContext context) : base(context) { }

        public async Task<IEnumerable<Favorite>> GetFavoritesByUserIdAsync(int userId)
        {
            return await _dbSet
                .Include(f => f.Property)
                .ThenInclude(p => p.PropertyImages)
                .Where(f => f.UserId == userId)
                .OrderByDescending(f => f.CreatedAt)
                .ToListAsync();
        }

        public async Task<bool> IsFavoriteAsync(int userId, int propertyId)
        {
            return await _dbSet
                .AnyAsync(f => f.UserId == userId && f.PropertyId == propertyId);
        }

        public async Task<IEnumerable<Property>> GetFavoritePropertiesByUserIdAsync(int userId)
        {
            return await _dbSet
                .Include(f => f.Property)
                .ThenInclude(p => p.PropertyImages)
                .Where(f => f.UserId == userId)
                .Select(f => f.Property)
                .ToListAsync();
        }

        public async Task<bool> ToggleFavoriteAsync(int userId, int propertyId)
        {
            var favorite = await _dbSet
                .FirstOrDefaultAsync(f => f.UserId == userId && f.PropertyId == propertyId);

            if (favorite != null)
            {
                Delete(favorite);
                return false; // Çıkarıldı
            }
            else
            {
                await AddAsync(new Favorite { UserId = userId, PropertyId = propertyId });
                return true; // Eklendi
            }
        }
    }

}