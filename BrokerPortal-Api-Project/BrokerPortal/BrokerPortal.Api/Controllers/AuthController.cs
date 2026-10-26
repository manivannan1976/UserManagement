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
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Name, $"{user.FirstName} {user.LastName}"),
                new Claim(ClaimTypes.GivenName, user.FirstName),
                new Claim(ClaimTypes.Surname, user.LastName),
                new Claim(ClaimTypes.Role, user.Role)
            };
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
    public ActionResult<AuthenticatedUserDto> Session()
    {
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id))
        {
            return Unauthorized();
        }

        return Ok(new AuthenticatedUserDto(
            id,
            User.FindFirstValue(ClaimTypes.Email) ?? string.Empty,
            User.FindFirstValue(ClaimTypes.GivenName) ?? string.Empty,
            User.FindFirstValue(ClaimTypes.Surname) ?? string.Empty,
            User.FindFirstValue(ClaimTypes.Role) ?? string.Empty));
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
