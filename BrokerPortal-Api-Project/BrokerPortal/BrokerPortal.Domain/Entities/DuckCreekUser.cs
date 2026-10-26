namespace BrokerPortal.Domain.Entities;

/// <summary>
/// Local stand-in for a Duck Creek User Admin account. Used until the real Duck Creek environment
/// is available; mirrors the fields the Duck Creek create-user API accepts.
/// </summary>
public sealed class DuckCreekUser
{
    public int Id { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;

    /// <summary>Hash of the generated password. Portal users sign in via the portal, not Duck Creek directly.</summary>
    public string PasswordHash { get; set; } = string.Empty;

    public int EntityId { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAtUtc { get; set; }
}
