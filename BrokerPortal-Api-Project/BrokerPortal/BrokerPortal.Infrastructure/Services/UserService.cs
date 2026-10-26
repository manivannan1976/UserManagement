using BrokerPortal.Application.Users;
using BrokerPortal.Domain;
using BrokerPortal.Domain.Entities;
using BrokerPortal.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace BrokerPortal.Infrastructure.Services;

public sealed class UserService(
    BrokerPortalDbContext dbContext,
    IOptions<WorkspaceOptions> workspaceOptions) : IUserService
{
    private readonly WorkspaceOptions workspaces = workspaceOptions.Value;

    public async Task<PagedResult<UserListItemDto>> GetUsersAsync(
        Actor actor, string? search, string? status, string? role, int? agencyId,
        int page, int pageSize, CancellationToken cancellationToken)
    {
        await MarkExpiredInvitationsAsync(cancellationToken);

        var usersQuery = ActorScope.Apply(dbContext.Users.AsNoTracking(), actor);
        var query = usersQuery;

        if (actor.Role == PortalRoles.SuperAdmin && agencyId is { } requestedAgency)
        {
            query = query.Where(user => user.AgencyId == requestedAgency);
        }

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
            .ThenBy(user => user.Email)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(user => new UserListItemDto(
                user.Id, user.FirstName, user.LastName, user.Email,
                user.JobTitle, user.Role, user.Status, user.CreatedAt,
                user.AgencyId, user.Agency != null ? user.Agency.Name : null))
            .ToListAsync(cancellationToken);

        var statusCounts = await usersQuery
            .GroupBy(user => user.Status)
            .Select(group => new { Status = group.Key, Count = group.Count() })
            .ToDictionaryAsync(group => group.Status, group => group.Count, cancellationToken);

        return new PagedResult<UserListItemDto>(
            items, totalCount, page, pageSize, statusCounts);
    }

    public async Task<IReadOnlyList<AgencyDto>> GetAgenciesAsync(
        Actor actor, CancellationToken cancellationToken)
    {
        if (actor.Role != PortalRoles.SuperAdmin && actor.Role != PortalRoles.BrokerAdmin)
        {
            throw new ForbiddenOperationException("You do not have access to agencies.");
        }

        var agencies = dbContext.Agencies.AsNoTracking().Where(agency => agency.IsActive);
        if (actor.Role == PortalRoles.BrokerAdmin)
        {
            var ownAgency = actor.AgencyId;
            agencies = agencies.Where(agency => agency.Id == ownAgency);
        }

        var rows = await agencies.OrderBy(agency => agency.Name).ToListAsync(cancellationToken);
        var seats = await dbContext.Users.AsNoTracking()
            .Where(user => user.AgencyId != null &&
                (user.Status == UserStatuses.Active || user.Status == UserStatuses.Invited))
            .GroupBy(user => user.AgencyId!.Value)
            .Select(group => new { AgencyId = group.Key, Count = group.Count() })
            .ToDictionaryAsync(group => group.AgencyId, group => group.Count, cancellationToken);

        return rows.Select(agency => new AgencyDto(
            agency.Id, agency.Name, agency.DuckCreekAgencyId, agency.GetDomains(),
            agency.Products.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries),
            agency.MaxActiveUsers, seats.GetValueOrDefault(agency.Id))).ToList();
    }

    public async Task<UserListItemDto> DeactivateAsync(
        Actor actor, int userId, CancellationToken cancellationToken)
    {
        var user = await FindManageableAsync(actor, userId, cancellationToken);
        if (user.Status == UserStatuses.Deactivated)
        {
            throw new BusinessRuleException("This user is already deactivated.");
        }

        var now = DateTime.UtcNow;
        user.Status = UserStatuses.Deactivated;
        user.DeactivatedAtUtc = now;
        user.UpdatedAt = now;

        // Outstanding invitations must not be usable after deactivation.
        var open = await dbContext.UserInvitations
            .Where(invitation => invitation.UserId == user.Id &&
                invitation.AcceptedAtUtc == null && invitation.RevokedAtUtc == null)
            .ToListAsync(cancellationToken);
        foreach (var invitation in open)
        {
            invitation.RevokedAtUtc = now;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return ToDto(user);
    }

    public async Task<UserListItemDto> ReactivateAsync(
        Actor actor, int userId, CancellationToken cancellationToken)
    {
        var user = await FindManageableAsync(actor, userId, cancellationToken);
        if (user.Status != UserStatuses.Deactivated)
        {
            throw new BusinessRuleException("Only deactivated users can be reactivated.");
        }

        if (user.ActivatedAtUtc is null)
        {
            throw new BusinessRuleException(
                "This user never completed activation. Resend their invitation instead.");
        }

        if (user.Agency is { MaxActiveUsers: { } limit } agency)
        {
            var seats = await dbContext.Users.CountAsync(candidate =>
                candidate.AgencyId == agency.Id &&
                (candidate.Status == UserStatuses.Active || candidate.Status == UserStatuses.Invited),
                cancellationToken);
            if (seats >= limit)
            {
                throw new BusinessRuleException(
                    $"{agency.Name} has reached its limit of {limit} active users.");
            }
        }

        // The returning employee keeps their existing record and history.
        user.Status = UserStatuses.Active;
        user.DeactivatedAtUtc = null;
        user.UpdatedAt = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
        return ToDto(user);
    }

    public async Task<AuthenticatedUserDto> UpdateProfileAsync(
        int userId, UpdateProfileRequest request, CancellationToken cancellationToken)
    {
        var firstName = request.FirstName.Trim();
        var lastName = request.LastName.Trim();
        if (firstName.Length == 0 || lastName.Length == 0)
        {
            throw new BusinessRuleException("First and last name are required.");
        }

        var user = await dbContext.Users.Include(candidate => candidate.Agency)
            .SingleOrDefaultAsync(candidate => candidate.Id == userId, cancellationToken)
            ?? throw new KeyNotFoundException("User was not found.");

        // Only name and job title can change. Email and role are deliberately untouched.
        user.FirstName = firstName;
        user.LastName = lastName;
        user.JobTitle = string.IsNullOrWhiteSpace(request.JobTitle) ? null : request.JobTitle.Trim();
        user.UpdatedAt = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
        return AccountMapper.ToAuthenticated(user, workspaces);
    }

    private async Task<PortalUser> FindManageableAsync(
        Actor actor, int userId, CancellationToken cancellationToken)
    {
        // Out-of-scope users look identical to missing ones so IDs cannot be probed.
        var user = await ActorScope.Apply(
                dbContext.Users.Include(candidate => candidate.Agency), actor)
            .SingleOrDefaultAsync(candidate => candidate.Id == userId, cancellationToken)
            ?? throw new KeyNotFoundException("User was not found.");
        ActorScope.EnsureCanManage(actor, user);
        return user;
    }

    /// <summary>Invited users whose every invitation has lapsed become Expired (requirements 4.D).</summary>
    private Task<int> MarkExpiredInvitationsAsync(CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        return dbContext.Users
            .Where(user => user.Status == UserStatuses.Invited &&
                !user.Invitations.Any(invitation =>
                    invitation.AcceptedAtUtc == null &&
                    invitation.RevokedAtUtc == null &&
                    invitation.ExpiresAtUtc > now))
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(user => user.Status, UserStatuses.Expired)
                .SetProperty(user => user.UpdatedAt, now), cancellationToken);
    }

    internal static UserListItemDto ToDto(PortalUser user) =>
        new(user.Id, user.FirstName, user.LastName, user.Email, user.JobTitle,
            user.Role, user.Status, user.CreatedAt, user.AgencyId, user.Agency?.Name);
}

internal static class AccountMapper
{
    public static AuthenticatedUserDto ToAuthenticated(PortalUser user, WorkspaceOptions workspaces) =>
        new(user.Id, user.Email, user.FirstName, user.LastName, user.Role,
            user.AgencyId, user.Agency?.Name, workspaces.Resolve(user.Role), user.JobTitle);
}
