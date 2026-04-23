namespace GestionaleRistorante.Core.Entities;

public sealed class User : SoftDeletableEntity
{
    public string Email { get; set; } = string.Empty;

    public string PasswordHash { get; set; } = string.Empty;

    public string FirstName { get; set; } = string.Empty;

    public string LastName { get; set; } = string.Empty;

    public int RoleId { get; set; }

    public bool RequirePasswordChange { get; set; }

    public Role Role { get; set; } = null!;

    public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();

    public ICollection<Reservation> Reservations { get; set; } = new List<Reservation>();

    public ICollection<Order> Orders { get; set; } = new List<Order>();
}
