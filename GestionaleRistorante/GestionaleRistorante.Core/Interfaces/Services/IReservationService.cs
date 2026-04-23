using GestionaleRistorante.Core.Entities;
using GestionaleRistorante.Core.Models;

namespace GestionaleRistorante.Core.Interfaces.Services;

public interface IReservationService
{
    Task<IReadOnlyList<Reservation>> GetForCustomerAsync(int customerUserId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Reservation>> GetForSalaAsync(CancellationToken cancellationToken = default);

    Task<Reservation> CreateAsync(
        Reservation reservation,
        int actorUserId,
        int actorRoleId,
        IReadOnlyCollection<CreateOrderItemModel>? orderItems,
        CancellationToken cancellationToken = default);

    Task<Reservation> AdminUpdateAsync(int reservationId, Reservation reservation, CancellationToken cancellationToken = default);

    Task DeleteAsync(int reservationId, int actorUserId, int actorRoleId, CancellationToken cancellationToken = default);
}
