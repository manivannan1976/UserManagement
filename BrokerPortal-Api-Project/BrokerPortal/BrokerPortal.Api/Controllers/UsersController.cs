using System.Data.Common;
using BrokerPortal.Application.Users;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace BrokerPortal.Api.Controllers;

[ApiController]
[Route("api/users")]
public sealed class UsersController(
    IUserService userService,
    IAccountService accountService,
    ILogger<UsersController> logger,
    IHostEnvironment environment) : ControllerBase
{
    [HttpGet]
    [Authorize(Roles = "PORTAL_SUPER_ADMIN")]
    [ProducesResponseType(typeof(PagedResult<UserListItemDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<PagedResult<UserListItemDto>>> GetUsers(
        [FromQuery] string? search,
        [FromQuery] string? status,
        [FromQuery] string? role,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        if (page < 1)
            return BadRequest(new { message = "Page must be at least 1." });

        if (pageSize < 1 || pageSize > 100)
            return BadRequest(new { message = "Page size must be between 1 and 100." });

        if (page > int.MaxValue / pageSize)
            return BadRequest(new { message = "The requested page is out of range." });

        if (search?.Length > 200)
            return BadRequest(new { message = "Search must not exceed 200 characters." });

        if (status?.Length > 30)
            return BadRequest(new { message = "Status must not exceed 30 characters." });

        if (role?.Length > 50)
            return BadRequest(new { message = "Role must not exceed 50 characters." });

        try
        {
            var result = await userService.GetUsersAsync(
                search, status, role, page, pageSize, cancellationToken);
            return Ok(result);
        }
        catch (DbException exception)
        {
            logger.LogError(exception, "The users query failed because the database is unavailable.");
            return Problem(
                title: "Users are temporarily unavailable.",
                detail: "The database could not process the users request. Please try again later.",
                statusCode: StatusCodes.Status503ServiceUnavailable);
        }
    }

    [HttpPost("invite")]
    [Authorize(Roles = "PORTAL_SUPER_ADMIN")]
    [ValidateAntiForgeryToken]
    [ProducesResponseType(typeof(InvitationCreatedDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<InvitationCreatedDto>> InviteUser(
        [FromBody] InviteUserRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var created = await accountService.CreateInvitationAsync(
                request, GetCurrentUserId(), environment.IsDevelopment(),
                cancellationToken);
            return CreatedAtAction(
                nameof(GetUsers),
                new { page = 1, pageSize = 10 },
                created);
        }
        catch (DuplicateUserEmailException exception)
        {
            return Conflict(new ProblemDetails
            {
                Title = "Email address already exists.",
                Detail = exception.Message,
                Status = StatusCodes.Status409Conflict
            });
        }
        catch (DbUpdateException exception)
        {
            logger.LogError(exception, "Failed to save an invited portal user.");
            return Problem(
                title: "The invitation could not be saved.",
                detail: "The user record could not be saved. Please try again later.",
                statusCode: StatusCodes.Status503ServiceUnavailable);
        }
        catch (DbException exception)
        {
            logger.LogError(exception, "Database failure while inviting a portal user.");
            return Problem(
                title: "The invitation could not be saved.",
                detail: "The database is temporarily unavailable. Please try again later.",
                statusCode: StatusCodes.Status503ServiceUnavailable);
        }
    }

    [HttpPost("{userId:int}/resend-invitation")]
    [Authorize(Roles = "PORTAL_SUPER_ADMIN")]
    [ValidateAntiForgeryToken]
    [ProducesResponseType(typeof(InvitationCreatedDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<InvitationCreatedDto>> ResendInvitation(
        int userId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await accountService.ResendInvitationAsync(
                userId, GetCurrentUserId(),
                environment.IsDevelopment(),
                cancellationToken);
            return Ok(result);
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { message = "User was not found." });
        }
        catch (InvalidOperationException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
        catch (DbUpdateException exception)
        {
            logger.LogError(exception, "Failed to create a replacement invitation.");
            return Problem(
                title: "The invitation could not be resent.",
                statusCode: StatusCodes.Status503ServiceUnavailable);
        }
        catch (DbException exception)
        {
            logger.LogError(exception, "Database failure while resending an invitation.");
            return Problem(
                title: "The invitation could not be resent.",
                statusCode: StatusCodes.Status503ServiceUnavailable);
        }
    }

    private int? GetCurrentUserId() =>
        int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId)
            ? userId
            : null;
}
