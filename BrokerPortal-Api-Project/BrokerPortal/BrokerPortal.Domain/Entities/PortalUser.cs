namespace BrokerPortal.Domain.Entities;

public sealed class PortalUser
{
    public int Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? JobTitle { get; set; }
    public string Role { get; set; } = "Broker User";
    public string Status { get; set; } = "Invited";
    public string? PasswordHash { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public DateTime? EmailVerifiedAtUtc { get; set; }
    public DateTime? ActivatedAtUtc { get; set; }
    public DateTime? DeactivatedAtUtc { get; set; }
    public int? AgencyId { get; set; }
    public Agency? Agency { get; set; }
    public byte[] RowVersion { get; set; } = [];
    public ICollection<UserInvitation> Invitations { get; set; } = [];
}
