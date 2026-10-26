namespace BrokerPortal.Application.DuckCreek;

/// <summary>
/// Duck Creek User Admin: creates a user under a Duck Creek entity.
/// Matches POST /ServerPolicy/api/v1/users?UserName=&amp;FullName=&amp;Password=&amp;FailIfExists=&amp;entityId=.
/// </summary>
public interface IDuckCreekUserAdminClient
{
    Task<DuckCreekUserResult> CreateUserAsync(
        DuckCreekCreateUserRequest request, CancellationToken cancellationToken);
}

public sealed record DuckCreekCreateUserRequest(
    string UserName,
    string FullName,
    string Password,
    int EntityId,
    bool FailIfExists = false);

/// <param name="Created">False when the user already existed and FailIfExists was not set.</param>
public sealed record DuckCreekUserResult(string UserName, int EntityId, bool Created);

/// <summary>The user already exists in Duck Creek and FailIfExists was requested.</summary>
public sealed class DuckCreekUserExistsException(string userName)
    : Exception($"Duck Creek user '{userName}' already exists.");

/// <summary>Duck Creek could not be reached or rejected the request.</summary>
public sealed class DuckCreekUnavailableException(string message, Exception? inner = null)
    : Exception(message, inner);
