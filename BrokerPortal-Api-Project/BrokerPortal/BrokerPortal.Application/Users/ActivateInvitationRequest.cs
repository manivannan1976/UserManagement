using System.ComponentModel.DataAnnotations;

namespace BrokerPortal.Application.Users;

public sealed class ActivateInvitationRequest : IValidatableObject
{
    [Required]
    public string Token { get; init; } = string.Empty;

    [Required]
    [StringLength(256)]
    public string Password { get; init; } = string.Empty;

    [Required]
    [StringLength(256)]
    public string ConfirmPassword { get; init; } = string.Empty;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (!string.Equals(Password, ConfirmPassword, StringComparison.Ordinal))
        {
            yield return new ValidationResult(
                "Password and confirmation do not match.",
                [nameof(ConfirmPassword)]);
        }

    }
}
