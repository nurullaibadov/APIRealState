using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RealEstateAPI.Domain.Interfaces.Repositories
{
    public interface IUnitOfWork : IDisposable
    {
        IUserRepository Users { get; }
        IPropertyRepository Properties { get; }
        IPropertyImageRepository PropertyImages { get; }
        IFavoriteRepository Favorites { get;  } 
        IPaymentRepository Payments { get;  }
        IContactMessageRepository ContactMessages { get;  }
        Task<int> SaveChangesAsync();
        Task BeginTransactionAsync();
        Task CommitTransactionAsync();
        Task RollbackTransactionAsync();
    }
}
