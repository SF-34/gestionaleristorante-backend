namespace GestionaleRistorante.Core.Entities;

public sealed class Reservation : SoftDeletableEntity
{
    public int UserId { get; set; }

    public int TableId { get; set; }

    public DateTime ReservationAtUtc { get; set; }

    public int PartySize { get; set; }

    public string? Notes { get; set; }

    public User User { get; set; } = null!;

    public RestaurantTable Table { get; set; } = null!;

    public ICollection<Order> Orders { get; set; } = new List<Order>();
}
