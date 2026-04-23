using GestionaleRistorante.Contracts;
using GestionaleRistorante.Core.Entities;
using GestionaleRistorante.Core.Exceptions;
using GestionaleRistorante.Core.Interfaces.Services;
using GestionaleRistorante.Core.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GestionaleRistorante.Controllers;

[Route("api/v1/dishes")]
public sealed class DishesController(IDishService dishService) : ApiControllerBase
{
    [AllowAnonymous]
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<DishResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var dishes = await dishService.GetAllAsync(cancellationToken);
        return Success(dishes.Select(ToResponse).ToList());
    }

    [AllowAnonymous]
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(DishResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken)
    {
        var dish = await dishService.GetByIdAsync(id, cancellationToken)
            ?? throw new AppException("DISH_NOT_FOUND", "Dish not found.", 404);

        return Success(ToResponse(dish));
    }

    [Authorize(Roles = "2,99")]
    [HttpPost]
    [ProducesResponseType(typeof(DishResponse), StatusCodes.Status201Created)]
    public async Task<IActionResult> Create([FromBody] CreateDishRequest request, CancellationToken cancellationToken)
    {
        var dish = new Dish
        {
            Name = request.Name,
            Description = request.Description,
            Price = request.Price,
            IsAvailable = request.IsAvailable
        };

        var created = await dishService.CreateAsync(dish, cancellationToken);
        return Success(ToResponse(created), StatusCodes.Status201Created);
    }

    [Authorize(Roles = "2,99")]
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(DishResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateDishRequest request, CancellationToken cancellationToken)
    {
        var dish = new Dish
        {
            Name = request.Name,
            Description = request.Description,
            Price = request.Price,
            IsAvailable = request.IsAvailable
        };

        var updated = await dishService.UpdateAsync(id, dish, cancellationToken);
        return Success(ToResponse(updated));
    }

    [Authorize(Roles = "2,99")]
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        await dishService.DeleteAsync(id, cancellationToken);
        return SuccessNoData();
    }

    [Authorize(Roles = "2,99")]
    [HttpPost("{id:int}/image")]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    public async Task<IActionResult> UploadImage(
        int id,
        [FromForm] UploadDishImageRequest request,
        CancellationToken cancellationToken)
    {
        await using var stream = request.File.OpenReadStream();
        var imageUrl = await dishService.UploadImageAsync(
            id,
            stream,
            request.File.FileName,
            request.File.ContentType,
            cancellationToken);

        return Success(new { imageUrl });
    }

    [Authorize(Roles = "2,99")]
    [HttpGet("{id:int}/ingredients")]
    [ProducesResponseType(typeof(DishRecipeResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetRecipe(int id, CancellationToken cancellationToken)
    {
        var recipeItems = await dishService.GetRecipeAsync(id, cancellationToken);
        return Success(ToRecipeResponse(id, recipeItems));
    }

    [Authorize(Roles = "2,99")]
    [HttpPut("{id:int}/ingredients")]
    [ProducesResponseType(typeof(DishRecipeResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> ReplaceRecipe(
        int id,
        [FromBody] ReplaceDishRecipeRequest request,
        CancellationToken cancellationToken)
    {
        var items = request.Items
            .Select(x => new UpsertDishRecipeItemModel(x.IngredientId, x.Quantity))
            .ToArray();

        var recipeItems = await dishService.ReplaceRecipeAsync(id, items, cancellationToken);
        return Success(ToRecipeResponse(id, recipeItems));
    }

    private static DishResponse ToResponse(Dish dish)
    {
        return new DishResponse
        {
            Id = dish.Id,
            Name = dish.Name,
            Description = dish.Description,
            Price = dish.Price,
            IsAvailable = dish.IsAvailable,
            ImageUrl = dish.ImageUrl
        };
    }

    private static DishRecipeResponse ToRecipeResponse(int dishId, IReadOnlyCollection<DishIngredient> recipeItems)
    {
        return new DishRecipeResponse
        {
            DishId = dishId,
            Items = recipeItems
                .OrderBy(x => x.Ingredient.Name)
                .Select(x => new DishRecipeItemResponse
                {
                    IngredientId = x.IngredientId,
                    IngredientName = x.Ingredient.Name,
                    Quantity = x.Quantity,
                    Unit = x.Ingredient.Unit
                })
                .ToList()
        };
    }
}
