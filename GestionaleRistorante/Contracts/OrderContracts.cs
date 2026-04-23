using System.ComponentModel.DataAnnotations;

namespace GestionaleRistorante.Contracts;

public sealed class CreateOrderRequest
{
    public int? TableId { get; set; }

    public int? ReservationId { get; set; }

    [Required]
    [MinLength(1)]
    public List<CreateOrderItemRequest> Items { get; set; } = new();
}

public sealed class CreateOrderItemRequest
{
    [Range(1, int.MaxValue)]
    public int DishId { get; set; }

    [Range(1, 999)]
    public int Quantity { get; set; }

    [MaxLength(1000)]
    public string? Notes { get; set; }
}

public sealed class UpdateOrderStatusRequest
{
    [Required]
    public string Status { get; set; } = string.Empty;
}

public sealed class OrderResponse
{
    public int Id { get; set; }

    public int? UserId { get; set; }

    public int? TableId { get; set; }

    public int? ReservationId { get; set; }

    public int CreatedByRoleId { get; set; }

    public string Status { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    public List<OrderItemResponse> Items { get; set; } = new();
}

public sealed class OrderItemResponse
{
    public int Id { get; set; }

    public int DishId { get; set; }

    public string DishName { get; set; } = string.Empty;

    public int Quantity { get; set; }

    public decimal UnitPrice { get; set; }

    public string? Notes { get; set; }
}
