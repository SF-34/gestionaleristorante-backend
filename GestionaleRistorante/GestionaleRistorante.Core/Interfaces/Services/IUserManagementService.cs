using GestionaleRistorante.Core.Entities;

namespace GestionaleRistorante.Core.Interfaces.Services;

public interface IUserManagementService
{
    Task<IReadOnlyList<User>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<User?> GetByIdAsync(int userId, CancellationToken cancellationToken = default);

    Task<User> CreateAsync(User user, string plainPassword, CancellationToken cancellationToken = default);

    Task<User> UpdateAsync(int userId, User user, CancellationToken cancellationToken = default);

    Task DeleteAsync(int userId, CancellationToken cancellationToken = default);
}
