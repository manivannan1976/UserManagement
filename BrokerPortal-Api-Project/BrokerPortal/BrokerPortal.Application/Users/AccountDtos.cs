namespace BrokerPortal.Application.Users;

public sealed record InvitationCreatedDto(
    UserListItemDto User,
    string DeliveryStatus,
    string? Message,
    string? DevelopmentActivationUrl,
    string? DevelopmentEmailPreviewHtml);

public sealed record InvitationValidationDto(
    bool IsValid,
    string? Email,
    string? State,
    string? FirstName = null,
    string? LastName = null);

public sealed record WorkspaceDto(string Key, string Label, string? Url);

public sealed record AuthenticatedUserDto(
    int Id,
    string Email,
    string FirstName,
    string LastName,
    string Role,
    int? AgencyId,
    string? AgencyName,
    WorkspaceDto Workspace,
    string? JobTitle = null);

public sealed record InvitationEmail(
    string Recipient,
    string FirstName,
    string ActivationUrl,
    DateTime ExpiresAtUtc);

public sealed record InvitationDeliveryResult(
    string Status,
    string? Message,
    string? PreviewHtml = null);

public sealed record AgencyDto(
    int Id,
    string Name,
    string? DuckCreekAgencyId,
    IReadOnlyList<string> AcceptedDomains,
    IReadOnlyList<string> Products,
    int? MaxActiveUsers,
    int SeatsUsed);

public sealed record BulkInviteResultDto(
    string Email,
    bool Succeeded,
    string? Error,
    InvitationCreatedDto? Invitation);

/// <summary>The authenticated caller, derived from the session cookie.</summary>
public sealed record Actor(int Id, string Role, int? AgencyId);
