using System.Security.Claims;
using BrokerPortal.Application.Users;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BrokerPortal.Api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController(
    IAccountService accountService,
    IUserService userService,
    IAntiforgery antiforgery) : ControllerBase
{
    [HttpGet("csrf")]
    [AllowAnonymous]
    public IActionResult GetCsrfToken()
    {
        var tokens = antiforgery.GetAndStoreTokens(HttpContext);
        return Ok(new { requestToken = tokens.RequestToken });
    }

    [HttpPost("login")]
    [AllowAnonymous]
    [ServiceFilter(typeof(DevelopmentOnlyAntiforgeryFilter))]
    public async Task<ActionResult<AuthenticatedUserDto>> Login(
        LoginRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var user = await accountService.LoginAsync(request, cancellationToken);
            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new(ClaimTypes.Email, user.Email),
                new(ClaimTypes.Name, $"{user.FirstName} {user.LastName}"),
                new(ClaimTypes.GivenName, user.FirstName),
                new(ClaimTypes.Surname, user.LastName),
                new(ClaimTypes.Role, user.Role)
            };
            if (user.AgencyId is { } agencyId)
            {
                claims.Add(new Claim(ClaimsPrincipalExtensions.AgencyIdClaim, agencyId.ToString()));
            }

            var identity = new ClaimsIdentity(
                claims, CookieAuthenticationDefaults.AuthenticationScheme);

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(identity));
            return Ok(user);
        }
        catch (InvalidCredentialsException)
        {
            return Unauthorized(new { message = "The email or password is incorrect." });
        }
    }

    [HttpGet("session")]
    [Authorize]
    public async Task<ActionResult<AuthenticatedUserDto>> Session(
        CancellationToken cancellationToken)
    {
        if (User.ToActor() is not { } actor)
        {
            return Unauthorized();
        }

        try
        {
            return Ok(await accountService.GetSessionUserAsync(actor.Id, cancellationToken));
        }
        catch (KeyNotFoundException)
        {
            return Unauthorized();
        }
    }

    /// <summary>Lets a signed-in user edit their own name and job title. Email and role are never editable.</summary>
    [HttpPut("profile")]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult<AuthenticatedUserDto>> UpdateProfile(
        [FromBody] UpdateProfileRequest request,
        CancellationToken cancellationToken)
    {
        if (User.ToActor() is not { } actor)
        {
            return Unauthorized();
        }

        try
        {
            return Ok(await userService.UpdateProfileAsync(actor.Id, request, cancellationToken));
        }
        catch (BusinessRuleException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
        catch (KeyNotFoundException)
        {
            return Unauthorized();
        }
    }

    [HttpPost("logout")]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return NoContent();
    }
}
