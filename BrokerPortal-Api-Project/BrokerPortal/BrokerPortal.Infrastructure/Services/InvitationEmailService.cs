using System.Net;
using System.Net.Mail;
using System.Text.Encodings.Web;
using BrokerPortal.Application.Users;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BrokerPortal.Infrastructure.Services;

public sealed class InvitationEmailService(
    IHostEnvironment environment,
    IOptions<InvitationEmailOptions> options,
    ILogger<InvitationEmailService> logger) : IInvitationEmailService
{
    private readonly InvitationEmailOptions settings = options.Value;

    public async Task<InvitationDeliveryResult> SendAsync(
        InvitationEmail email, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(settings.SmtpHost) ||
            string.IsNullOrWhiteSpace(settings.SenderAddress))
        {
            return environment.IsDevelopment()
                ? new InvitationDeliveryResult(
                    "Preview",
                    "Email was not sent. Use the development email preview and activation link.",
                    BuildHtml(email))
                : new InvitationDeliveryResult(
                    "Failed",
                    "Email delivery is not configured. Contact the portal administrator.");
        }

        try
        {
            using var client = new SmtpClient(settings.SmtpHost, settings.SmtpPort)
            {
                EnableSsl = settings.EnableSsl,
                Timeout = settings.TimeoutMilliseconds
            };
            if (!string.IsNullOrWhiteSpace(settings.Username))
            {
                client.Credentials = new NetworkCredential(settings.Username, settings.Password);
            }

            using var message = new MailMessage
            {
                From = new MailAddress(settings.SenderAddress, settings.SenderName),
                Subject = "Your S|2 Portal invitation",
                IsBodyHtml = true,
                Body = BuildHtml(email)
            };
            message.To.Add(new MailAddress(email.Recipient));

            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(settings.TimeoutMilliseconds);
            await client.SendMailAsync(message, timeout.Token);
            return new InvitationDeliveryResult("Sent", null);
        }
        catch (OperationCanceledException exception) when (!cancellationToken.IsCancellationRequested)
        {
            logger.LogError(exception, "Invitation email delivery timed out.");
            return new InvitationDeliveryResult(
                "Failed",
                "The invitation record was saved, but email delivery failed. Please retry the invitation.");
        }
        catch (Exception exception) when (exception is SmtpException or TimeoutException)
        {
            logger.LogError(exception, "Invitation email delivery failed.");
            return new InvitationDeliveryResult(
                "Failed",
                "The invitation record was saved, but email delivery failed. Please retry the invitation.");
        }
    }

    private string BuildHtml(InvitationEmail email)
    {
        var firstName = HtmlEncoder.Default.Encode(email.FirstName);
        var activationUrl = HtmlEncoder.Default.Encode(email.ActivationUrl);
        var expires = email.ExpiresAtUtc.ToString("u");

        return $"""
            <!doctype html>
            <html lang="en">
            <body style="margin:0;background:#f5f7fb;font-family:Arial,sans-serif;color:#172033">
              <div style="max-width:600px;margin:32px auto;padding:32px;background:#fff;border:1px solid #e5e9f1;border-radius:12px">
                <p style="color:#174c70;font-weight:bold;letter-spacing:1px">S|2 PORTAL</p>
                <h1 style="font-size:24px">You are invited</h1>
                <p>Hello {firstName},</p>
                <p>An administrator has invited you to access the S|2 Broker Portal. Activate your account to create a password.</p>
                <p style="margin:28px 0"><a href="{activationUrl}" style="padding:13px 20px;background:#243a83;color:#fff;text-decoration:none;border-radius:6px">Activate Account</a></p>
                <p>This single-use link expires at {expires} UTC. If you were not expecting this invitation, you can ignore this email.</p>
              </div>
            </body>
            </html>
            """;
    }
}
