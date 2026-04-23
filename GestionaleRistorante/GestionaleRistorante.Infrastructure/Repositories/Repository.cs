using GestionaleRistorante.Core.Interfaces.Repositories;
using GestionaleRistorante.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace GestionaleRistorante.Infrastructure.Repositories;

public sealed class Repository<T>(AppDbContext dbContext) : IRepository<T>
    where T : class
{
    private readonly DbSet<T> _dbSet = dbContext.Set<T>();

    public IQueryable<T> Query()
    {
        return _dbSet.AsQueryable();
    }

    public Task<T?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return _dbSet.FirstOrDefaultAsync(x => EF.Property<int>(x, "Id") == id, cancellationToken);
    }

    public Task AddAsync(T entity, CancellationToken cancellationToken = default)
    {
        return _dbSet.AddAsync(entity, cancellationToken).AsTask();
    }

    public void Update(T entity)
    {
        _dbSet.Update(entity);
    }

    public void Remove(T entity)
    {
        _dbSet.Remove(entity);
    }
}
