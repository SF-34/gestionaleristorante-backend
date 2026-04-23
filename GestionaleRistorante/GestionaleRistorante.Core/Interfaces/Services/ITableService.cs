using GestionaleRistorante.Core.Entities;

namespace GestionaleRistorante.Core.Interfaces.Services;

public interface ITableService
{
    Task<IReadOnlyList<RestaurantTable>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<RestaurantTable> CreateAsync(RestaurantTable table, CancellationToken cancellationToken = default);

    Task<RestaurantTable> UpdateAsync(int id, RestaurantTable table, CancellationToken cancellationToken = default);

    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
}
