namespace GestionaleRistorante.Core.Interfaces.Repositories;

public interface IRepository<T>
    where T : class
{
    IQueryable<T> Query();

    Task<T?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task AddAsync(T entity, CancellationToken cancellationToken = default);

    void Update(T entity);

    void Remove(T entity);
}
