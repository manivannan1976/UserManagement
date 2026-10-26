using System.ComponentModel.DataAnnotations;

namespace BrokerPortal.Application.Users;

/// <summary>Self-service profile edit. Email and role are never editable here.</summary>
public sealed class UpdateProfileRequest
{
    [Required]
    [StringLength(100, MinimumLength = 1)]
    public string FirstName { get; init; } = string.Empty;

    [Required]
    [StringLength(100, MinimumLength = 1)]
    public string LastName { get; init; } = string.Empty;

    [StringLength(150)]
    public string? JobTitle { get; init; }
}

public sealed class DeactivateUserRequest
{
    /// <summary>Deactivation must be explicitly confirmed by the caller.</summary>
    public bool Confirm { get; init; }
}
