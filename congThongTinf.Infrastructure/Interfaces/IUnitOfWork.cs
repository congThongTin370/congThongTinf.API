
using congThongTinf.Domain.Entities;

public interface IUnitOfWork : IDisposable
{
    IGenericRepository<User> Users { get; }

    IGenericRepository<Role> Roles { get; }

    IGenericRepository<News> News { get; }

    IGenericRepository<Category> Categories { get; }

    Task<int> SaveChangesAsync();
}