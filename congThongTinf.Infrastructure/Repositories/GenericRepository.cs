using System.Linq.Expressions;
using congThongTinf.Domain;
using congThongTinf.Domain.Entities;
using Microsoft.EntityFrameworkCore;

public class GenericRepository<TEntity> : IGenericRepository<TEntity> where TEntity : BaseEntity
{
    private readonly DbSet<TEntity> _dbSet;

    public GenericRepository(congThongTinfDbContext context)
    {
        _dbSet = context.Set<TEntity>();
    }

    public async Task<List<TEntity>> GetAllAsync(Expression<Func<TEntity, bool>> predicate = null,
        params Expression<Func<TEntity, object>>[] includes)
    {
        var query = ApplyIncludes(_dbSet.AsQueryable(), includes);
        if (predicate != null)
            query = query.Where(predicate);

        return await query.ToListAsync();
    }

    public async Task<TEntity?> GetByIdAsync(Guid id, params Expression<Func<TEntity, object>>[] includes)
    {
        var query = ApplyIncludes(_dbSet.AsQueryable(), includes);
        return await query.FirstOrDefaultAsync(e => e.Id == id);
    }

    public async Task<TEntity> AddAsync(TEntity entity)
    {
        var now = DateTime.UtcNow;
        if (entity.Id == Guid.Empty)
            entity.Id = Guid.NewGuid();

        entity.CreatedAt = now;
        entity.UpdatedAt = now;
        await _dbSet.AddAsync(entity);
        return entity;
    }

    public Task<bool> UpdateRange(List<TEntity> entities)
    {
        foreach (var entity in entities)
            entity.UpdatedAt = DateTime.UtcNow;

        _dbSet.UpdateRange(entities);
        return Task.FromResult(true);
    }

    public Task<bool> Update(TEntity entity)
    {
        entity.UpdatedAt = DateTime.UtcNow;
        _dbSet.Update(entity);
        return Task.FromResult(true);
    }

    public Task<bool> SoftRemoveRangeById(List<Guid> entitiesId)
    {
        throw new NotSupportedException("Soft delete is not supported.");
    }

    public Task<bool> SoftRemoveRange(List<TEntity> entities)
    {
        throw new NotSupportedException("Soft delete is not supported.");
    }

    public Task<bool> SoftRemove(TEntity entity)
    {
        throw new NotSupportedException("Soft delete is not supported.");
    }

    public async Task AddRangeAsync(List<TEntity> entities)
    {
        var now = DateTime.UtcNow;
        foreach (var entity in entities)
        {
            if (entity.Id == Guid.Empty)
                entity.Id = Guid.NewGuid();

            entity.CreatedAt = now;
            entity.UpdatedAt = now;
        }

        await _dbSet.AddRangeAsync(entities);
    }

    public IQueryable<TEntity> GetQueryable()
    {
        return _dbSet.AsQueryable();
    }

    public async Task<TEntity?> FirstOrDefaultAsync(Expression<Func<TEntity, bool>> predicate = null,
        params Expression<Func<TEntity, object>>[] includes)
    {
        var query = ApplyIncludes(_dbSet.AsQueryable(), includes);
        if (predicate != null)
            query = query.Where(predicate);

        return await query.FirstOrDefaultAsync();
    }

    public async Task<bool> HardRemoveRange(List<TEntity> entities)
    {
        _dbSet.RemoveRange(entities);
        return await Task.FromResult(true);
    }

    public async Task<bool> HardRemove(Expression<Func<TEntity, bool>> predicate)
    {
        var entities = await _dbSet.Where(predicate).ToListAsync();
        _dbSet.RemoveRange(entities);
        return true;
    }

    public Task<int> CountAsync(Expression<Func<TEntity, bool>> predicate)
    {
        return _dbSet.CountAsync(predicate);
    }

    private static IQueryable<TEntity> ApplyIncludes(IQueryable<TEntity> query,
        params Expression<Func<TEntity, object>>[] includes)
    {
        if (includes == null)
            return query;

        foreach (var include in includes)
            query = query.Include(include);

        return query;
    }
}
