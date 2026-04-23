namespace GestionaleRistorante.Core.Entities;

public sealed class Ingredient : SoftDeletableEntity
{
    public string Name { get; set; } = string.Empty;

    public decimal QuantityInStock { get; set; }

    public string Unit { get; set; } = string.Empty;

    public ICollection<DishIngredient> DishIngredients { get; set; } = new List<DishIngredient>();
}
