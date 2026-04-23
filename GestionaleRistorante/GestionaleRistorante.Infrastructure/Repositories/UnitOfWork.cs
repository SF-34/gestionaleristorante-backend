using GestionaleRistorante.Core.Interfaces.Repositories;
using GestionaleRistorante.Infrastructure.Data;

namespace GestionaleRistorante.Infrastructure.Repositories;

public sealed class UnitOfWork(AppDbContext dbContext) : IUnitOfWork
{
    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return dbContext.SaveChangesAsync(cancellationToken);
    }
}
