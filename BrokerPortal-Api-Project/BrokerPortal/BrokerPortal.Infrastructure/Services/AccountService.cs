using System.Data;
using System.Security.Cryptography;
using System.Text;
using BrokerPortal.Application.DuckCreek;
using BrokerPortal.Application.Users;
using BrokerPortal.Domain;
using BrokerPortal.Domain.Entities;
using BrokerPortal.Infrastructure.Data;
using BrokerPortal.Infrastructure.DuckCreek;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace BrokerPortal.Infrastructure.Services;

public sealed class AccountService(
    BrokerPortalDbContext dbContext,
    IPasswordHasher<PortalUser> passwordHasher,
    IInvitationEmailService emailService,
    IOptions<InvitationEmailOptions> emailOptions,
    IOptions<ActivationPasswordOptions> passwordOptions,
    IOptions<WorkspaceOptions> workspaceOptions,
    IDuckCreekUserAdminClient duckCreek,
    IOptions<DuckCreekOptions> duckCreekOptions) : IAccountService
{
    private readonly DuckCreekOptions duckCreekSettings = duckCreekOptions.Value;
    private readonly InvitationEmailOptions settings = emailOptions.Value;
    private readonly ActivationPasswordOptions passwordPolicy = passwordOptions.Value;
    private readonly WorkspaceOptions workspaces = workspaceOptions.Value;

    public async Task<InvitationCreatedDto> CreateInvitationAsync(
        Actor actor, InviteUserRequest request, bool includePreviewLink,
        CancellationToken cancellationToken)
    {
        var email = NormalizeEmail(request.Email);
        var agency = await ResolveInviteAgencyAsync(actor, request, email, cancellationToken);

        if (await dbContext.Users.AnyAsync(
                user => user.Email.ToLower() == email, cancellationToken))
        {
            throw new DuplicateUserEmailException();
        }

        var user = new PortalUser
        {
            FirstName = request.FirstName?.Trim() ?? string.Empty,
            LastName = request.LastName?.Trim() ?? string.Empty,
            Email = email,
            JobTitle = string.IsNullOrWhiteSpace(request.JobTitle) ? null : request.JobTitle.Trim(),
            Role = request.Role,
            Status = UserStatuses.Invited,
            CreatedAt = DateTime.UtcNow,
            Agency = agency
        };

        dbContext.Users.Add(user);
        return await AddInvitationAsync(user, actor.Id, includePreviewLink, cancellationToken);
    }

    public async Task<IReadOnlyList<BulkInviteResultDto>> CreateInvitationsAsync(
        Actor actor, IReadOnlyList<InviteUserRequest> requests, bool includePreviewLink,
        CancellationToken cancellationToken)
    {
        var results = new List<BulkInviteResultDto>(requests.Count);
        foreach (var request in requests)
        {
            try
            {
                var created = await CreateInvitationAsync(
                    actor, request, includePreviewLink, cancellationToken);
                results.Add(new BulkInviteResultDto(request.Email, true, null, created));
            }
            catch (DuplicateUserEmailException exception)
            {
                dbContext.ChangeTracker.Clear();
                results.Add(new BulkInviteResultDto(request.Email, false, exception.Message, null));
            }
            catch (BusinessRuleException exception)
            {
                dbContext.ChangeTracker.Clear();
                results.Add(new BulkInviteResultDto(request.Email, false, exception.Message, null));
            }
        }

        return results;
    }

    public async Task<InvitationCreatedDto> ResendInvitationAsync(
        Actor actor, int userId, bool includePreviewLink,
        CancellationToken cancellationToken)
    {
        var user = await ActorScope.Apply(
                dbContext.Users.Include(candidate => candidate.Agency), actor)
            .SingleOrDefaultAsync(candidate => candidate.Id == userId, cancellationToken)
            ?? throw new KeyNotFoundException("User was not found.");
        ActorScope.EnsureCanManage(actor, user);

        // Expired invitations can be resent without re-entering the user's data.
        if (user.Status != UserStatuses.Invited && user.Status != UserStatuses.Expired &&
            user.Status != UserStatuses.SetupIncomplete)
        {
            throw new InvalidOperationException(
                "Only invited, expired or setup-incomplete users can receive a new invitation.");
        }

        var now = DateTime.UtcNow;
        var previous = await dbContext.UserInvitations
            .Where(invitation => invitation.UserId == user.Id &&
                invitation.AcceptedAtUtc == null && invitation.RevokedAtUtc == null)
            .ToListAsync(cancellationToken);
        foreach (var invitation in previous)
        {
            invitation.RevokedAtUtc = now;
        }

        user.Status = UserStatuses.Invited;
        user.UpdatedAt = now;
        return await AddInvitationAsync(user, actor.Id, includePreviewLink, cancellationToken);
    }

    public async Task<InvitationValidationDto> ValidateInvitationAsync(
        string token, CancellationToken cancellationToken)
    {
        if (!TryHashToken(token, out var hash))
        {
            return new InvitationValidationDto(false, null, "invalid");
        }

        var invitation = await dbContext.UserInvitations
            .AsNoTracking()
            .Include(candidate => candidate.User)
            .SingleOrDefaultAsync(candidate => candidate.TokenHash == hash, cancellationToken);

        return invitation is null
            ? new InvitationValidationDto(false, null, "invalid")
            : GetValidationResult(invitation, DateTime.UtcNow);
    }

    public async Task ActivateInvitationAsync(
        ActivateInvitationRequest request, CancellationToken cancellationToken)
    {
        var passwordError = passwordPolicy.GetValidationMessage(request.Password);
        if (passwordError is not null)
        {
            throw new PasswordPolicyException(passwordError);
        }

        if (!TryHashToken(request.Token, out var hash))
        {
            throw new InvitationUnavailableException("invalid");
        }

        await using var transaction = await dbContext.Database.BeginTransactionAsync(
            IsolationLevel.Serializable, cancellationToken);
        var invitation = await dbContext.UserInvitations
            .Include(candidate => candidate.User)
            .SingleOrDefaultAsync(candidate => candidate.TokenHash == hash, cancellationToken)
            ?? throw new InvitationUnavailableException("invalid");

        var validity = GetValidationResult(invitation, DateTime.UtcNow);
        if (!validity.IsValid)
        {
            throw new InvitationUnavailableException(validity.State ?? "invalid");
        }

        var now = DateTime.UtcNow;
        invitation.User.FirstName = request.FirstName.Trim();
        invitation.User.LastName = request.LastName.Trim();
        invitation.User.PasswordHash = passwordHasher.HashPassword(invitation.User, request.Password);
        invitation.User.Status = UserStatuses.Active;
        invitation.User.EmailVerifiedAtUtc = now;
        invitation.User.ActivatedAtUtc = now;
        invitation.User.UpdatedAt = now;
        invitation.AcceptedAtUtc = now;

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            await transaction.RollbackAsync(cancellationToken);
            throw new InvitationUnavailableException("used");
        }
    }

    public async Task<AuthenticatedUserDto> LoginAsync(
        LoginRequest request, CancellationToken cancellationToken)
    {
        var email = NormalizeEmail(request.Email);
        var user = await dbContext.Users.Include(candidate => candidate.Agency)
            .SingleOrDefaultAsync(candidate => candidate.Email.ToLower() == email, cancellationToken);

        if (user is null || user.Status != UserStatuses.Active || string.IsNullOrEmpty(user.PasswordHash))
        {
            throw new InvalidCredentialsException();
        }

        var result = passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.Password);
        if (result == PasswordVerificationResult.Failed)
        {
            throw new InvalidCredentialsException();
        }

        if (result == PasswordVerificationResult.SuccessRehashNeeded)
        {
            user.PasswordHash = passwordHasher.HashPassword(user, request.Password);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        return AccountMapper.ToAuthenticated(user, workspaces);
    }

    public async Task<AuthenticatedUserDto> GetSessionUserAsync(
        int userId, CancellationToken cancellationToken)
    {
        var user = await dbContext.Users.AsNoTracking()
            .Include(candidate => candidate.Agency)
            .SingleOrDefaultAsync(candidate => candidate.Id == userId, cancellationToken)
            ?? throw new KeyNotFoundException("User was not found.");
        return AccountMapper.ToAuthenticated(user, workspaces);
    }

    /// <summary>
    /// Applies the delegated-administration rules from the requirements: who may invite which role,
    /// agency inheritance, accepted email domains and the per-agency user limit.
    /// </summary>
    private async Task<Agency?> ResolveInviteAgencyAsync(
        Actor actor, InviteUserRequest request, string email, CancellationToken cancellationToken)
    {
        var role = request.Role;
        int? agencyId;
        switch (actor.Role)
        {
            case PortalRoles.SuperAdmin:
                if (PortalRoles.IsInternal(role))
                {
                    return null; // Internal roles are never tied to a broker agency.
                }

                if (!PortalRoles.IsBroker(role))
                {
                    throw new BusinessRuleException("This role cannot be invited.");
                }

                agencyId = request.AgencyId
                    ?? throw new BusinessRuleException("Select the agency for this broker user.");
                break;

            case PortalRoles.BrokerAdmin:
                if (role != PortalRoles.BrokerUser)
                {
                    throw new ForbiddenOperationException(
                        "Broker admins can only invite Broker users.");
                }

                // The agency is always inherited; anything in the request is ignored.
                agencyId = actor.AgencyId
                    ?? throw new ForbiddenOperationException("Your account is not linked to an agency.");
                break;

            default:
                throw new ForbiddenOperationException("You are not allowed to invite users.");
        }

        var agency = await dbContext.Agencies
            .SingleOrDefaultAsync(candidate => candidate.Id == agencyId, cancellationToken)
            ?? throw new BusinessRuleException("The selected agency was not found.");
        if (!agency.IsActive)
        {
            throw new BusinessRuleException($"{agency.Name} is not active.");
        }

        var domains = agency.GetDomains();
        var emailDomain = email[(email.LastIndexOf('@') + 1)..];
        if (domains.Count > 0 && !domains.Contains(emailDomain))
        {
            throw new BusinessRuleException(
                $"{email} is not on an accepted domain for {agency.Name} ({string.Join(", ", domains.Select(domain => "@" + domain))}).");
        }

        if (agency.MaxActiveUsers is { } limit)
        {
            var seats = await dbContext.Users.CountAsync(user =>
                user.AgencyId == agency.Id &&
                (user.Status == UserStatuses.Active || user.Status == UserStatuses.Invited),
                cancellationToken);
            if (seats >= limit)
            {
                throw new BusinessRuleException(
                    $"{agency.Name} has reached its limit of {limit} active users.");
            }
        }

        return agency;
    }

    private async Task<InvitationCreatedDto> AddInvitationAsync(
        PortalUser user, int? invitedByUserId, bool includePreviewLink,
        CancellationToken cancellationToken)
    {
        var token = CreateToken();
        var now = DateTime.UtcNow;
        var expires = now.AddHours(Math.Clamp(settings.LifetimeHours, 1, 168));
        var invitation = new UserInvitation
        {
            User = user,
            TokenHash = HashToken(token),
            CreatedAtUtc = now,
            ExpiresAtUtc = expires,
            InvitedByUserId = invitedByUserId
        };
        dbContext.UserInvitations.Add(invitation);

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (IsUniqueConstraintViolation(exception))
        {
            throw new DuplicateUserEmailException();
        }

        // Every portal user also gets a Duck Creek User Admin account. If that fails the user is left
        // as Setup Incomplete, no invitation is emailed, and a resend retries the provisioning.
        if (user.DuckCreekUserName is null)
        {
            var failure = await ProvisionDuckCreekUserAsync(user, cancellationToken);
            if (failure is not null)
            {
                user.Status = UserStatuses.SetupIncomplete;
                user.UpdatedAt = DateTime.UtcNow;
                invitation.RevokedAtUtc = DateTime.UtcNow;
                await dbContext.SaveChangesAsync(cancellationToken);
                return new InvitationCreatedDto(
                    UserService.ToDto(user), "Failed",
                    $"The user was saved but Duck Creek setup failed: {failure} Use Resend to retry.",
                    null, null);
            }
        }

        var activationUrl = BuildActivationUrl(token);
        var greetingName = string.IsNullOrWhiteSpace(user.FirstName) ? "there" : user.FirstName;
        var delivery = await emailService.SendAsync(
            new InvitationEmail(user.Email, greetingName, activationUrl, expires),
            cancellationToken);

        return new InvitationCreatedDto(
            UserService.ToDto(user),
            delivery.Status,
            delivery.Message,
            includePreviewLink && delivery.Status == "Preview" ? activationUrl : null,
            includePreviewLink && delivery.Status == "Preview" ? delivery.PreviewHtml : null);
    }

    /// <summary>Creates the matching Duck Creek User Admin account. Returns an error message, or null on success.</summary>
    private async Task<string?> ProvisionDuckCreekUserAsync(
        PortalUser user, CancellationToken cancellationToken)
    {
        int? entityId = user.Agency?.DuckCreekEntityId;
        if (entityId is null && PortalRoles.IsInternal(user.Role))
        {
            entityId = duckCreekSettings.InternalEntityId;
        }

        if (entityId is null)
        {
            return $"{user.Agency?.Name ?? "The agency"} has no Duck Creek entity id.";
        }

        var fullName = $"{user.FirstName} {user.LastName}".Trim();
        try
        {
            // FailIfExists stays false so retrying after a partial failure is safe.
            await duckCreek.CreateUserAsync(
                new DuckCreekCreateUserRequest(
                    user.Email,
                    fullName.Length > 0 ? fullName : user.Email,
                    CreateDuckCreekPassword(),
                    entityId.Value),
                cancellationToken);
        }
        catch (DuckCreekUnavailableException exception)
        {
            return exception.Message;
        }

        user.DuckCreekUserName = user.Email;
        await dbContext.SaveChangesAsync(cancellationToken);
        return null;
    }

    /// <summary>
    /// Portal users never sign in to Duck Creek with this password, so it is random and not stored
    /// by the portal. It satisfies typical complexity rules (upper, lower, digit, symbol).
    /// </summary>
    private static string CreateDuckCreekPassword()
    {
        const string upper = "ABCDEFGHJKLMNPQRSTUVWXYZ";
        const string lower = "abcdefghijkmnopqrstuvwxyz";
        const string digits = "23456789";
        const string symbols = "!@#$%^&*";
        var all = upper + lower + digits + symbols;
        var chars = new List<char>
        {
            upper[RandomNumberGenerator.GetInt32(upper.Length)],
            lower[RandomNumberGenerator.GetInt32(lower.Length)],
            digits[RandomNumberGenerator.GetInt32(digits.Length)],
            symbols[RandomNumberGenerator.GetInt32(symbols.Length)]
        };
        while (chars.Count < 24)
        {
            chars.Add(all[RandomNumberGenerator.GetInt32(all.Length)]);
        }

        return new string(chars.OrderBy(_ => RandomNumberGenerator.GetInt32(int.MaxValue)).ToArray());
    }

    private string BuildActivationUrl(string token)
    {
        var baseUrl = settings.FrontendBaseUrl.TrimEnd('/');
        return $"{baseUrl}/activate-invitation?token={Uri.EscapeDataString(token)}";
    }

    private static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();

    private static string CreateToken() =>
        Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');

    private static string HashToken(string token) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));

    private static bool TryHashToken(string token, out string hash)
    {
        if (string.IsNullOrWhiteSpace(token) || token.Length > 200)
        {
            hash = string.Empty;
            return false;
        }

        hash = HashToken(token);
        return true;
    }

    private static InvitationValidationDto GetValidationResult(
        UserInvitation invitation, DateTime now)
    {
        if (invitation.AcceptedAtUtc is not null)
            return new InvitationValidationDto(false, null, "used");
        if (invitation.RevokedAtUtc is not null)
            return new InvitationValidationDto(false, null, "revoked");
        if (invitation.ExpiresAtUtc <= now)
            return new InvitationValidationDto(false, null, "expired");
        if (invitation.User.Status != UserStatuses.Invited)
            return new InvitationValidationDto(false, null, "unavailable");

        return new InvitationValidationDto(
            true, invitation.User.Email, null,
            invitation.User.FirstName, invitation.User.LastName);
    }

    private static bool IsUniqueConstraintViolation(DbUpdateException exception)
    {
        for (Exception? current = exception; current is not null; current = current.InnerException)
        {
            if (current is Microsoft.Data.SqlClient.SqlException { Number: 2601 or 2627 })
            {
                return true;
            }
        }

        return false;
    }
}
