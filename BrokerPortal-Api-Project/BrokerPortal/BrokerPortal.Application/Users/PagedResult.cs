namespace BrokerPortal.Application.Users;

public sealed record PagedResult<T>(
    IReadOnlyList<T> Users,
    int TotalCount,
    int Page,
    int PageSize,
    IReadOnlyDictionary<string, int> StatusCounts);
