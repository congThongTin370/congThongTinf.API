
using congThongTinf.Domain.Entities;

public interface IUnitOfWork : IDisposable
{
    IGenericRepository<User> Users { get; }

    IGenericRepository<Role> Roles { get; }

    Task<int> SaveChangesAsync();
}