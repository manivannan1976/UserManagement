using BrokerPortal.Domain;
using BrokerPortal.Infrastructure.Data;
using BrokerPortal.Infrastructure.DuckCreek;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace BrokerPortal.Api.Controllers;

/// <summary>
/// Read-only view of the local Duck Creek User Admin store, so administrators can confirm that every
/// portal user was provisioned. Only meaningful while DuckCreek:Mode is "Local".
/// </summary>
[ApiController]
[Route("api/duckcreek/users")]
[Authorize(Roles = PortalRoles.SuperAdmin)]
public sealed class DuckCreekUsersController(
    BrokerPortalDbContext dbContext,
    IOptions<DuckCreekOptions> options) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(CancellationToken cancellationToken)
    {
        if (!string.Equals(options.Value.Mode, DuckCreekOptions.ModeLocal,
                StringComparison.OrdinalIgnoreCase))
        {
            return NotFound(new { message = "Duck Creek users are not stored locally in this mode." });
        }

        // Password hashes are never returned.
        var users = await dbContext.DuckCreekUsers.AsNoTracking()
            .OrderBy(user => user.UserName)
            .Select(user => new
            {
                user.Id, user.UserName, user.FullName, user.EntityId, user.IsActive, user.CreatedAtUtc
            })
            .ToListAsync(cancellationToken);
        return Ok(users);
    }
}
