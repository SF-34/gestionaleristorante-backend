namespace GestionaleRistorante.Core.Entities;

public sealed class RestaurantTable : SoftDeletableEntity
{
    public string Name { get; set; } = string.Empty;

    public int Capacity { get; set; }

    public string Status { get; set; } = "available";

    public ICollection<Reservation> Reservations { get; set; } = new List<Reservation>();

    public ICollection<Order> Orders { get; set; } = new List<Order>();
}
