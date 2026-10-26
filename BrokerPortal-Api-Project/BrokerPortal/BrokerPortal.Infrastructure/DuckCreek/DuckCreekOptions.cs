namespace BrokerPortal.Infrastructure.DuckCreek;

public sealed class DuckCreekOptions
{
    public const string ModeLocal = "Local";
    public const string ModeHttp = "Http";

    /// <summary>"Local" stores users in the portal database; "Http" calls the Duck Creek User Admin API.</summary>
    public string Mode { get; set; } = ModeLocal;

    /// <summary>Duck Creek entity for internal (BHSI) users, who have no broker agency.</summary>
    public int InternalEntityId { get; set; } = 10001;

    // Http mode only. Supply Username/Password through User Secrets or environment variables,
    // never appsettings.json.
    public string BaseUrl { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public int TimeoutSeconds { get; set; } = 30;
}
