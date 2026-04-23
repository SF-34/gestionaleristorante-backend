using GestionaleRistorante.Core.Entities;
using GestionaleRistorante.Core.Enums;
using GestionaleRistorante.Core.Exceptions;
using GestionaleRistorante.Core.Interfaces.Repositories;
using GestionaleRistorante.Core.Interfaces.Services;
using GestionaleRistorante.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace GestionaleRistorante.Infrastructure.Services;

public sealed class OrderService(
    IRepository<Order> orderRepository,
    IRepository<Dish> dishRepository,
    IRepository<DishIngredient> dishIngredientRepository,
    IRepository<RestaurantTable> tableRepository,
    IRepository<Reservation> reservationRepository,
    IRepository<Ingredient> ingredientRepository,
    IUnitOfWork unitOfWork) : IOrderService
{
    public async Task<IReadOnlyList<Order>> GetForCustomerAsync(
        int customerUserId,
        CancellationToken cancellationToken = default)
    {
        return await orderRepository.Query()
            .Where(x => x.UserId == customerUserId && x.CreatedByRoleId == (int)RoleIds.Customer)
            .Include(x => x.Items)
            .ThenInclude(x => x.Dish)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Order>> GetForBackofficeAsync(
        int actorRoleId,
        CancellationToken cancellationToken = default)
    {
        var query = orderRepository.Query()
            .Include(x => x.Items)
            .ThenInclude(x => x.Dish)
            .OrderByDescending(x => x.CreatedAt)
            .AsQueryable();

        if (actorRoleId == (int)RoleIds.Kitchen)
        {
            query = query.Where(x =>
                x.Status == OrderStatuses.Pending
                || x.Status == OrderStatuses.InPreparation
                || x.Status == OrderStatuses.Ready);
        }

        return await query.ToListAsync(cancellationToken);
    }

    public async Task<Order> CreateAsync(
        int actorUserId,
        int actorRoleId,
        int? tableId,
        int? reservationId,
        IReadOnlyCollection<CreateOrderItemModel> items,
        CancellationToken cancellationToken = default)
    {
        if (items.Count == 0)
        {
            throw new AppException("ORDER_ITEMS_REQUIRED", "At least one order item is required.", 400);
        }

        if (actorRoleId == (int)RoleIds.Customer && reservationId is null)
        {
            throw new AppException(
                "ORDER_RESERVATION_REQUIRED",
                "Customer order requires reservation.",
                422);
        }

        if (actorRoleId == (int)RoleIds.Sala && tableId is null)
        {
            throw new AppException("ORDER_TABLE_REQUIRED", "Table is required for Sala orders.", 400);
        }

        Reservation? reservation = null;
        if (reservationId.HasValue)
        {
            reservation = await reservationRepository.GetByIdAsync(reservationId.Value, cancellationToken)
                ?? throw new AppException("ORDER_RESERVATION_NOT_FOUND", "Reservation not found.", 404);

            if (actorRoleId == (int)RoleIds.Customer && reservation.UserId != actorUserId)
            {
                throw new AppException(
                    "ORDER_RESERVATION_FORBIDDEN",
                    "Customer can only use owned reservations.",
                    403);
            }

            if (actorRoleId == (int)RoleIds.Customer)
            {
                var hasOrderForReservation = await orderRepository.Query()
                    .AnyAsync(
                        x => x.ReservationId == reservation.Id
                            && x.CreatedByRoleId == (int)RoleIds.Customer,
                        cancellationToken);

                if (hasOrderForReservation)
                {
                    throw new AppException(
                        "ORDER_RESERVATION_ALREADY_HAS_ORDER",
                        "Reservation already has an order.",
                        422);
                }
            }
        }

        var resolvedTableId = reservation?.TableId ?? tableId;

        if (resolvedTableId.HasValue)
        {
            var tableExists = await tableRepository.Query()
                .AnyAsync(x => x.Id == resolvedTableId.Value, cancellationToken);
            if (!tableExists)
            {
                throw new AppException("TABLE_NOT_FOUND", "Table not found.", 404);
            }
        }

        var requestedDishIds = items.Select(x => x.DishId).Distinct().ToArray();
        var dishesById = await dishRepository.Query()
            .Where(x => requestedDishIds.Contains(x.Id) && x.IsAvailable)
            .ToDictionaryAsync(x => x.Id, cancellationToken);

        if (dishesById.Count != requestedDishIds.Length)
        {
            throw new AppException("ORDER_DISH_NOT_AVAILABLE", "One or more dishes are not available.", 400);
        }

        var recipesByDishId = await GetRecipesByDishIdAsync(requestedDishIds, cancellationToken);
        var requiredIngredients = BuildIngredientRequirements(
            items.Select(x => (x.DishId, x.Quantity)),
            recipesByDishId);

        await ApplyIngredientStockDeltaAsync(requiredIngredients, subtract: true, cancellationToken);

        var order = new Order
        {
            UserId = actorRoleId == (int)RoleIds.Customer ? actorUserId : null,
            TableId = resolvedTableId,
            ReservationId = reservation?.Id,
            CreatedByRoleId = actorRoleId,
            Status = OrderStatuses.Pending,
            Items = items
                .Select(item =>
                {
                    var dish = dishesById[item.DishId];
                    return new OrderItem
                    {
                        DishId = item.DishId,
                        Quantity = item.Quantity,
                        Notes = item.Notes,
                        UnitPrice = dish.Price
                    };
                })
                .ToList()
        };

        await orderRepository.AddAsync(order, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return await LoadOrderDetailsAsync(order.Id, cancellationToken);
    }

    public async Task<Order> UpdateStatusAsync(
        int orderId,
        string newStatus,
        int actorUserId,
        int actorRoleId,
        CancellationToken cancellationToken = default)
    {
        var order = await orderRepository.Query()
            .Include(x => x.Items)
            .FirstOrDefaultAsync(x => x.Id == orderId, cancellationToken)
            ?? throw new AppException("ORDER_NOT_FOUND", "Order not found.", 404);

        var normalizedStatus = newStatus.Trim().ToLowerInvariant();
        if (!OrderStatuses.All.Contains(normalizedStatus))
        {
            throw new AppException("ORDER_INVALID_STATUS", "Invalid order status.", 422);
        }

        if (!IsTransitionAllowed(order, normalizedStatus, actorUserId, actorRoleId))
        {
            throw new AppException(
                "ORDER_INVALID_TRANSITION",
                "Transition is not allowed for the current state and role.",
                422);
        }

        if (normalizedStatus == OrderStatuses.Cancelled && ShouldRestoreStockOnCancellation(order.Status))
        {
            await RestoreStockForOrderAsync(order, cancellationToken);
        }

        order.Status = normalizedStatus;
        orderRepository.Update(order);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return await LoadOrderDetailsAsync(order.Id, cancellationToken);
    }

    public async Task DeleteAsync(
        int orderId,
        int actorUserId,
        int actorRoleId,
        CancellationToken cancellationToken = default)
    {
        var order = await orderRepository.Query()
            .Include(x => x.Items)
            .FirstOrDefaultAsync(x => x.Id == orderId, cancellationToken)
            ?? throw new AppException("ORDER_NOT_FOUND", "Order not found.", 404);

        if (actorRoleId == (int)RoleIds.Customer)
        {
            var isOwner = order.UserId == actorUserId && order.CreatedByRoleId == (int)RoleIds.Customer;
            var canDelete = isOwner && order.Status == OrderStatuses.Pending;
            if (!canDelete)
            {
                throw new AppException(
                    "ORDER_DELETE_FORBIDDEN",
                    "Customers can delete only their pending online orders.",
                    403);
            }
        }

        if (ShouldRestoreStockOnCancellation(order.Status))
        {
            await RestoreStockForOrderAsync(order, cancellationToken);
        }

        order.Status = OrderStatuses.Cancelled;
        order.IsDeleted = true;
        orderRepository.Update(order);

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private async Task<Order> LoadOrderDetailsAsync(int orderId, CancellationToken cancellationToken)
    {
        return await orderRepository.Query()
            .Include(x => x.Items)
            .ThenInclude(x => x.Dish)
            .FirstAsync(x => x.Id == orderId, cancellationToken);
    }

    private async Task<Dictionary<int, IReadOnlyCollection<DishIngredient>>> GetRecipesByDishIdAsync(
        IReadOnlyCollection<int> dishIds,
        CancellationToken cancellationToken)
    {
        var recipeRows = await dishIngredientRepository.Query()
            .Where(x => dishIds.Contains(x.DishId))
            .ToListAsync(cancellationToken);

        return recipeRows
            .GroupBy(x => x.DishId)
            .ToDictionary(x => x.Key, x => (IReadOnlyCollection<DishIngredient>)x.ToList());
    }

    private static Dictionary<int, decimal> BuildIngredientRequirements(
        IEnumerable<(int DishId, int Quantity)> orderedItems,
        IReadOnlyDictionary<int, IReadOnlyCollection<DishIngredient>> recipesByDishId)
    {
        var requiredIngredients = new Dictionary<int, decimal>();

        foreach (var (dishId, quantity) in orderedItems)
        {
            if (!recipesByDishId.TryGetValue(dishId, out var recipeItems) || recipeItems.Count == 0)
            {
                throw new AppException(
                    "ORDER_RECIPE_MISSING",
                    $"Dish {dishId} has no configured recipe.",
                    422);
            }

            foreach (var recipeItem in recipeItems)
            {
                if (recipeItem.Quantity <= 0)
                {
                    throw new AppException(
                        "ORDER_RECIPE_MISSING",
                        $"Dish {dishId} has invalid recipe quantities.",
                        422);
                }

                var totalRequiredQuantity = recipeItem.Quantity * quantity;
                if (requiredIngredients.TryGetValue(recipeItem.IngredientId, out var existingQuantity))
                {
                    requiredIngredients[recipeItem.IngredientId] = existingQuantity + totalRequiredQuantity;
                }
                else
                {
                    requiredIngredients[recipeItem.IngredientId] = totalRequiredQuantity;
                }
            }
        }

        return requiredIngredients;
    }

    private async Task ApplyIngredientStockDeltaAsync(
        IReadOnlyDictionary<int, decimal> ingredientRequirements,
        bool subtract,
        CancellationToken cancellationToken)
    {
        if (ingredientRequirements.Count == 0)
        {
            throw new AppException("ORDER_RECIPE_MISSING", "Missing recipe requirements.", 422);
        }

        var ingredientIds = ingredientRequirements.Keys.ToArray();
        var ingredientsById = await ingredientRepository.Query()
            .Where(x => ingredientIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, cancellationToken);

        if (ingredientsById.Count != ingredientIds.Length)
        {
            throw new AppException("ORDER_RECIPE_MISSING", "One or more recipe ingredients were not found.", 422);
        }

        if (subtract)
        {
            foreach (var ingredientId in ingredientIds)
            {
                var ingredient = ingredientsById[ingredientId];
                var requiredQuantity = ingredientRequirements[ingredientId];
                if (ingredient.QuantityInStock < requiredQuantity)
                {
                    throw new AppException(
                        "ORDER_INGREDIENT_STOCK_INSUFFICIENT",
                        $"Insufficient stock for ingredient {ingredient.Name}.",
                        422);
                }
            }
        }

        foreach (var ingredientId in ingredientIds)
        {
            var ingredient = ingredientsById[ingredientId];
            var requiredQuantity = ingredientRequirements[ingredientId];
            ingredient.QuantityInStock = subtract
                ? ingredient.QuantityInStock - requiredQuantity
                : ingredient.QuantityInStock + requiredQuantity;
        }
    }

    private async Task RestoreStockForOrderAsync(Order order, CancellationToken cancellationToken)
    {
        if (order.Items.Count == 0)
        {
            return;
        }

        var dishIds = order.Items.Select(x => x.DishId).Distinct().ToArray();
        var recipesByDishId = await GetRecipesByDishIdAsync(dishIds, cancellationToken);
        var requiredIngredients = BuildIngredientRequirements(
            order.Items.Select(x => (x.DishId, x.Quantity)),
            recipesByDishId);

        await ApplyIngredientStockDeltaAsync(requiredIngredients, subtract: false, cancellationToken);
    }

    private static bool ShouldRestoreStockOnCancellation(string currentStatus)
    {
        return string.Equals(currentStatus, OrderStatuses.Pending, StringComparison.OrdinalIgnoreCase)
            || string.Equals(currentStatus, OrderStatuses.InPreparation, StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsTransitionAllowed(Order order, string newStatus, int actorUserId, int actorRoleId)
    {
        var isAdmin = actorRoleId == (int)RoleIds.Admin;
        var isKitchen = actorRoleId == (int)RoleIds.Kitchen;
        var isSala = actorRoleId == (int)RoleIds.Sala;
        var isCustomer = actorRoleId == (int)RoleIds.Customer;

        if (OrderStatuses.FinalStates.Contains(order.Status))
        {
            return false;
        }

        return order.Status switch
        {
            OrderStatuses.Pending => newStatus switch
            {
                OrderStatuses.InPreparation => isKitchen || isAdmin,
                OrderStatuses.Cancelled =>
                    isAdmin
                    || isSala
                    || (isCustomer
                        && order.CreatedByRoleId == (int)RoleIds.Customer
                        && order.UserId == actorUserId),
                _ => false
            },
            OrderStatuses.InPreparation => newStatus switch
            {
                OrderStatuses.Ready => isKitchen || isAdmin,
                OrderStatuses.Cancelled => isSala || isAdmin,
                _ => false
            },
            OrderStatuses.Ready => newStatus switch
            {
                OrderStatuses.Served => isSala || isAdmin,
                OrderStatuses.InPreparation => isKitchen || isAdmin,
                _ => false
            },
            _ => false
        };
    }
}
