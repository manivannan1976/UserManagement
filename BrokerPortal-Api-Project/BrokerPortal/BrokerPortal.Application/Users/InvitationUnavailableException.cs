namespace BrokerPortal.Application.Users;

public sealed class InvitationUnavailableException(string state) : Exception
{
    public string State { get; } = state;
}

public sealed class InvalidCredentialsException : Exception
{
}
