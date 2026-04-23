using GestionaleRistorante.Core.Entities;
using GestionaleRistorante.Core.Exceptions;
using GestionaleRistorante.Core.Interfaces.Repositories;
using GestionaleRistorante.Core.Interfaces.Services;
using GestionaleRistorante.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace GestionaleRistorante.Infrastructure.Services;

public sealed class DishService(
    IRepository<Dish> dishRepository,
    IRepository<DishIngredient> dishIngredientRepository,
    IRepository<Ingredient> ingredientRepository,
    IUnitOfWork unitOfWork,
    IBlobStorageService blobStorageService) : IDishService
{
    public async Task<IReadOnlyList<Dish>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await dishRepository.Query().OrderBy(x => x.Name).ToListAsync(cancellationToken);
    }

    public Task<Dish?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return dishRepository.GetByIdAsync(id, cancellationToken);
    }

    public async Task<Dish> CreateAsync(Dish dish, CancellationToken cancellationToken = default)
    {
        await dishRepository.AddAsync(dish, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return dish;
    }

    public async Task<Dish> UpdateAsync(int id, Dish dish, CancellationToken cancellationToken = default)
    {
        var existing = await dishRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new AppException("DISH_NOT_FOUND", "Dish not found.", 404);

        existing.Name = dish.Name;
        existing.Description = dish.Description;
        existing.Price = dish.Price;
        existing.IsAvailable = dish.IsAvailable;

        dishRepository.Update(existing);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return existing;
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var existing = await dishRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new AppException("DISH_NOT_FOUND", "Dish not found.", 404);

        existing.IsDeleted = true;
        dishRepository.Update(existing);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task<string> UploadImageAsync(
        int id,
        Stream stream,
        string fileName,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        var existing = await dishRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new AppException("DISH_NOT_FOUND", "Dish not found.", 404);

        var imageUrl = await blobStorageService.UploadDishImageAsync(stream, fileName, contentType, cancellationToken);
        existing.ImageUrl = imageUrl;

        dishRepository.Update(existing);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return imageUrl;
    }

    public async Task<IReadOnlyList<DishIngredient>> GetRecipeAsync(
        int dishId,
        CancellationToken cancellationToken = default)
    {
        var dishExists = await dishRepository.Query()
            .AnyAsync(x => x.Id == dishId, cancellationToken);
        if (!dishExists)
        {
            throw new AppException("DISH_NOT_FOUND", "Dish not found.", 404);
        }

        return await dishIngredientRepository.Query()
            .Where(x => x.DishId == dishId)
            .Include(x => x.Ingredient)
            .OrderBy(x => x.Ingredient.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<DishIngredient>> ReplaceRecipeAsync(
        int dishId,
        IReadOnlyCollection<UpsertDishRecipeItemModel> items,
        CancellationToken cancellationToken = default)
    {
        var dishExists = await dishRepository.Query()
            .AnyAsync(x => x.Id == dishId, cancellationToken);
        if (!dishExists)
        {
            throw new AppException("DISH_NOT_FOUND", "Dish not found.", 404);
        }

        if (items.Count == 0)
        {
            throw new AppException("DISH_RECIPE_REQUIRED", "At least one recipe ingredient is required.", 400);
        }

        if (items.Any(x => x.Quantity <= 0))
        {
            throw new AppException(
                "DISH_RECIPE_ITEM_INVALID_QUANTITY",
                "Recipe quantity must be greater than zero.",
                422);
        }

        var duplicateIngredientId = items
            .GroupBy(x => x.IngredientId)
            .Where(x => x.Count() > 1)
            .Select(x => x.Key)
            .FirstOrDefault();

        if (duplicateIngredientId > 0)
        {
            throw new AppException(
                "DISH_RECIPE_ITEM_DUPLICATE",
                $"Ingredient {duplicateIngredientId} is duplicated in recipe.",
                422);
        }

        var ingredientIds = items.Select(x => x.IngredientId).Distinct().ToArray();
        var existingIngredientIds = await ingredientRepository.Query()
            .Where(x => ingredientIds.Contains(x.Id))
            .Select(x => x.Id)
            .ToArrayAsync(cancellationToken);

        if (existingIngredientIds.Length != ingredientIds.Length)
        {
            throw new AppException("INGREDIENT_NOT_FOUND", "One or more ingredients were not found.", 404);
        }

        var existingRecipeItems = await dishIngredientRepository.Query()
            .Where(x => x.DishId == dishId)
            .ToListAsync(cancellationToken);

        foreach (var existingRecipeItem in existingRecipeItems)
        {
            dishIngredientRepository.Remove(existingRecipeItem);
        }

        foreach (var item in items)
        {
            await dishIngredientRepository.AddAsync(
                new DishIngredient
                {
                    DishId = dishId,
                    IngredientId = item.IngredientId,
                    Quantity = item.Quantity
                },
                cancellationToken);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return await dishIngredientRepository.Query()
            .Where(x => x.DishId == dishId)
            .Include(x => x.Ingredient)
            .OrderBy(x => x.Ingredient.Name)
            .ToListAsync(cancellationToken);
    }
}
