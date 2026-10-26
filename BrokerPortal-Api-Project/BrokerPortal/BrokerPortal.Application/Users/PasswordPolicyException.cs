namespace BrokerPortal.Application.Users;

public sealed class PasswordPolicyException(string message) : Exception(message)
{
}
