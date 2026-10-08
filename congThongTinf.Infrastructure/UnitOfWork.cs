using congThongTinf.Domain;
using congThongTinf.Repository.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;


public class UnitOfWork : IUnitOfWork
{
    private IDbContextTransaction? _transaction;
    private readonly congThongTinfDbContext _dbContext;

    public UnitOfWork(
        congThongTinfDbContext dbContext,
        
        IDbContextTransaction? transaction = null
    )
    {
        _dbContext = dbContext;
        _transaction = transaction;
       
    }

    public void Dispose()
    {
        _dbContext.Dispose();
    }

    public async Task<int> SaveChangesAsync()
    {
        return await _dbContext.SaveChangesAsync();
    }

    // Transaction support
    public async Task BeginTransactionAsync()
    {
        if (_transaction != null) return;
        _transaction = await _dbContext.Database.BeginTransactionAsync();
    }

    public async Task CommitAsync()
    {
        try
        {
            if (_transaction != null)
            {
                await _dbContext.SaveChangesAsync();
                await _transaction.CommitAsync();
            }
        }
        finally
        {
            await DisposeTransactionAsync();
        }
    }

    public async Task RollbackAsync()
    {
        try
        {
            if (_transaction != null)
                await _transaction.RollbackAsync();
        }
        finally
        {
            await DisposeTransactionAsync();
        }
    }

    private async Task DisposeTransactionAsync()
    {
        if (_transaction != null)
        {
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    //public IGenericRepository<User> Users { get; }
  
}