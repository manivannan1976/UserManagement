namespace BrokerPortal.Application.Users;

public interface IAccountService
{
    Task<InvitationCreatedDto> CreateInvitationAsync(
        InviteUserRequest request, int? invitedByUserId, bool includePreviewLink,
        CancellationToken cancellationToken);

    Task<InvitationValidationDto> ValidateInvitationAsync(
        string token, CancellationToken cancellationToken);

    Task ActivateInvitationAsync(
        ActivateInvitationRequest request, CancellationToken cancellationToken);

    Task<InvitationCreatedDto> ResendInvitationAsync(
        int userId, int? invitedByUserId, bool includePreviewLink,
        CancellationToken cancellationToken);

    Task<AuthenticatedUserDto> LoginAsync(
        LoginRequest request, CancellationToken cancellationToken);
}
