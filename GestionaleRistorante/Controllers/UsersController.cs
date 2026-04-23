using GestionaleRistorante.Contracts;
using GestionaleRistorante.Core.Entities;
using GestionaleRistorante.Core.Exceptions;
using GestionaleRistorante.Core.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GestionaleRistorante.Controllers;

[Authorize(Roles = "99")]
[Route("api/v1/users")]
public sealed class UsersController(IUserManagementService userManagementService) : ApiControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<UserResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var users = await userManagementService.GetAllAsync(cancellationToken);
        return Success(users.Select(ToResponse).ToList());
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(UserResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken)
    {
        var user = await userManagementService.GetByIdAsync(id, cancellationToken)
            ?? throw new AppException("USER_NOT_FOUND", "User not found.", 404);

        return Success(ToResponse(user));
    }

    [HttpPost]
    [ProducesResponseType(typeof(UserResponse), StatusCodes.Status201Created)]
    public async Task<IActionResult> Create([FromBody] CreateUserRequest request, CancellationToken cancellationToken)
    {
        var user = new User
        {
            Email = request.Email,
            FirstName = request.FirstName,
            LastName = request.LastName,
            RoleId = request.RoleId,
            RequirePasswordChange = request.RequirePasswordChange
        };

        var created = await userManagementService.CreateAsync(user, request.Password, cancellationToken);
        return Success(ToResponse(created), StatusCodes.Status201Created);
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(UserResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateUserRequest request, CancellationToken cancellationToken)
    {
        var user = new User
        {
            Email = request.Email,
            FirstName = request.FirstName,
            LastName = request.LastName,
            RoleId = request.RoleId,
            RequirePasswordChange = request.RequirePasswordChange
        };

        var updated = await userManagementService.UpdateAsync(id, user, cancellationToken);
        return Success(ToResponse(updated));
    }

    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        await userManagementService.DeleteAsync(id, cancellationToken);
        return SuccessNoData();
    }

    private static UserResponse ToResponse(User user)
    {
        return new UserResponse
        {
            Id = user.Id,
            Email = user.Email,
            FirstName = user.FirstName,
            LastName = user.LastName,
            RoleId = user.RoleId,
            RequirePasswordChange = user.RequirePasswordChange
        };
    }
}
