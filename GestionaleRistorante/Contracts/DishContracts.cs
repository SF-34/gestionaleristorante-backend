using System.ComponentModel.DataAnnotations;

namespace GestionaleRistorante.Contracts;

public class CreateDishRequest
{
    [Required]
    [MaxLength(150)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(2000)]
    public string? Description { get; set; }

    [Range(0.01, 99999)]
    public decimal Price { get; set; }

    public bool IsAvailable { get; set; } = true;
}

public sealed class UpdateDishRequest : CreateDishRequest;

public sealed class UploadDishImageRequest
{
    [Required]
    public IFormFile File { get; set; } = null!;
}

public sealed class ReplaceDishRecipeRequest
{
    [Required]
    [MinLength(1)]
    public List<ReplaceDishRecipeItemRequest> Items { get; set; } = new();
}

public sealed class ReplaceDishRecipeItemRequest
{
    [Range(1, int.MaxValue)]
    public int IngredientId { get; set; }

    [Range(typeof(decimal), "0.001", "999999")]
    public decimal Quantity { get; set; }
}

public sealed class DishRecipeResponse
{
    public int DishId { get; set; }

    public List<DishRecipeItemResponse> Items { get; set; } = new();
}

public sealed class DishRecipeItemResponse
{
    public int IngredientId { get; set; }

    public string IngredientName { get; set; } = string.Empty;

    public decimal Quantity { get; set; }

    public string Unit { get; set; } = string.Empty;
}

public sealed class DishResponse
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public decimal Price { get; set; }

    public bool IsAvailable { get; set; }

    public string? ImageUrl { get; set; }
}
