namespace BrokerPortal.Infrastructure.Services;

public sealed class InvitationEmailOptions
{
    public string FrontendBaseUrl { get; set; } = "http://localhost:4200";
    public int LifetimeHours { get; set; } = 168;
    public string SmtpHost { get; set; } = string.Empty;
    public int SmtpPort { get; set; } = 587;
    public bool EnableSsl { get; set; } = true;
    public string SenderAddress { get; set; } = string.Empty;
    public string SenderName { get; set; } = "S|2 Portal";
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public int TimeoutMilliseconds { get; set; } = 10000;
}
