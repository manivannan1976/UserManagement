namespace BrokerPortal.Domain;

public static class PortalRoles
{
    public const string SuperAdmin = "PORTAL_SUPER_ADMIN";
    public const string Underwriter = "UNDERWRITER";
    public const string UnderwriterManager = "UNDERWRITER_MANAGER";
    public const string BrokerAdmin = "BROKER_ADMIN";
    public const string BrokerUser = "BROKER_USER";

    public static readonly IReadOnlySet<string> Internal =
        new HashSet<string> { Underwriter, UnderwriterManager };

    public static readonly IReadOnlySet<string> Broker =
        new HashSet<string> { BrokerAdmin, BrokerUser };

    public static bool IsBroker(string role) => Broker.Contains(role);
    public static bool IsInternal(string role) => Internal.Contains(role);
}

public static class UserStatuses
{
    public const string Invited = "Invited";
    public const string Active = "Active";
    public const string Expired = "Expired";
    public const string Deactivated = "Deactivated";
    public const string SetupIncomplete = "Setup Incomplete";
}
