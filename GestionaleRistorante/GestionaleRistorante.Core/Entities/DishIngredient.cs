namespace GestionaleRistorante.Core.Entities;

public sealed class DishIngredient : AuditableEntity
{
    public int DishId { get; set; }

    public int IngredientId { get; set; }

    public decimal Quantity { get; set; }

    public Dish Dish { get; set; } = null!;

    public Ingredient Ingredient { get; set; } = null!;
}
