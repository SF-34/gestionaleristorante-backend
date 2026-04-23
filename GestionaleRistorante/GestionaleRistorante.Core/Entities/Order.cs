using GestionaleRistorante.Core.Enums;

namespace GestionaleRistorante.Core.Entities;

public sealed class Order : SoftDeletableEntity
{
    public int? UserId { get; set; }

    public int? TableId { get; set; }

    public int? ReservationId { get; set; }

    public int CreatedByRoleId { get; set; }

    public string Status { get; set; } = OrderStatuses.Pending;

    public User? User { get; set; }

    public RestaurantTable? Table { get; set; }

    public Reservation? Reservation { get; set; }

    public ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();
}
