using System.ComponentModel.DataAnnotations;

namespace GestionaleRistorante.Contracts;

public class CreateIngredientRequest
{
    [Required]
    [MaxLength(150)]
    public string Name { get; set; } = string.Empty;

    [Range(0, 999999)]
    public decimal QuantityInStock { get; set; }

    [Required]
    [MaxLength(30)]
    public string Unit { get; set; } = string.Empty;
}

public sealed class UpdateIngredientRequest : CreateIngredientRequest;

public sealed class IngredientResponse
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public decimal QuantityInStock { get; set; }

    public string Unit { get; set; } = string.Empty;
}
