namespace BrokerPortal.Application.Users;

/// <summary>The caller is not allowed to perform the operation (HTTP 403).</summary>
public sealed class ForbiddenOperationException(string message) : Exception(message);

/// <summary>A business rule rejected the request (HTTP 400/409).</summary>
public sealed class BusinessRuleException(string message) : Exception(message);
