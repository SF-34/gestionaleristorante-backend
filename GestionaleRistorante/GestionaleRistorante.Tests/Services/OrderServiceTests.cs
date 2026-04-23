using GestionaleRistorante.Core.Entities;
using GestionaleRistorante.Core.Enums;
using GestionaleRistorante.Core.Exceptions;
using GestionaleRistorante.Core.Models;
using GestionaleRistorante.Infrastructure.Data;
using GestionaleRistorante.Infrastructure.Repositories;
using GestionaleRistorante.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace GestionaleRistorante.Tests.Services;

public sealed class OrderServiceTests
{
    [Fact]
    public async Task UpdateStatusAsync_PendingToInPreparation_ByKitchen_Succeeds()
    {
        await using var dbContext = CreateDbContext();
        var order = await SeedOrderAsync(dbContext, OrderStatuses.Pending, userId: 10, createdByRoleId: (int)RoleIds.Customer);
        var service = CreateService(dbContext);

        var updated = await service.UpdateStatusAsync(
            order.Id,
            OrderStatuses.InPreparation,
            actorUserId: 20,
            actorRoleId: (int)RoleIds.Kitchen);

        Assert.Equal(OrderStatuses.InPreparation, updated.Status);
    }

    [Fact]
    public async Task UpdateStatusAsync_PendingToCancelled_ByCustomerOwner_Succeeds()
    {
        await using var dbContext = CreateDbContext();
        var order = await SeedOrderAsync(dbContext, OrderStatuses.Pending, userId: 10, createdByRoleId: (int)RoleIds.Customer);
        var service = CreateService(dbContext);

        var updated = await service.UpdateStatusAsync(
            order.Id,
            OrderStatuses.Cancelled,
            actorUserId: 10,
            actorRoleId: (int)RoleIds.Customer);

        Assert.Equal(OrderStatuses.Cancelled, updated.Status);
    }

    [Fact]
    public async Task UpdateStatusAsync_PendingToCancelled_ByDifferentCustomer_ThrowsInvalidTransition()
    {
        await using var dbContext = CreateDbContext();
        var order = await SeedOrderAsync(dbContext, OrderStatuses.Pending, userId: 10, createdByRoleId: (int)RoleIds.Customer);
        var service = CreateService(dbContext);

        var exception = await Assert.ThrowsAsync<AppException>(() =>
            service.UpdateStatusAsync(
                order.Id,
                OrderStatuses.Cancelled,
                actorUserId: 99,
                actorRoleId: (int)RoleIds.Customer));

        Assert.Equal("ORDER_INVALID_TRANSITION", exception.Code);
        Assert.Equal(422, exception.StatusCode);
    }

    [Fact]
    public async Task UpdateStatusAsync_InPreparationToReady_BySala_ThrowsInvalidTransition()
    {
        await using var dbContext = CreateDbContext();
        var order = await SeedOrderAsync(dbContext, OrderStatuses.InPreparation, userId: null, createdByRoleId: (int)RoleIds.Sala);
        var service = CreateService(dbContext);

        var exception = await Assert.ThrowsAsync<AppException>(() =>
            service.UpdateStatusAsync(
                order.Id,
                OrderStatuses.Ready,
                actorUserId: 30,
                actorRoleId: (int)RoleIds.Sala));

        Assert.Equal("ORDER_INVALID_TRANSITION", exception.Code);
        Assert.Equal(422, exception.StatusCode);
    }

    [Fact]
    public async Task UpdateStatusAsync_ReadyToServed_BySala_Succeeds()
    {
        await using var dbContext = CreateDbContext();
        var order = await SeedOrderAsync(dbContext, OrderStatuses.Ready, userId: null, createdByRoleId: (int)RoleIds.Sala);
        var service = CreateService(dbContext);

        var updated = await service.UpdateStatusAsync(
            order.Id,
            OrderStatuses.Served,
            actorUserId: 30,
            actorRoleId: (int)RoleIds.Sala);

        Assert.Equal(OrderStatuses.Served, updated.Status);
    }

    [Fact]
    public async Task UpdateStatusAsync_FromFinalState_ThrowsInvalidTransition()
    {
        await using var dbContext = CreateDbContext();
        var order = await SeedOrderAsync(dbContext, OrderStatuses.Served, userId: null, createdByRoleId: (int)RoleIds.Sala);
        var service = CreateService(dbContext);

        var exception = await Assert.ThrowsAsync<AppException>(() =>
            service.UpdateStatusAsync(
                order.Id,
                OrderStatuses.InPreparation,
                actorUserId: 20,
                actorRoleId: (int)RoleIds.Kitchen));

        Assert.Equal("ORDER_INVALID_TRANSITION", exception.Code);
        Assert.Equal(422, exception.StatusCode);
    }

    [Fact]
    public async Task DeleteAsync_CustomerOwnPendingOrder_SoftDeletesAndCancels()
    {
        await using var dbContext = CreateDbContext();
        var order = await SeedOrderAsync(dbContext, OrderStatuses.Pending, userId: 10, createdByRoleId: (int)RoleIds.Customer);
        var service = CreateService(dbContext);

        await service.DeleteAsync(order.Id, actorUserId: 10, actorRoleId: (int)RoleIds.Customer);

        var persisted = await dbContext.Orders
            .IgnoreQueryFilters()
            .SingleAsync(x => x.Id == order.Id);

        Assert.True(persisted.IsDeleted);
        Assert.Equal(OrderStatuses.Cancelled, persisted.Status);
    }

    [Fact]
    public async Task DeleteAsync_CustomerOwnNonPendingOrder_ThrowsForbidden()
    {
        await using var dbContext = CreateDbContext();
        var order = await SeedOrderAsync(dbContext, OrderStatuses.InPreparation, userId: 10, createdByRoleId: (int)RoleIds.Customer);
        var service = CreateService(dbContext);

        var exception = await Assert.ThrowsAsync<AppException>(() =>
            service.DeleteAsync(order.Id, actorUserId: 10, actorRoleId: (int)RoleIds.Customer));

        Assert.Equal("ORDER_DELETE_FORBIDDEN", exception.Code);
        Assert.Equal(403, exception.StatusCode);
    }

    [Fact]
    public async Task CreateAsync_CustomerWithoutReservation_ThrowsReservationRequired()
    {
        await using var dbContext = CreateDbContext();
        var service = CreateService(dbContext);

        var exception = await Assert.ThrowsAsync<AppException>(() =>
            service.CreateAsync(
                actorUserId: 10,
                actorRoleId: (int)RoleIds.Customer,
                tableId: null,
                reservationId: null,
                items: [new CreateOrderItemModel(1, 1, null)]));

        Assert.Equal("ORDER_RESERVATION_REQUIRED", exception.Code);
        Assert.Equal(422, exception.StatusCode);
    }

    [Fact]
    public async Task CreateAsync_CustomerWithReservation_DecrementsStockAndBindsReservation()
    {
        await using var dbContext = CreateDbContext();

        var table = new RestaurantTable { Name = "T1", Capacity = 4, Status = "available" };
        var ingredient = new Ingredient { Name = "Pomodoro", QuantityInStock = 10m, Unit = "kg" };
        var dish = new Dish { Name = "Pasta", Price = 12m, IsAvailable = true };
        var reservation = new Reservation
        {
            UserId = 10,
            Table = table,
            ReservationAtUtc = DateTime.UtcNow.AddHours(2),
            PartySize = 2
        };

        dbContext.Tables.Add(table);
        dbContext.Ingredients.Add(ingredient);
        dbContext.Dishes.Add(dish);
        dbContext.Reservations.Add(reservation);
        await dbContext.SaveChangesAsync();

        dbContext.DishIngredients.Add(new DishIngredient
        {
            DishId = dish.Id,
            IngredientId = ingredient.Id,
            Quantity = 2m
        });
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext);

        var created = await service.CreateAsync(
            actorUserId: 10,
            actorRoleId: (int)RoleIds.Customer,
            tableId: null,
            reservationId: reservation.Id,
            items: [new CreateOrderItemModel(dish.Id, 3, null)]);

        var persistedIngredient = await dbContext.Ingredients.SingleAsync(x => x.Id == ingredient.Id);

        Assert.Equal(reservation.Id, created.ReservationId);
        Assert.Equal(table.Id, created.TableId);
        Assert.Equal(4m, persistedIngredient.QuantityInStock);
    }

    [Fact]
    public async Task CreateAsync_DishWithoutRecipe_ThrowsRecipeMissing()
    {
        await using var dbContext = CreateDbContext();

        var table = new RestaurantTable { Name = "T2", Capacity = 4, Status = "available" };
        var dish = new Dish { Name = "Risotto", Price = 14m, IsAvailable = true };
        var reservation = new Reservation
        {
            UserId = 10,
            Table = table,
            ReservationAtUtc = DateTime.UtcNow.AddHours(1),
            PartySize = 2
        };

        dbContext.Tables.Add(table);
        dbContext.Dishes.Add(dish);
        dbContext.Reservations.Add(reservation);
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext);

        var exception = await Assert.ThrowsAsync<AppException>(() =>
            service.CreateAsync(
                actorUserId: 10,
                actorRoleId: (int)RoleIds.Customer,
                tableId: null,
                reservationId: reservation.Id,
                items: [new CreateOrderItemModel(dish.Id, 1, null)]));

        Assert.Equal("ORDER_RECIPE_MISSING", exception.Code);
        Assert.Equal(422, exception.StatusCode);
    }

    [Fact]
    public async Task CreateAsync_CustomerReservationAlreadyHasOrder_Throws()
    {
        await using var dbContext = CreateDbContext();

        var reservation = new Reservation
        {
            UserId = 10,
            Table = new RestaurantTable { Name = "T3", Capacity = 2, Status = "available" },
            ReservationAtUtc = DateTime.UtcNow.AddHours(2),
            PartySize = 2
        };
        dbContext.Reservations.Add(reservation);
        await dbContext.SaveChangesAsync();

        dbContext.Orders.Add(new Order
        {
            UserId = 10,
            ReservationId = reservation.Id,
            TableId = reservation.TableId,
            CreatedByRoleId = (int)RoleIds.Customer,
            Status = OrderStatuses.Pending
        });
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext);

        var exception = await Assert.ThrowsAsync<AppException>(() =>
            service.CreateAsync(
                actorUserId: 10,
                actorRoleId: (int)RoleIds.Customer,
                tableId: null,
                reservationId: reservation.Id,
                items: [new CreateOrderItemModel(1, 1, null)]));

        Assert.Equal("ORDER_RESERVATION_ALREADY_HAS_ORDER", exception.Code);
        Assert.Equal(422, exception.StatusCode);
    }

    [Fact]
    public async Task UpdateStatusAsync_ToCancelled_RestoresIngredientStock()
    {
        await using var dbContext = CreateDbContext();

        var table = new RestaurantTable { Name = "T4", Capacity = 4, Status = "available" };
        var ingredient = new Ingredient { Name = "Mozzarella", QuantityInStock = 1m, Unit = "kg" };
        var dish = new Dish { Name = "Pizza", Price = 15m, IsAvailable = true };

        dbContext.Tables.Add(table);
        dbContext.Ingredients.Add(ingredient);
        dbContext.Dishes.Add(dish);
        await dbContext.SaveChangesAsync();

        dbContext.DishIngredients.Add(new DishIngredient
        {
            DishId = dish.Id,
            IngredientId = ingredient.Id,
            Quantity = 2m
        });

        var order = new Order
        {
            UserId = 10,
            TableId = table.Id,
            CreatedByRoleId = (int)RoleIds.Customer,
            Status = OrderStatuses.InPreparation,
            Items =
            [
                new OrderItem
                {
                    DishId = dish.Id,
                    Quantity = 2,
                    UnitPrice = dish.Price
                }
            ]
        };

        dbContext.Orders.Add(order);
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext);

        var updated = await service.UpdateStatusAsync(
            order.Id,
            OrderStatuses.Cancelled,
            actorUserId: 30,
            actorRoleId: (int)RoleIds.Sala);

        var persistedIngredient = await dbContext.Ingredients.SingleAsync(x => x.Id == ingredient.Id);

        Assert.Equal(OrderStatuses.Cancelled, updated.Status);
        Assert.Equal(5m, persistedIngredient.QuantityInStock);
    }

    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;

        return new AppDbContext(options);
    }

    private static OrderService CreateService(AppDbContext dbContext)
    {
        return new OrderService(
            new Repository<Order>(dbContext),
            new Repository<Dish>(dbContext),
            new Repository<DishIngredient>(dbContext),
            new Repository<RestaurantTable>(dbContext),
            new Repository<Reservation>(dbContext),
            new Repository<Ingredient>(dbContext),
            new UnitOfWork(dbContext));
    }

    private static async Task<Order> SeedOrderAsync(
        AppDbContext dbContext,
        string status,
        int? userId,
        int createdByRoleId)
    {
        var order = new Order
        {
            UserId = userId,
            CreatedByRoleId = createdByRoleId,
            Status = status,
        };

        dbContext.Orders.Add(order);
        await dbContext.SaveChangesAsync();
        return order;
    }
}
