namespace BrokerPortal.Application.Users;

public interface IAccountService
{
    Task<InvitationCreatedDto> CreateInvitationAsync(
        Actor actor, InviteUserRequest request, bool includePreviewLink,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<BulkInviteResultDto>> CreateInvitationsAsync(
        Actor actor, IReadOnlyList<InviteUserRequest> requests, bool includePreviewLink,
        CancellationToken cancellationToken);

    Task<InvitationValidationDto> ValidateInvitationAsync(
        string token, CancellationToken cancellationToken);

    Task ActivateInvitationAsync(
        ActivateInvitationRequest request, CancellationToken cancellationToken);

    Task<InvitationCreatedDto> ResendInvitationAsync(
        Actor actor, int userId, bool includePreviewLink,
        CancellationToken cancellationToken);

    Task<AuthenticatedUserDto> LoginAsync(
        LoginRequest request, CancellationToken cancellationToken);

    Task<AuthenticatedUserDto> GetSessionUserAsync(
        int userId, CancellationToken cancellationToken);
}
