using Microsoft.EntityFrameworkCore;
using RealEstateAPI.Domain.Interfaces.Repositories;
using RealEstateAPI.Infrastructure.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace RealEstateAPI.Infrastructure.Repositories
{
    public class GenericRepository<T> : IGenericRepository<T> where T :class
    {
        protected readonly ApplicationDbContext _context;

        // DbSet - T tipindeki entity'lerin tablosu
        protected readonly DbSet<T> _dbSet;

        /// <summary>
        /// Constructor - Dependency Injection ile DbContext alır
        /// </summary>
        public GenericRepository(ApplicationDbContext context)
        {
            _context = context;
            _dbSet = context.Set<T>(); // DbSet'i al
        }

        // ========== READ OPERATIONS ==========

        public virtual async Task<IEnumerable<T>> GetAllAsync()
        {
            // Tüm kayıtları getir
            // ToListAsync: SQL sorgusu execute edilir
            return await _dbSet.ToListAsync();
        }

        public virtual async Task<T?> GetByIdAsync(int id)
        {
            // Primary key ile kayıt bul
            // FindAsync: Cache'te varsa oradan döner (performans)
            return await _dbSet.FindAsync(id);
        }

        public virtual async Task<IEnumerable<T>> GetAsync(
            Expression<Func<T, bool>> predicate)
        {
            // Koşula uyan kayıtları getir
            // Where: Lambda expression'ı SQL WHERE'e çevirir
            return await _dbSet.Where(predicate).ToListAsync();
        }

        public virtual async Task<T?> GetFirstOrDefaultAsync(
            Expression<Func<T, bool>> predicate)
        {
            // Koşula uyan ilk kaydı getir, yoksa null
            return await _dbSet.FirstOrDefaultAsync(predicate);
        }

        public virtual async Task<bool> ExistsAsync(
            Expression<Func<T, bool>> predicate)
        {
            // Koşula uyan kayıt var mı?
            // AnyAsync: SELECT COUNT(*) > 0
            return await _dbSet.AnyAsync(predicate);
        }

        public virtual async Task<int> CountAsync()
        {
            // Toplam kayıt sayısı
            return await _dbSet.CountAsync();
        }

        public virtual async Task<int> CountAsync(
            Expression<Func<T, bool>> predicate)
        {
            // Koşula uyan kayıt sayısı
            return await _dbSet.CountAsync(predicate);
        }

        // ========== WRITE OPERATIONS ==========

        public virtual async Task AddAsync(T entity)
        {
            // Yeni kayıt ekle (henüz database'e yazılmadı)
            await _dbSet.AddAsync(entity);
        }

        public virtual async Task AddRangeAsync(IEnumerable<T> entities)
        {
            // Birden fazla kayıt ekle (bulk insert)
            await _dbSet.AddRangeAsync(entities);
        }

        public virtual void Update(T entity)
        {
            // Kaydı güncelle
            // Entry: Entity'nin state'ini değiştirir
            _dbSet.Update(entity);
        }

        public virtual void Delete(T entity)
        {
            // Kaydı sil (Soft delete BaseEntity'de yapılacak)
            _dbSet.Remove(entity);
        }

        public virtual async Task DeleteAsync(int id)
        {
            // ID ile sil
            var entity = await GetByIdAsync(id);
            if (entity != null)
            {
                Delete(entity);
            }
        }

        public virtual void DeleteRange(IEnumerable<T> entities)
        {
            // Birden fazla kaydı sil
            _dbSet.RemoveRange(entities);
        }

        // ========== ADVANCED QUERIES ==========

        public virtual async Task<IEnumerable<T>> GetWithIncludesAsync(
            Expression<Func<T, bool>>? predicate = null,
            params string[] includes)
        {
            // Query oluştur
            IQueryable<T> query = _dbSet;

            // Include'ları ekle (Eager loading)
            foreach (var include in includes)
            {
                query = query.Include(include);
            }

            // Filtre varsa uygula
            if (predicate != null)
            {
                query = query.Where(predicate);
            }

            return await query.ToListAsync();
        }

        public virtual async Task<(IEnumerable<T> Items, int TotalCount)> GetPagedAsync(
            int pageNumber,
            int pageSize,
            Expression<Func<T, bool>>? predicate = null,
            Func<IQueryable<T>, IOrderedQueryable<T>>? orderBy = null)
        {
            // Query oluştur
            IQueryable<T> query = _dbSet;

            // Filtre varsa uygula
            if (predicate != null)
            {
                query = query.Where(predicate);
            }

            // Toplam kayıt sayısı (filtrelenmiş)
            var totalCount = await query.CountAsync();

            // Sıralama varsa uygula
            if (orderBy != null)
            {
                query = orderBy(query);
            }

            // Sayfalama uygula
            // Skip: İlk N kaydı atla
            // Take: Sonraki M kaydı al
            var items = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (items, totalCount);
        }
    }
}
