

public interface IUnitOfWork : IDisposable
{
   

    Task<int> SaveChangesAsync();
}