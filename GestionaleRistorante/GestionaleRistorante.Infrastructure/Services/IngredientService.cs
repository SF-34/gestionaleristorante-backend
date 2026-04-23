using GestionaleRistorante.Core.Entities;
using GestionaleRistorante.Core.Exceptions;
using GestionaleRistorante.Core.Interfaces.Repositories;
using GestionaleRistorante.Core.Interfaces.Services;
using Microsoft.EntityFrameworkCore;

namespace GestionaleRistorante.Infrastructure.Services;

public sealed class IngredientService(
    IRepository<Ingredient> ingredientRepository,
    IUnitOfWork unitOfWork) : IIngredientService
{
    public async Task<IReadOnlyList<Ingredient>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await ingredientRepository.Query().OrderBy(x => x.Name).ToListAsync(cancellationToken);
    }

    public async Task<Ingredient> CreateAsync(Ingredient ingredient, CancellationToken cancellationToken = default)
    {
        var exists = await ingredientRepository.Query()
            .AnyAsync(x => x.Name == ingredient.Name, cancellationToken);
        if (exists)
        {
            throw new AppException("INGREDIENT_ALREADY_EXISTS", "Ingredient already exists.", 409);
        }

        await ingredientRepository.AddAsync(ingredient, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return ingredient;
    }

    public async Task<Ingredient> UpdateAsync(int id, Ingredient ingredient, CancellationToken cancellationToken = default)
    {
        var existing = await ingredientRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new AppException("INGREDIENT_NOT_FOUND", "Ingredient not found.", 404);

        existing.Name = ingredient.Name;
        existing.QuantityInStock = ingredient.QuantityInStock;
        existing.Unit = ingredient.Unit;

        ingredientRepository.Update(existing);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return existing;
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var existing = await ingredientRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new AppException("INGREDIENT_NOT_FOUND", "Ingredient not found.", 404);

        existing.IsDeleted = true;
        ingredientRepository.Update(existing);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
