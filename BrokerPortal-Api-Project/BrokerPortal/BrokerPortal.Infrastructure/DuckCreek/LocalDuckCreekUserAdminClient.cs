using BrokerPortal.Application.DuckCreek;
using BrokerPortal.Domain.Entities;
using BrokerPortal.Infrastructure.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace BrokerPortal.Infrastructure.DuckCreek;

/// <summary>
/// Local-database implementation of Duck Creek User Admin. Behaves like the real API (including
/// FailIfExists) so the portal can be built and tested without a Duck Creek environment.
/// </summary>
public sealed class LocalDuckCreekUserAdminClient(
    BrokerPortalDbContext dbContext,
    IPasswordHasher<DuckCreekUser> passwordHasher) : IDuckCreekUserAdminClient
{
    public async Task<DuckCreekUserResult> CreateUserAsync(
        DuckCreekCreateUserRequest request, CancellationToken cancellationToken)
    {
        var userName = request.UserName.Trim();
        var existing = await dbContext.DuckCreekUsers
            .AnyAsync(user => user.UserName == userName, cancellationToken);
        if (existing)
        {
            return request.FailIfExists
                ? throw new DuckCreekUserExistsException(userName)
                : new DuckCreekUserResult(userName, request.EntityId, Created: false);
        }

        var user = new DuckCreekUser
        {
            UserName = userName,
            FullName = request.FullName.Trim(),
            EntityId = request.EntityId,
            CreatedAtUtc = DateTime.UtcNow
        };
        user.PasswordHash = passwordHasher.HashPassword(user, request.Password);
        dbContext.DuckCreekUsers.Add(user);

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception)
        {
            dbContext.Entry(user).State = EntityState.Detached;
            throw new DuckCreekUnavailableException(
                "The local Duck Creek user store could not save the user.", exception);
        }

        return new DuckCreekUserResult(userName, request.EntityId, Created: true);
    }
}
