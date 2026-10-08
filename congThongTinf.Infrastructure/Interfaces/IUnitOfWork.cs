
using congThongTinf.Domain.Entities;

public interface IUnitOfWork : IDisposable
{
    IGenericRepository<User> Users { get; }

    IGenericRepository<Role> Roles { get; }

    IGenericRepository<News> News { get; }

    Task<int> SaveChangesAsync();
}