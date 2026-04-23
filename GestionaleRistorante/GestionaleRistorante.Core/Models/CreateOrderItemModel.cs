namespace GestionaleRistorante.Core.Models;

public sealed record CreateOrderItemModel(int DishId, int Quantity, string? Notes);
