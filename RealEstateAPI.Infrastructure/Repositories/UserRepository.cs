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
    public class UserRepository : GenericRepository<User>, IUserRepository
    {
        public UserRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<User?> GetByEmailAsync(string email)
        {
            return await _dbSet
                .FirstOrDefaultAsync(u => u.Email.ToLower() == email.ToLower());
            // ToLower: Case-insensitive arama (TEST@test.com = test@test.com)
        }

        public async Task<User?> GetByEmailVerificationTokenAsync(string token)
        {
            return await _dbSet
                .FirstOrDefaultAsync(u => u.EmailVerificationToken == token);
        }

        public async Task<User?> GetByPasswordResetTokenAsync(string token)
        {
            return await _dbSet
                .FirstOrDefaultAsync(u =>
                    u.PasswordResetToken == token &&
                    u.PasswordResetTokenExpiry > DateTime.UtcNow);
            // Token geçerlilik süresi dolmamış olmalı
        }

        public async Task<bool> IsEmailExistsAsync(string email)
        {
            return await _dbSet
                .AnyAsync(u => u.Email.ToLower() == email.ToLower());
        }

        public async Task<IEnumerable<User>> GetUsersByRoleAsync(UserRole role)
        {
            return await _dbSet
                .Where(u => u.Role == role)
                .OrderBy(u => u.FirstName)
                .ToListAsync();
        }

        public async Task<IEnumerable<User>> GetUnverifiedUsersAsync()
        {
            return await _dbSet
                .Where(u => !u.IsEmailVerified)
                .OrderByDescending(u => u.CreatedAt)
                .ToListAsync();
        }
    }
}
