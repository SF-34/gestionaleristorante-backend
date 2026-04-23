using GestionaleRistorante.Core.Entities;
using GestionaleRistorante.Core.Models;

namespace GestionaleRistorante.Core.Interfaces.Services;

public interface IOrderService
{
    Task<IReadOnlyList<Order>> GetForCustomerAsync(int customerUserId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Order>> GetForBackofficeAsync(int actorRoleId, CancellationToken cancellationToken = default);

    Task<Order> CreateAsync(
        int actorUserId,
        int actorRoleId,
        int? tableId,
        int? reservationId,
        IReadOnlyCollection<CreateOrderItemModel> items,
        CancellationToken cancellationToken = default);

    Task<Order> UpdateStatusAsync(
        int orderId,
        string newStatus,
        int actorUserId,
        int actorRoleId,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(int orderId, int actorUserId, int actorRoleId, CancellationToken cancellationToken = default);
}
