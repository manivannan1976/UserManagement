using System.ComponentModel.DataAnnotations;
using System.Net.Mail;

namespace BrokerPortal.Application.Users;

public sealed class InviteUserRequest : IValidatableObject
{
    // Names are optional on invitation: the invitee confirms them at activation.
    [StringLength(100)]
    public string? FirstName { get; init; }

    [StringLength(100)]
    public string? LastName { get; init; }

    [Required]
    [EmailAddress]
    [StringLength(256)]
    public string Email { get; init; } = string.Empty;

    [StringLength(150)]
    public string? JobTitle { get; init; }

    [Required]
    [RegularExpression("^(BROKER_ADMIN|BROKER_USER|UNDERWRITER|UNDERWRITER_MANAGER)$",
        ErrorMessage = "Role must be BROKER_ADMIN, BROKER_USER, UNDERWRITER or UNDERWRITER_MANAGER.")]
    public string Role { get; init; } = string.Empty;

    /// <summary>Required for Portal super admins inviting broker roles. Broker admins inherit their own agency.</summary>
    public int? AgencyId { get; init; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (string.IsNullOrWhiteSpace(Email))
        {
            yield return new ValidationResult("Email is required.", [nameof(Email)]);
            yield break;
        }

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

public sealed class BulkInviteRequest
{
    [Required]
    [MinLength(1)]
    [MaxLength(500, ErrorMessage = "Invite at most 500 users at a time.")]
    public List<InviteUserRequest> Invitations { get; init; } = [];
}
