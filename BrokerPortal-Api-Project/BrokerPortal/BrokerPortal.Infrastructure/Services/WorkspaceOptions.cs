using BrokerPortal.Application.Users;
using BrokerPortal.Domain;

namespace BrokerPortal.Infrastructure.Services;

/// <summary>Duck Creek workspace destinations by user type (requirements section 4.B space routing).</summary>
public sealed class WorkspaceOptions
{
    public string BrokerUrl { get; set; } = string.Empty;
    public string UnderwritingUrl { get; set; } = string.Empty;

    public WorkspaceDto Resolve(string role) => role switch
    {
        PortalRoles.Underwriter or PortalRoles.UnderwriterManager =>
            new WorkspaceDto("DUCK_CREEK_UNDERWRITING", "Duck Creek underwriting workspace",
                NullIfEmpty(UnderwritingUrl)),
        PortalRoles.BrokerAdmin or PortalRoles.BrokerUser =>
            new WorkspaceDto("DUCK_CREEK_BROKER", "Duck Creek broker workspace",
                NullIfEmpty(BrokerUrl)),
        _ => new WorkspaceDto("PORTAL_ADMIN", "Portal administration", null)
    };

    private static string? NullIfEmpty(string value) =>
        string.IsNullOrWhiteSpace(value) ? null : value;
}
