namespace GestionaleRistorante.Core.Entities;

public sealed class RefreshToken : AuditableEntity
{
    public int UserId { get; set; }

    public string TokenHash { get; set; } = string.Empty;

    public DateTime ExpiresAtUtc { get; set; }

    public bool IsRevoked { get; set; }

    public DateTime? RevokedAtUtc { get; set; }

    public string? ReplacedByTokenHash { get; set; }

    public User User { get; set; } = null!;
}
