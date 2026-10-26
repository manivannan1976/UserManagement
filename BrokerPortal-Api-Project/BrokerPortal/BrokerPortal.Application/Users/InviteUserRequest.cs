using System.ComponentModel.DataAnnotations;
using System.Net.Mail;

namespace BrokerPortal.Application.Users;

public sealed class InviteUserRequest : IValidatableObject
{
    [Required]
    [StringLength(100)]
    public string FirstName { get; init; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string LastName { get; init; } = string.Empty;

    [Required]
    [EmailAddress]
    [StringLength(256)]
    public string Email { get; init; } = string.Empty;

    [StringLength(150)]
    public string? JobTitle { get; init; }

    [Required]
    [RegularExpression("^(BROKER_ADMIN|BROKER_USER)$",
        ErrorMessage = "Role must be BROKER_ADMIN or BROKER_USER.")]
    public string Role { get; init; } = string.Empty;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (string.IsNullOrWhiteSpace(FirstName))
        {
            yield return new ValidationResult("First name is required.", [nameof(FirstName)]);
        }

        if (string.IsNullOrWhiteSpace(LastName))
        {
            yield return new ValidationResult("Last name is required.", [nameof(LastName)]);
        }

        if (string.IsNullOrWhiteSpace(Email))
        {
            yield return new ValidationResult("Email is required.", [nameof(Email)]);
        }
        else
        {
            var isValidEmail = true;
            try
            {
                _ = new MailAddress(Email.Trim());
            }
            catch (FormatException)
            {
                isValidEmail = false;
            }

            if (!isValidEmail)
            {
                yield return new ValidationResult("Enter a valid email address.", [nameof(Email)]);
            }
        }
    }
}
