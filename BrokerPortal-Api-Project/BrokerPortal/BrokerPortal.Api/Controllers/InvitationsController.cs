using BrokerPortal.Application.Users;
using Microsoft.AspNetCore.Mvc;

namespace BrokerPortal.Api.Controllers;

[ApiController]
[Route("api/invitations")]
public sealed class InvitationsController(IAccountService accountService) : ControllerBase
{
    [HttpGet("validate")]
    public async Task<ActionResult<InvitationValidationDto>> Validate(
        [FromQuery] string? token,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(token) || token.Length > 200)
        {
            return Ok(new InvitationValidationDto(false, null, "invalid"));
        }

        return Ok(await accountService.ValidateInvitationAsync(token, cancellationToken));
    }

    [HttpPost("activate")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Activate(
        ActivateInvitationRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            await accountService.ActivateInvitationAsync(request, cancellationToken);
            return NoContent();
        }
        catch (PasswordPolicyException exception)
        {
            return BadRequest(new
            {
                message = exception.Message
            });
        }
        catch (InvitationUnavailableException exception)
        {
            return Conflict(new
            {
                code = exception.State,
                message = exception.State switch
                {
                    "expired" => "This invitation has expired. Ask your administrator to resend it.",
                    "used" => "This invitation has already been used.",
                    "revoked" => "This invitation has been revoked.",
                    "unavailable" => "This account is not eligible for activation.",
                    _ => "This invitation link is invalid."
                }
            });
        }
    }
}
