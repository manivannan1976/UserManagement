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
    string? State);

public sealed record AuthenticatedUserDto(
    int Id,
    string Email,
    string FirstName,
    string LastName,
    string Role);

public sealed record InvitationEmail(
    string Recipient,
    string FirstName,
    string ActivationUrl,
    DateTime ExpiresAtUtc);

public sealed record InvitationDeliveryResult(
    string Status,
    string? Message,
    string? PreviewHtml = null);
