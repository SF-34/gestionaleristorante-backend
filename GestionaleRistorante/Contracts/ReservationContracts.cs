using System.ComponentModel.DataAnnotations;

namespace GestionaleRistorante.Contracts;

public class CreateReservationRequest
{
    public int? UserId { get; set; }

    [Range(1, int.MaxValue)]
    public int TableId { get; set; }

    [Required]
    public DateTime ReservationAtUtc { get; set; }

    [Range(1, 100)]
    public int PartySize { get; set; }

    [MaxLength(1000)]
    public string? Notes { get; set; }

    public CreateReservationOrderRequest? Order { get; set; }
}

public sealed class AdminUpdateReservationRequest : CreateReservationRequest;

public sealed class CreateReservationOrderRequest
{
    [Required]
    [MinLength(1)]
    public List<CreateOrderItemRequest> Items { get; set; } = new();
}

public sealed class ReservationResponse
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public int TableId { get; set; }

    public DateTime ReservationAtUtc { get; set; }

    public int PartySize { get; set; }

    public string? Notes { get; set; }
}
