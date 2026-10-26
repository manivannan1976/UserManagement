namespace BrokerPortal.Application.Users;

public sealed class DuplicateUserEmailException : Exception
{
    public DuplicateUserEmailException()
        : base("A user with this email address already exists.")
    {
    }
}
