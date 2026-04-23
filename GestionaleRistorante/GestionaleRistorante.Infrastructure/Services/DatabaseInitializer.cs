using GestionaleRistorante.Core.Entities;
using GestionaleRistorante.Core.Enums;
using GestionaleRistorante.Core.Interfaces.Services;
using GestionaleRistorante.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace GestionaleRistorante.Infrastructure.Services;

public sealed class DatabaseInitializer(
    AppDbContext dbContext,
    IConfiguration configuration,
    IPasswordHasherService passwordHasherService) : IDatabaseInitializer
{
    private sealed record SeedUser(string Email, string Password, int RoleId, string FirstName, string LastName, bool RequirePasswordChange);

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await dbContext.Database.MigrateAsync(cancellationToken);
        }
        catch (InvalidOperationException)
        {
            await dbContext.Database.EnsureCreatedAsync(cancellationToken);
        }

        var adminEmail = (configuration["SEED_ADMIN_EMAIL"] ?? "admin@ristorante.local").Trim().ToLowerInvariant();
        var adminPassword = configuration["SEED_ADMIN_PASSWORD"] ?? "ChangeThisPassword!123";
        var seedAdminUpdateExisting = !bool.TryParse(
            configuration["SEED_ADMIN_UPDATE_EXISTING"],
            out var updateExisting) || updateExisting;
        var seedDefaultUsersUpdateExisting = !bool.TryParse(
            configuration["SEED_DEFAULT_USERS_UPDATE_EXISTING"],
            out var updateDefaultUsersExisting) || updateDefaultUsersExisting;

        var usersToSeed = new[]
        {
            new SeedUser(
                adminEmail,
                adminPassword,
                (int)RoleIds.Admin,
                "System",
                "Admin",
                true),
            new SeedUser(
                (configuration["SEED_KITCHEN_EMAIL"] ?? "kitchen@ristorante.local").Trim().ToLowerInvariant(),
                configuration["SEED_KITCHEN_PASSWORD"] ?? "KitchenPass!123",
                (int)RoleIds.Kitchen,
                "Kitchen",
                "Staff",
                false),
            new SeedUser(
                (configuration["SEED_SALA_EMAIL"] ?? "sala@ristorante.local").Trim().ToLowerInvariant(),
                configuration["SEED_SALA_PASSWORD"] ?? "SalaPass!123",
                (int)RoleIds.Sala,
                "Sala",
                "Staff",
                false),
            new SeedUser(
                (configuration["SEED_CUSTOMER_EMAIL"] ?? "test@cliente.com").Trim().ToLowerInvariant(),
                configuration["SEED_CUSTOMER_PASSWORD"] ?? "ClientePass!123",
                (int)RoleIds.Customer,
                "Test",
                "Cliente",
                false)
        };

        foreach (var seedUser in usersToSeed)
        {
            if (string.IsNullOrWhiteSpace(seedUser.Email) || string.IsNullOrWhiteSpace(seedUser.Password))
            {
                throw new InvalidOperationException("Seed user configuration is invalid: email and password are required.");
            }

            var existingUser = await dbContext.Users
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(x => x.Email == seedUser.Email, cancellationToken);

            var shouldUpdateExisting = seedUser.RoleId == (int)RoleIds.Admin
                ? seedAdminUpdateExisting
                : seedDefaultUsersUpdateExisting;

            if (existingUser is null)
            {
                dbContext.Users.Add(new User
                {
                    Email = seedUser.Email,
                    PasswordHash = passwordHasherService.HashPassword(seedUser.Password),
                    FirstName = seedUser.FirstName,
                    LastName = seedUser.LastName,
                    RoleId = seedUser.RoleId,
                    RequirePasswordChange = seedUser.RequirePasswordChange
                });

                continue;
            }

            if (!shouldUpdateExisting)
            {
                continue;
            }

            var mustUpdatePassword = !passwordHasherService.VerifyPassword(existingUser.PasswordHash, seedUser.Password);
            var mustRestoreUser = existingUser.IsDeleted;
            var mustUpdateMetadata = existingUser.RoleId != seedUser.RoleId
                || existingUser.FirstName != seedUser.FirstName
                || existingUser.LastName != seedUser.LastName
                || existingUser.RequirePasswordChange != seedUser.RequirePasswordChange;

            if (!mustUpdatePassword && !mustRestoreUser && !mustUpdateMetadata)
            {
                continue;
            }

            existingUser.PasswordHash = passwordHasherService.HashPassword(seedUser.Password);
            existingUser.FirstName = seedUser.FirstName;
            existingUser.LastName = seedUser.LastName;
            existingUser.RoleId = seedUser.RoleId;
            existingUser.RequirePasswordChange = seedUser.RequirePasswordChange;
            existingUser.IsDeleted = false;

            dbContext.Users.Update(existingUser);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
