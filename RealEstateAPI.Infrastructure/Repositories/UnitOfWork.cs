using Microsoft.EntityFrameworkCore.Storage;
using RealEstateAPI.Domain.Interfaces.Repositories;
using RealEstateAPI.Infrastructure.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RealEstateAPI.Infrastructure.Repositories
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly ApplicationDbContext _context;

        // Transaction için
        private IDbContextTransaction? _transaction;

        // Repository'ler (Lazy initialization)
        private IUserRepository? _users;
        private IPropertyRepository? _properties;
        private IPropertyImageRepository? _propertyImages;
        private IFavoriteRepository? _favorites;
        private IPaymentRepository? _payments;
        private IContactMessageRepository? _contactMessages;

        /// <summary>
        /// Constructor - Dependency Injection ile DbContext alır
        /// </summary>
        public UnitOfWork(ApplicationDbContext context)
        {
            _context = context;
        }

        // ========== REPOSITORY PROPERTIES ==========
        // Lazy initialization: İlk erişimde oluşturulur

        public IUserRepository Users
        {
            get
            {
                // Null ise oluştur
                _users ??= new UserRepository(_context);
                return _users;
            }
        }

        public IPropertyRepository Properties
        {
            get
            {
                _properties ??= new PropertyRepository(_context);
                return _properties;
            }
        }

        public IPropertyImageRepository PropertyImages
        {
            get
            {
                _propertyImages ??= new PropertyImageRepository(_context);
                return _propertyImages;
            }
        }

        public IFavoriteRepository Favorites
        {
            get
            {
                _favorites ??= new FavoriteRepository(_context);
                return _favorites;
            }
        }

        public IPaymentRepository Payments
        {
            get
            {
                _payments ??= new PaymentRepository(_context);
                return _payments;
            }
        }

        public IContactMessageRepository ContactMessages
        {
            get
            {
                _contactMessages ??= new ContactMessageRepository(_context);
                return _contactMessages;
            }
        }

        // ========== TRANSACTION METHODS ==========

        /// <summary>
        /// Tüm değişiklikleri database'e kaydet
        /// </summary>
        public async Task<int> SaveChangesAsync()
        {
            // ChangeTracker'daki tüm değişiklikleri database'e yaz
            // SaveChangesAsync override'ında otomatik timestamp güncellenir
            return await _context.SaveChangesAsync(CancellationToken.None);
        }

        /// <summary>
        /// Transaction başlat
        /// </summary>
        public async Task BeginTransactionAsync()
        {
            // Zaten transaction başlatılmışsa hata verme
            if (_transaction != null)
            {
                throw new InvalidOperationException("Transaction already started.");
            }

            // Yeni transaction başlat
            _transaction = await _context.Database.BeginTransactionAsync();
        }

        /// <summary>
        /// Transaction'ı commit et (onayla)
        /// </summary>
        public async Task CommitTransactionAsync()
        {
            // Transaction yoksa hata ver
            if (_transaction == null)
            {
                throw new InvalidOperationException("No transaction to commit.");
            }

            try
            {
                // Değişiklikleri kaydet
                await SaveChangesAsync();

                // Transaction'ı commit et
                await _transaction.CommitAsync();
            }
            catch
            {
                // Hata olursa rollback
                await RollbackTransactionAsync();
                throw;
            }
            finally
            {
                // Transaction'ı dispose et
                await _transaction.DisposeAsync();
                _transaction = null;
            }
        }

        /// <summary>
        /// Transaction'ı rollback et (geri al)
        /// </summary>
        public async Task RollbackTransactionAsync()
        {
            // Transaction yoksa işlem yapma
            if (_transaction == null)
            {
                return;
            }

            try
            {
                // Transaction'ı geri al
                await _transaction.RollbackAsync();
            }
            finally
            {
                // Transaction'ı dispose et
                await _transaction.DisposeAsync();
                _transaction = null;
            }
        }

        // ========== DISPOSE ==========

        /// <summary>
        /// Kaynakları temizle (DbContext, Transaction)
        /// </summary>
        public void Dispose()
        {
            // Transaction dispose et
            _transaction?.Dispose();

            // DbContext dispose et
            _context.Dispose();
        }
    }
}
