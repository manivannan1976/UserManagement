namespace BrokerPortal.Application.Users;

public sealed record UserListItemDto(
    int Id, string FirstName, string LastName, string Email,
    string? JobTitle, string Role, string Status, DateTime CreatedAt);
