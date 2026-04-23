using GestionaleRistorante.Core.Entities;
using GestionaleRistorante.Core.Exceptions;
using GestionaleRistorante.Core.Interfaces.Repositories;
using GestionaleRistorante.Core.Interfaces.Services;
using Microsoft.EntityFrameworkCore;

namespace GestionaleRistorante.Infrastructure.Services;

public sealed class UserManagementService(
    IRepository<User> userRepository,
    IRepository<Role> roleRepository,
    IPasswordHasherService passwordHasherService,
    IUnitOfWork unitOfWork) : IUserManagementService
{
    public async Task<IReadOnlyList<User>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await userRepository.Query()
            .OrderBy(x => x.Email)
            .ToListAsync(cancellationToken);
    }

    public Task<User?> GetByIdAsync(int userId, CancellationToken cancellationToken = default)
    {
        return userRepository.GetByIdAsync(userId, cancellationToken);
    }

    public async Task<User> CreateAsync(User user, string plainPassword, CancellationToken cancellationToken = default)
    {
        var normalizedEmail = user.Email.Trim().ToLowerInvariant();
        var emailExists = await userRepository.Query().AnyAsync(x => x.Email == normalizedEmail, cancellationToken);
        if (emailExists)
        {
            throw new AppException("USER_EMAIL_ALREADY_EXISTS", "Email already exists.", 409);
        }

        var roleExists = await roleRepository.Query().AnyAsync(x => x.Id == user.RoleId, cancellationToken);
        if (!roleExists)
        {
            throw new AppException("USER_ROLE_NOT_FOUND", "Role not found.", 400);
        }

        user.Email = normalizedEmail;
        user.PasswordHash = passwordHasherService.HashPassword(plainPassword);

        await userRepository.AddAsync(user, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return user;
    }

    public async Task<User> UpdateAsync(int userId, User user, CancellationToken cancellationToken = default)
    {
        var existing = await userRepository.GetByIdAsync(userId, cancellationToken)
            ?? throw new AppException("USER_NOT_FOUND", "User not found.", 404);

        var normalizedEmail = user.Email.Trim().ToLowerInvariant();
        var duplicateEmail = await userRepository.Query().AnyAsync(
            x => x.Id != userId && x.Email == normalizedEmail,
            cancellationToken);
        if (duplicateEmail)
        {
            throw new AppException("USER_EMAIL_ALREADY_EXISTS", "Email already exists.", 409);
        }

        var roleExists = await roleRepository.Query().AnyAsync(x => x.Id == user.RoleId, cancellationToken);
        if (!roleExists)
        {
            throw new AppException("USER_ROLE_NOT_FOUND", "Role not found.", 400);
        }

        existing.Email = normalizedEmail;
        existing.FirstName = user.FirstName;
        existing.LastName = user.LastName;
        existing.RoleId = user.RoleId;
        existing.RequirePasswordChange = user.RequirePasswordChange;

        userRepository.Update(existing);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return existing;
    }

    public async Task DeleteAsync(int userId, CancellationToken cancellationToken = default)
    {
        var user = await userRepository.GetByIdAsync(userId, cancellationToken)
            ?? throw new AppException("USER_NOT_FOUND", "User not found.", 404);

        user.IsDeleted = true;
        userRepository.Update(user);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
