using System.Security.Claims;
using BrokerPortal.Application.Users;

namespace BrokerPortal.Api;

public static class ClaimsPrincipalExtensions
{
    public const string AgencyIdClaim = "agency_id";

    public static Actor? ToActor(this ClaimsPrincipal principal)
    {
        if (!int.TryParse(principal.FindFirstValue(ClaimTypes.NameIdentifier), out var id))
        {
            return null;
        }

        int? agencyId = int.TryParse(principal.FindFirstValue(AgencyIdClaim), out var agency)
            ? agency
            : null;
        return new Actor(id, principal.FindFirstValue(ClaimTypes.Role) ?? string.Empty, agencyId);
    }
}
