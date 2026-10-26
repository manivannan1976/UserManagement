using BrokerPortal.Application.Users;
using BrokerPortal.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace BrokerPortal.Infrastructure.Services;

public sealed class UserService(BrokerPortalDbContext dbContext) : IUserService
{
    public async Task<PagedResult<UserListItemDto>> GetUsersAsync(
        string? search, string? status, string? role, int page, int pageSize,
        CancellationToken cancellationToken)
    {
        var usersQuery = dbContext.Users.AsNoTracking();
        var query = usersQuery;

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(user =>
                user.FirstName.Contains(term) ||
                user.LastName.Contains(term) ||
                (user.FirstName + " " + user.LastName).Contains(term) ||
                user.Email.Contains(term));
        }

        if (!string.IsNullOrWhiteSpace(status) &&
            !status.Equals("All", StringComparison.OrdinalIgnoreCase))
        {
            query = query.Where(user => user.Status == status);
        }

        if (!string.IsNullOrWhiteSpace(role) &&
            !role.Equals("All", StringComparison.OrdinalIgnoreCase))
        {
            query = query.Where(user => user.Role == role);
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderBy(user => user.LastName)
            .ThenBy(user => user.FirstName)
            .ThenBy(user => user.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(user => new UserListItemDto(
                user.Id, user.FirstName, user.LastName, user.Email,
                user.JobTitle, user.Role, user.Status, user.CreatedAt))
            .ToListAsync(cancellationToken);

        var statusCounts = await usersQuery
            .GroupBy(user => user.Status)
            .Select(group => new { Status = group.Key, Count = group.Count() })
            .ToDictionaryAsync(group => group.Status, group => group.Count, cancellationToken);

        return new PagedResult<UserListItemDto>(
            items, totalCount, page, pageSize, statusCounts);
    }

}
