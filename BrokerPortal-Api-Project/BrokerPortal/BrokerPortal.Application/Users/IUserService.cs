namespace BrokerPortal.Application.Users;

public interface IUserService
{
    Task<PagedResult<UserListItemDto>> GetUsersAsync(
        string? search, string? status, string? role, int page, int pageSize,
        CancellationToken cancellationToken);

}
