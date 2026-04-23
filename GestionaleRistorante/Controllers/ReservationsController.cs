using GestionaleRistorante.Common.Extensions;
using GestionaleRistorante.Contracts;
using GestionaleRistorante.Core.Entities;
using GestionaleRistorante.Core.Interfaces.Services;
using GestionaleRistorante.Core.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GestionaleRistorante.Controllers;

[Route("api/v1/reservations")]
public sealed class ReservationsController(IReservationService reservationService) : ApiControllerBase
{
    [Authorize(Roles = "1")]
    [HttpGet("my")]
    [ProducesResponseType(typeof(IReadOnlyList<ReservationResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMine(CancellationToken cancellationToken)
    {
        var userId = User.GetRequiredUserId();
        var reservations = await reservationService.GetForCustomerAsync(userId, cancellationToken);
        return Success(reservations.Select(ToResponse).ToList());
    }

    [Authorize(Roles = "3,99")]
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<ReservationResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var reservations = await reservationService.GetForSalaAsync(cancellationToken);
        return Success(reservations.Select(ToResponse).ToList());
    }

    [Authorize(Roles = "1,99")]
    [HttpPost]
    [ProducesResponseType(typeof(ReservationResponse), StatusCodes.Status201Created)]
    public async Task<IActionResult> Create(
        [FromBody] CreateReservationRequest request,
        CancellationToken cancellationToken)
    {
        var actorUserId = User.GetRequiredUserId();
        var actorRoleId = User.GetRequiredRoleId();

        var reservation = new Reservation
        {
            UserId = request.UserId ?? actorUserId,
            TableId = request.TableId,
            ReservationAtUtc = request.ReservationAtUtc,
            PartySize = request.PartySize,
            Notes = request.Notes
        };

        var orderItems = request.Order?.Items
            .Select(x => new CreateOrderItemModel(x.DishId, x.Quantity, x.Notes))
            .ToArray();

        var created = await reservationService.CreateAsync(
            reservation,
            actorUserId,
            actorRoleId,
            orderItems,
            cancellationToken);

        return Success(ToResponse(created), StatusCodes.Status201Created);
    }

    [Authorize(Roles = "99")]
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(ReservationResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> AdminUpdate(
        int id,
        [FromBody] AdminUpdateReservationRequest request,
        CancellationToken cancellationToken)
    {
        var reservation = new Reservation
        {
            UserId = request.UserId ?? 0,
            TableId = request.TableId,
            ReservationAtUtc = request.ReservationAtUtc,
            PartySize = request.PartySize,
            Notes = request.Notes
        };

        var updated = await reservationService.AdminUpdateAsync(id, reservation, cancellationToken);
        return Success(ToResponse(updated));
    }

    [Authorize(Roles = "1,99")]
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var actorUserId = User.GetRequiredUserId();
        var actorRoleId = User.GetRequiredRoleId();

        await reservationService.DeleteAsync(id, actorUserId, actorRoleId, cancellationToken);
        return SuccessNoData();
    }

    private static ReservationResponse ToResponse(Reservation reservation)
    {
        return new ReservationResponse
        {
            Id = reservation.Id,
            UserId = reservation.UserId,
            TableId = reservation.TableId,
            ReservationAtUtc = reservation.ReservationAtUtc,
            PartySize = reservation.PartySize,
            Notes = reservation.Notes
        };
    }
}
