using GestionaleRistorante.Core.Entities;
using GestionaleRistorante.Core.Models;

namespace GestionaleRistorante.Core.Interfaces.Services;

public interface IDishService
{
    Task<IReadOnlyList<Dish>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<Dish?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<Dish> CreateAsync(Dish dish, CancellationToken cancellationToken = default);

    Task<Dish> UpdateAsync(int id, Dish dish, CancellationToken cancellationToken = default);

    Task DeleteAsync(int id, CancellationToken cancellationToken = default);

    Task<string> UploadImageAsync(
        int id,
        Stream stream,
        string fileName,
        string contentType,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<DishIngredient>> GetRecipeAsync(int dishId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<DishIngredient>> ReplaceRecipeAsync(
        int dishId,
        IReadOnlyCollection<UpsertDishRecipeItemModel> items,
        CancellationToken cancellationToken = default);
}
