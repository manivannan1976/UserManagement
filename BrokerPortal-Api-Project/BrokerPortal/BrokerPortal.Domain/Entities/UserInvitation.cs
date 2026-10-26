namespace BrokerPortal.Domain.Entities;

public sealed class UserInvitation
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public int UserId { get; set; }
    public string TokenHash { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }
    public DateTime ExpiresAtUtc { get; set; }
    public DateTime? AcceptedAtUtc { get; set; }
    public DateTime? RevokedAtUtc { get; set; }
    public int? InvitedByUserId { get; set; }
    public byte[] RowVersion { get; set; } = [];
    public PortalUser User { get; set; } = null!;
}
