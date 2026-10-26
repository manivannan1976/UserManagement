using System.Data.Common;
using BrokerPortal.Application.Users;
using BrokerPortal.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BrokerPortal.Api.Controllers;

[ApiController]
[Route("api")]
[Authorize(Roles = $"{PortalRoles.SuperAdmin},{PortalRoles.BrokerAdmin}")]
public sealed class UsersController(
    IUserService userService,
    IAccountService accountService,
    ILogger<UsersController> logger,
    IHostEnvironment environment) : ControllerBase
{
    private Actor CurrentActor => User.ToActor() ?? throw new ForbiddenOperationException("Not signed in.");

    [HttpGet("users")]
    [ProducesResponseType(typeof(PagedResult<UserListItemDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<PagedResult<UserListItemDto>>> GetUsers(
        [FromQuery] string? search,
        [FromQuery] string? status,
        [FromQuery] string? role,
        [FromQuery] int? agencyId,
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

        return await Run<PagedResult<UserListItemDto>>(async () => Ok(await userService.GetUsersAsync(
            CurrentActor, search, status, role, agencyId, page, pageSize, cancellationToken)),
            "The users query failed because the database is unavailable.");
    }

    [HttpGet("agencies")]
    [ProducesResponseType(typeof(IReadOnlyList<AgencyDto>), StatusCodes.Status200OK)]
    public Task<ActionResult<IReadOnlyList<AgencyDto>>> GetAgencies(
        CancellationToken cancellationToken = default) =>
        Run<IReadOnlyList<AgencyDto>>(async () => Ok(
            await userService.GetAgenciesAsync(CurrentActor, cancellationToken)),
            "The agencies query failed because the database is unavailable.");

    [HttpPost("users/invite")]
    [ValidateAntiForgeryToken]
    [ProducesResponseType(typeof(InvitationCreatedDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public Task<ActionResult<InvitationCreatedDto>> InviteUser(
        [FromBody] InviteUserRequest request,
        CancellationToken cancellationToken = default) =>
        Run<InvitationCreatedDto>(async () =>
        {
            var created = await accountService.CreateInvitationAsync(
                CurrentActor, request, environment.IsDevelopment(), cancellationToken);
            return CreatedAtAction(nameof(GetUsers), new { page = 1, pageSize = 10 }, created);
        }, "Database failure while inviting a portal user.");

    /// <summary>Invites many users in one request. Each row succeeds or fails on its own.</summary>
    [HttpPost("users/invite/bulk")]
    [ValidateAntiForgeryToken]
    [ProducesResponseType(typeof(IReadOnlyList<BulkInviteResultDto>), StatusCodes.Status200OK)]
    public Task<ActionResult<IReadOnlyList<BulkInviteResultDto>>> InviteUsers(
        [FromBody] BulkInviteRequest request,
        CancellationToken cancellationToken = default) =>
        Run<IReadOnlyList<BulkInviteResultDto>>(async () => Ok(
            await accountService.CreateInvitationsAsync(
                CurrentActor, request.Invitations, environment.IsDevelopment(), cancellationToken)),
            "Database failure while bulk-inviting portal users.");

    [HttpPost("users/{userId:int}/resend-invitation")]
    [ValidateAntiForgeryToken]
    [ProducesResponseType(typeof(InvitationCreatedDto), StatusCodes.Status200OK)]
    public Task<ActionResult<InvitationCreatedDto>> ResendInvitation(
        int userId,
        CancellationToken cancellationToken = default) =>
        Run<InvitationCreatedDto>(async () => Ok(
            await accountService.ResendInvitationAsync(
                CurrentActor, userId, environment.IsDevelopment(), cancellationToken)),
            "Database failure while resending an invitation.");

    /// <summary>Deactivation must be explicitly confirmed; it ends portal access immediately.</summary>
    [HttpPost("users/{userId:int}/deactivate")]
    [ValidateAntiForgeryToken]
    [ProducesResponseType(typeof(UserListItemDto), StatusCodes.Status200OK)]
    public Task<ActionResult<UserListItemDto>> Deactivate(
        int userId,
        [FromBody] DeactivateUserRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!request.Confirm)
        {
            return Task.FromResult<ActionResult<UserListItemDto>>(BadRequest(
                new { message = "Deactivation must be confirmed." }));
        }

        return Run<UserListItemDto>(async () => Ok(
            await userService.DeactivateAsync(CurrentActor, userId, cancellationToken)),
            "Database failure while deactivating a user.");
    }

    [HttpPost("users/{userId:int}/reactivate")]
    [ValidateAntiForgeryToken]
    [ProducesResponseType(typeof(UserListItemDto), StatusCodes.Status200OK)]
    public Task<ActionResult<UserListItemDto>> Reactivate(
        int userId,
        CancellationToken cancellationToken = default) =>
        Run<UserListItemDto>(async () => Ok(
            await userService.ReactivateAsync(CurrentActor, userId, cancellationToken)),
            "Database failure while reactivating a user.");

    /// <summary>Maps domain exceptions to HTTP responses so every endpoint behaves consistently.</summary>
    private async Task<ActionResult<T>> Run<T>(Func<Task<ActionResult<T>>> action, string failureLog)
    {
        try
        {
            return await action();
        }
        catch (ForbiddenOperationException exception)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = exception.Message });
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { message = "User was not found." });
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
        catch (BusinessRuleException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
        catch (InvalidOperationException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
        catch (Exception exception) when (exception is DbUpdateException or DbException)
        {
            logger.LogError(exception, "{FailureLog}", failureLog);
            return Problem(
                title: "The request could not be completed.",
                detail: "The database is temporarily unavailable. Please try again later.",
                statusCode: StatusCodes.Status503ServiceUnavailable);
        }
    }
}
