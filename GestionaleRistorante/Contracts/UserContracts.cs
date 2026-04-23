using System.ComponentModel.DataAnnotations;

namespace GestionaleRistorante.Contracts;

public sealed class CreateUserRequest
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    [MinLength(8)]
    [MaxLength(128)]
    public string Password { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string LastName { get; set; } = string.Empty;

    [Range(1, 99)]
    public int RoleId { get; set; }

    public bool RequirePasswordChange { get; set; }
}

public sealed class UpdateUserRequest
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string LastName { get; set; } = string.Empty;

    [Range(1, 99)]
    public int RoleId { get; set; }

    public bool RequirePasswordChange { get; set; }
}

public sealed class UserResponse
{
    public int Id { get; set; }

    public string Email { get; set; } = string.Empty;

    public string FirstName { get; set; } = string.Empty;

    public string LastName { get; set; } = string.Empty;

    public int RoleId { get; set; }

    public bool RequirePasswordChange { get; set; }
}
