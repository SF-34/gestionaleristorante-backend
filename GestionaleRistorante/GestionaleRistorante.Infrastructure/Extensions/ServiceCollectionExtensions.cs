using GestionaleRistorante.Core.Interfaces.Repositories;
using GestionaleRistorante.Core.Interfaces.Services;
using GestionaleRistorante.Infrastructure.Data;
using GestionaleRistorante.Infrastructure.Options;
using GestionaleRistorante.Infrastructure.Repositories;
using GestionaleRistorante.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace GestionaleRistorante.Infrastructure.Extensions;

public static class ServiceCollectionExtensions
{
    public const string FrontendCorsPolicy = "FrontendCorsPolicy";

    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString =
            configuration.GetConnectionString("DefaultConnection")
            ?? configuration["CONNECTIONSTRINGS__DEFAULT"];

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("Missing database connection string: ConnectionStrings:DefaultConnection");
        }

        services.Configure<JwtOptions>(configuration.GetSection("Jwt"));
        services.Configure<AzureBlobOptions>(configuration.GetSection("AzureBlob"));

        services.AddDbContext<AppDbContext>(options => options.UseSqlServer(connectionString));

        services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        services.AddScoped<IPasswordHasherService, PasswordHasherService>();
        services.AddScoped<IJwtTokenService, JwtTokenService>();
        services.AddScoped<IBlobStorageService, BlobStorageService>();

        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IDishService, DishService>();
        services.AddScoped<IIngredientService, IngredientService>();
        services.AddScoped<ITableService, TableService>();
        services.AddScoped<IReservationService, ReservationService>();
        services.AddScoped<IOrderService, OrderService>();
        services.AddScoped<IUserManagementService, UserManagementService>();

        services.AddScoped<IDatabaseInitializer, DatabaseInitializer>();

        return services;
    }
}
