namespace GestionaleRistorante.Core.Entities;

public sealed class Dish : SoftDeletableEntity
{
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public decimal Price { get; set; }

    public bool IsAvailable { get; set; } = true;

    public string? ImageUrl { get; set; }

    public ICollection<DishIngredient> DishIngredients { get; set; } = new List<DishIngredient>();

    public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
}
