namespace BrokerPortal.Application.Users;

public interface IUserService
{
    /// <summary>
    /// Lists users visible to <paramref name="actor"/>. Broker admins are always restricted to their
    /// own agency here, regardless of the filters supplied.
    /// </summary>
    Task<PagedResult<UserListItemDto>> GetUsersAsync(
        Actor actor, string? search, string? status, string? role, int? agencyId,
        int page, int pageSize, CancellationToken cancellationToken);

    Task<IReadOnlyList<AgencyDto>> GetAgenciesAsync(
        Actor actor, CancellationToken cancellationToken);

    Task<UserListItemDto> DeactivateAsync(
        Actor actor, int userId, CancellationToken cancellationToken);

    Task<UserListItemDto> ReactivateAsync(
        Actor actor, int userId, CancellationToken cancellationToken);

    Task<AuthenticatedUserDto> UpdateProfileAsync(
        int userId, UpdateProfileRequest request, CancellationToken cancellationToken);
}
