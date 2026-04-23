using GestionaleRistorante.Contracts;
using GestionaleRistorante.Core.Entities;
using GestionaleRistorante.Core.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GestionaleRistorante.Controllers;

[Authorize(Roles = "2,99")]
[Route("api/v1/ingredients")]
public sealed class IngredientsController(IIngredientService ingredientService) : ApiControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<IngredientResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var ingredients = await ingredientService.GetAllAsync(cancellationToken);
        return Success(ingredients.Select(ToResponse).ToList());
    }

    [HttpPost]
    [ProducesResponseType(typeof(IngredientResponse), StatusCodes.Status201Created)]
    public async Task<IActionResult> Create(
        [FromBody] CreateIngredientRequest request,
        CancellationToken cancellationToken)
    {
        var ingredient = new Ingredient
        {
            Name = request.Name,
            QuantityInStock = request.QuantityInStock,
            Unit = request.Unit
        };

        var created = await ingredientService.CreateAsync(ingredient, cancellationToken);
        return Success(ToResponse(created), StatusCodes.Status201Created);
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(IngredientResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> Update(
        int id,
        [FromBody] UpdateIngredientRequest request,
        CancellationToken cancellationToken)
    {
        var ingredient = new Ingredient
        {
            Name = request.Name,
            QuantityInStock = request.QuantityInStock,
            Unit = request.Unit
        };

        var updated = await ingredientService.UpdateAsync(id, ingredient, cancellationToken);
        return Success(ToResponse(updated));
    }

    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        await ingredientService.DeleteAsync(id, cancellationToken);
        return SuccessNoData();
    }

    private static IngredientResponse ToResponse(Ingredient ingredient)
    {
        return new IngredientResponse
        {
            Id = ingredient.Id,
            Name = ingredient.Name,
            QuantityInStock = ingredient.QuantityInStock,
            Unit = ingredient.Unit
        };
    }
}
