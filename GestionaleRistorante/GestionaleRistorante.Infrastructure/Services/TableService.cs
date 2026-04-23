using GestionaleRistorante.Core.Entities;
using GestionaleRistorante.Core.Exceptions;
using GestionaleRistorante.Core.Interfaces.Repositories;
using GestionaleRistorante.Core.Interfaces.Services;
using Microsoft.EntityFrameworkCore;

namespace GestionaleRistorante.Infrastructure.Services;

public sealed class TableService(
    IRepository<RestaurantTable> tableRepository,
    IUnitOfWork unitOfWork) : ITableService
{
    public async Task<IReadOnlyList<RestaurantTable>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await tableRepository.Query().OrderBy(x => x.Name).ToListAsync(cancellationToken);
    }

    public async Task<RestaurantTable> CreateAsync(RestaurantTable table, CancellationToken cancellationToken = default)
    {
        await tableRepository.AddAsync(table, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return table;
    }

    public async Task<RestaurantTable> UpdateAsync(int id, RestaurantTable table, CancellationToken cancellationToken = default)
    {
        var existing = await tableRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new AppException("TABLE_NOT_FOUND", "Table not found.", 404);

        existing.Name = table.Name;
        existing.Capacity = table.Capacity;
        existing.Status = table.Status;

        tableRepository.Update(existing);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return existing;
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var existing = await tableRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new AppException("TABLE_NOT_FOUND", "Table not found.", 404);

        existing.IsDeleted = true;
        tableRepository.Update(existing);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
