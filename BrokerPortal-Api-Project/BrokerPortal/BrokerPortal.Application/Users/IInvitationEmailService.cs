namespace BrokerPortal.Application.Users;

public interface IInvitationEmailService
{
    Task<InvitationDeliveryResult> SendAsync(
        InvitationEmail email, CancellationToken cancellationToken);
}
