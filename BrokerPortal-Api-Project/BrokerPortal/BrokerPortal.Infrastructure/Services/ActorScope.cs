using BrokerPortal.Application.Users;
using BrokerPortal.Domain;
using BrokerPortal.Domain.Entities;

namespace BrokerPortal.Infrastructure.Services;

/// <summary>
/// Server-side visibility rules. Broker admins only ever see and manage users in their own agency;
/// this is enforced in queries, not only in the UI.
/// </summary>
internal static class ActorScope
{
    public static IQueryable<PortalUser> Apply(IQueryable<PortalUser> users, Actor actor)
    {
        if (actor.Role == PortalRoles.SuperAdmin)
        {
            return users;
        }

        if (actor.Role == PortalRoles.BrokerAdmin && actor.AgencyId is { } agencyId)
        {
            return users.Where(user => user.AgencyId == agencyId);
        }

        throw new ForbiddenOperationException("You do not have access to user administration.");
    }

    /// <summary>Whether the actor may administer (resend, deactivate, reactivate) the target user.</summary>
    public static void EnsureCanManage(Actor actor, PortalUser target)
    {
        if (target.Id == actor.Id)
        {
            throw new BusinessRuleException("You cannot change your own account status.");
        }

        if (target.Role == PortalRoles.SuperAdmin && actor.Role != PortalRoles.SuperAdmin)
        {
            throw new ForbiddenOperationException("You cannot manage this user.");
        }

        if (actor.Role == PortalRoles.SuperAdmin)
        {
            return;
        }

        if (actor.Role == PortalRoles.BrokerAdmin &&
            actor.AgencyId is { } agencyId && target.AgencyId == agencyId)
        {
            return;
        }

        throw new ForbiddenOperationException("You cannot manage users outside your own agency.");
    }
}
