using GestionaleRistorante.Common.Extensions;
using GestionaleRistorante.Contracts;
using GestionaleRistorante.Core.Entities;
using GestionaleRistorante.Core.Interfaces.Services;
using GestionaleRistorante.Core.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GestionaleRistorante.Controllers;

[Route("api/v1/orders")]
public sealed class OrdersController(IOrderService orderService) : ApiControllerBase
{
    [Authorize(Roles = "1")]
    [HttpGet("my")]
    [ProducesResponseType(typeof(IReadOnlyList<OrderResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMine(CancellationToken cancellationToken)
    {
        var actorUserId = User.GetRequiredUserId();
        var orders = await orderService.GetForCustomerAsync(actorUserId, cancellationToken);
        return Success(orders.Select(ToResponse).ToList());
    }

    [Authorize(Roles = "2,3,99")]
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<OrderResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var actorRoleId = User.GetRequiredRoleId();
        var orders = await orderService.GetForBackofficeAsync(actorRoleId, cancellationToken);
        return Success(orders.Select(ToResponse).ToList());
    }

    [Authorize(Roles = "3,99")]
    [HttpPost]
    [ProducesResponseType(typeof(OrderResponse), StatusCodes.Status201Created)]
    public async Task<IActionResult> Create([FromBody] CreateOrderRequest request, CancellationToken cancellationToken)
    {
        var actorUserId = User.GetRequiredUserId();
        var actorRoleId = User.GetRequiredRoleId();

        var items = request.Items
            .Select(x => new CreateOrderItemModel(x.DishId, x.Quantity, x.Notes))
            .ToArray();

        var order = await orderService.CreateAsync(
            actorUserId,
            actorRoleId,
            request.TableId,
            request.ReservationId,
            items,
            cancellationToken);

        return Success(ToResponse(order), StatusCodes.Status201Created);
    }

    [Authorize(Roles = "1,2,3,99")]
    [HttpPut("{id:int}/status")]
    [ProducesResponseType(typeof(OrderResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateStatus(
        int id,
        [FromBody] UpdateOrderStatusRequest request,
        CancellationToken cancellationToken)
    {
        // Policy allineata a Docs/09: transizioni stato validate da OrderService per ruolo/stato corrente.
        var actorUserId = User.GetRequiredUserId();
        var actorRoleId = User.GetRequiredRoleId();

        var order = await orderService.UpdateStatusAsync(
            id,
            request.Status,
            actorUserId,
            actorRoleId,
            cancellationToken);

        return Success(ToResponse(order));
    }

    [Authorize(Roles = "1,3,99")]
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var actorUserId = User.GetRequiredUserId();
        var actorRoleId = User.GetRequiredRoleId();

        await orderService.DeleteAsync(id, actorUserId, actorRoleId, cancellationToken);
        return SuccessNoData();
    }

    private static OrderResponse ToResponse(Order order)
    {
        return new OrderResponse
        {
            Id = order.Id,
            UserId = order.UserId,
            TableId = order.TableId,
            ReservationId = order.ReservationId,
            CreatedByRoleId = order.CreatedByRoleId,
            Status = order.Status,
            CreatedAt = order.CreatedAt,
            Items = order.Items
                .Select(x => new OrderItemResponse
                {
                    Id = x.Id,
                    DishId = x.DishId,
                    DishName = x.Dish?.Name ?? string.Empty,
                    Quantity = x.Quantity,
                    UnitPrice = x.UnitPrice,
                    Notes = x.Notes
                })
                .ToList()
        };
    }
}
