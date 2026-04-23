using GestionaleRistorante.Core.Entities;

namespace GestionaleRistorante.Core.Interfaces.Services;

public interface IIngredientService
{
    Task<IReadOnlyList<Ingredient>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<Ingredient> CreateAsync(Ingredient ingredient, CancellationToken cancellationToken = default);

    Task<Ingredient> UpdateAsync(int id, Ingredient ingredient, CancellationToken cancellationToken = default);

    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
}
