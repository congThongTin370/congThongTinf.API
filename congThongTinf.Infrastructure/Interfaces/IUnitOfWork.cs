
using congThongTinf.Domain.Entities;

public interface IUnitOfWork : IDisposable
{
    IGenericRepository<User> Users { get; }

    IGenericRepository<Role> Roles { get; }

    IGenericRepository<News> News { get; }

    IGenericRepository<Category> Categories { get; }

    IGenericRepository<Document> Documents { get; }

    IGenericRepository<Banner> Banners { get; }

    IGenericRepository<Leader> Leaders { get; }

    IGenericRepository<SharedFile> SharedFiles { get; }

    Task<int> SaveChangesAsync();
}