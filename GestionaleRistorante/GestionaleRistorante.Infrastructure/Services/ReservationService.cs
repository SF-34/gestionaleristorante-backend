using GestionaleRistorante.Core.Entities;
using GestionaleRistorante.Core.Enums;
using GestionaleRistorante.Core.Exceptions;
using GestionaleRistorante.Core.Interfaces.Repositories;
using GestionaleRistorante.Core.Interfaces.Services;
using GestionaleRistorante.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace GestionaleRistorante.Infrastructure.Services;

public sealed class ReservationService(
    IRepository<Reservation> reservationRepository,
    IRepository<RestaurantTable> tableRepository,
    IOrderService orderService,
    IUnitOfWork unitOfWork) : IReservationService
{
    public async Task<IReadOnlyList<Reservation>> GetForCustomerAsync(
        int customerUserId,
        CancellationToken cancellationToken = default)
    {
        return await reservationRepository.Query()
            .Where(x => x.UserId == customerUserId)
            .OrderBy(x => x.ReservationAtUtc)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Reservation>> GetForSalaAsync(CancellationToken cancellationToken = default)
    {
        return await reservationRepository.Query()
            .OrderBy(x => x.ReservationAtUtc)
            .ToListAsync(cancellationToken);
    }

    public async Task<Reservation> CreateAsync(
        Reservation reservation,
        int actorUserId,
        int actorRoleId,
        IReadOnlyCollection<CreateOrderItemModel>? orderItems,
        CancellationToken cancellationToken = default)
    {
        if (actorRoleId == (int)RoleIds.Customer)
        {
            reservation.UserId = actorUserId;
        }

        if (actorRoleId != (int)RoleIds.Customer && orderItems is { Count: > 0 })
        {
            throw new AppException(
                "RESERVATION_ORDER_FORBIDDEN",
                "Only customer reservation flow can include order items.",
                403);
        }

        if (reservation.UserId <= 0)
        {
            throw new AppException("RESERVATION_USER_REQUIRED", "Reservation user is required.", 400);
        }

        var tableExists = await tableRepository.Query()
            .AnyAsync(x => x.Id == reservation.TableId, cancellationToken);
        if (!tableExists)
        {
            throw new AppException("TABLE_NOT_FOUND", "Table not found.", 404);
        }

        await reservationRepository.AddAsync(reservation, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        if (actorRoleId == (int)RoleIds.Customer && orderItems is { Count: > 0 })
        {
            try
            {
                await orderService.CreateAsync(
                    actorUserId,
                    actorRoleId,
                    tableId: null,
                    reservationId: reservation.Id,
                    items: orderItems,
                    cancellationToken: cancellationToken);
            }
            catch
            {
                reservation.IsDeleted = true;
                reservationRepository.Update(reservation);
                await unitOfWork.SaveChangesAsync(cancellationToken);
                throw;
            }
        }

        return reservation;
    }

    public async Task<Reservation> AdminUpdateAsync(
        int reservationId,
        Reservation reservation,
        CancellationToken cancellationToken = default)
    {
        var existing = await reservationRepository.GetByIdAsync(reservationId, cancellationToken)
            ?? throw new AppException("RESERVATION_NOT_FOUND", "Reservation not found.", 404);

        existing.UserId = reservation.UserId;
        existing.TableId = reservation.TableId;
        existing.ReservationAtUtc = reservation.ReservationAtUtc;
        existing.PartySize = reservation.PartySize;
        existing.Notes = reservation.Notes;

        reservationRepository.Update(existing);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return existing;
    }

    public async Task DeleteAsync(
        int reservationId,
        int actorUserId,
        int actorRoleId,
        CancellationToken cancellationToken = default)
    {
        var existing = await reservationRepository.GetByIdAsync(reservationId, cancellationToken)
            ?? throw new AppException("RESERVATION_NOT_FOUND", "Reservation not found.", 404);

        if (actorRoleId == (int)RoleIds.Customer && existing.UserId != actorUserId)
        {
            throw new AppException("RESERVATION_FORBIDDEN", "You can only delete your own reservation.", 403);
        }

        existing.IsDeleted = true;
        reservationRepository.Update(existing);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
