using GestionaleRistorante.Core.Entities;
using GestionaleRistorante.Core.Enums;
using Microsoft.EntityFrameworkCore;

namespace GestionaleRistorante.Infrastructure.Data;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Role> Roles => Set<Role>();

    public DbSet<User> Users => Set<User>();

    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    public DbSet<RestaurantTable> Tables => Set<RestaurantTable>();

    public DbSet<Reservation> Reservations => Set<Reservation>();

    public DbSet<Dish> Dishes => Set<Dish>();

    public DbSet<Ingredient> Ingredients => Set<Ingredient>();

    public DbSet<DishIngredient> DishIngredients => Set<DishIngredient>();

    public DbSet<Order> Orders => Set<Order>();

    public DbSet<OrderItem> OrderItems => Set<OrderItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Role>(entity =>
        {
            entity.ToTable("Roles");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Name).IsRequired().HasMaxLength(50);
            entity.HasData(
                new Role { Id = (int)RoleIds.Customer, Name = "Customer" },
                new Role { Id = (int)RoleIds.Kitchen, Name = "Kitchen" },
                new Role { Id = (int)RoleIds.Sala, Name = "Sala" },
                new Role { Id = (int)RoleIds.Admin, Name = "Admin" });
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("Users");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Email).IsRequired().HasMaxLength(256);
            entity.Property(x => x.PasswordHash).IsRequired().HasMaxLength(512);
            entity.Property(x => x.FirstName).IsRequired().HasMaxLength(100);
            entity.Property(x => x.LastName).IsRequired().HasMaxLength(100);
            entity.HasIndex(x => x.Email).IsUnique();
            entity.HasOne(x => x.Role)
                .WithMany(x => x.Users)
                .HasForeignKey(x => x.RoleId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasQueryFilter(x => !x.IsDeleted);
        });

        modelBuilder.Entity<RefreshToken>(entity =>
        {
            entity.ToTable("RefreshTokens");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.TokenHash).IsRequired().HasMaxLength(128);
            entity.HasIndex(x => x.TokenHash).IsUnique();
            entity.HasIndex(x => new { x.UserId, x.IsRevoked });
            entity.HasOne(x => x.User)
                .WithMany(x => x.RefreshTokens)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<RestaurantTable>(entity =>
        {
            entity.ToTable("Tables");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Name).IsRequired().HasMaxLength(100);
            entity.Property(x => x.Status).IsRequired().HasMaxLength(30);
            entity.HasIndex(x => x.Name).IsUnique();
            entity.ToTable(t => t.HasCheckConstraint("CK_Tables_Capacity", "[Capacity] > 0"));
            entity.HasQueryFilter(x => !x.IsDeleted);
        });

        modelBuilder.Entity<Reservation>(entity =>
        {
            entity.ToTable("Reservations");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.PartySize).IsRequired();
            entity.Property(x => x.Notes).HasMaxLength(1000);
            entity.HasIndex(x => x.UserId);
            entity.HasIndex(x => x.TableId);
            entity.HasIndex(x => x.ReservationAtUtc);
            entity.HasOne(x => x.User)
                .WithMany(x => x.Reservations)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Table)
                .WithMany(x => x.Reservations)
                .HasForeignKey(x => x.TableId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasQueryFilter(x => !x.IsDeleted);
        });

        modelBuilder.Entity<Dish>(entity =>
        {
            entity.ToTable("Dishes");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Name).IsRequired().HasMaxLength(150);
            entity.Property(x => x.Description).HasMaxLength(2000);
            entity.Property(x => x.ImageUrl).HasMaxLength(1000);
            entity.Property(x => x.Price).HasPrecision(18, 2);
            entity.HasIndex(x => x.Name);
            entity.HasIndex(x => x.IsDeleted);
            entity.HasQueryFilter(x => !x.IsDeleted);
        });

        modelBuilder.Entity<Ingredient>(entity =>
        {
            entity.ToTable("Ingredients");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Name).IsRequired().HasMaxLength(150);
            entity.Property(x => x.Unit).IsRequired().HasMaxLength(30);
            entity.Property(x => x.QuantityInStock).HasPrecision(18, 3);
            entity.HasIndex(x => x.Name).IsUnique();
            entity.HasIndex(x => x.IsDeleted);
            entity.HasQueryFilter(x => !x.IsDeleted);
        });

        modelBuilder.Entity<DishIngredient>(entity =>
        {
            entity.ToTable("DishIngredients");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Quantity).HasPrecision(18, 3);
            entity.HasIndex(x => new { x.DishId, x.IngredientId }).IsUnique();
            entity.HasOne(x => x.Dish)
                .WithMany(x => x.DishIngredients)
                .HasForeignKey(x => x.DishId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.Ingredient)
                .WithMany(x => x.DishIngredients)
                .HasForeignKey(x => x.IngredientId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Order>(entity =>
        {
            entity.ToTable("Orders");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Status).IsRequired().HasMaxLength(50);
            entity.HasIndex(x => x.Status);
            entity.HasIndex(x => x.CreatedAt);
            entity.HasIndex(x => x.TableId);
            entity.HasIndex(x => x.ReservationId);
            entity.ToTable(t => t.HasCheckConstraint(
                "CK_Orders_Status",
                "[Status] IN ('pending','in_preparation','ready','served','cancelled')"));
            entity.HasOne(x => x.User)
                .WithMany(x => x.Orders)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Table)
                .WithMany(x => x.Orders)
                .HasForeignKey(x => x.TableId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Reservation)
                .WithMany(x => x.Orders)
                .HasForeignKey(x => x.ReservationId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasQueryFilter(x => !x.IsDeleted);
        });

        modelBuilder.Entity<OrderItem>(entity =>
        {
            entity.ToTable("OrderItems");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.UnitPrice).HasPrecision(18, 2);
            entity.Property(x => x.Notes).HasMaxLength(1000);
            entity.HasIndex(x => x.OrderId);
            entity.HasOne(x => x.Order)
                .WithMany(x => x.Items)
                .HasForeignKey(x => x.OrderId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.Dish)
                .WithMany(x => x.OrderItems)
                .HasForeignKey(x => x.DishId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        ApplyAuditing();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        ApplyAuditing();
        return base.SaveChangesAsync(cancellationToken);
    }

    private void ApplyAuditing()
    {
        var utcNow = DateTime.UtcNow;

        foreach (var entry in ChangeTracker.Entries<SoftDeletableEntity>())
        {
            if (entry.State != EntityState.Deleted)
            {
                continue;
            }

            entry.State = EntityState.Modified;
            entry.Entity.IsDeleted = true;
            entry.Entity.UpdatedAt = utcNow;
        }

        foreach (var entry in ChangeTracker.Entries<AuditableEntity>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAt = utcNow;
                entry.Entity.UpdatedAt = utcNow;
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedAt = utcNow;
            }
        }
    }
}
