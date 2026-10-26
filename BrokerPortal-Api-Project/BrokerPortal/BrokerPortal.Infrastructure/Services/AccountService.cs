using System.Data;
using System.Security.Cryptography;
using System.Text;
using BrokerPortal.Application.Users;
using BrokerPortal.Domain.Entities;
using BrokerPortal.Infrastructure.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace BrokerPortal.Infrastructure.Services;

public sealed class AccountService(
    BrokerPortalDbContext dbContext,
    IPasswordHasher<PortalUser> passwordHasher,
    IInvitationEmailService emailService,
    IOptions<InvitationEmailOptions> emailOptions,
    IOptions<ActivationPasswordOptions> passwordOptions) : IAccountService
{
    private static readonly HashSet<string> AllowedInviteRoles =
        ["BROKER_ADMIN", "BROKER_USER"];

    private readonly InvitationEmailOptions settings = emailOptions.Value;
    private readonly ActivationPasswordOptions passwordPolicy = passwordOptions.Value;

    public async Task<InvitationCreatedDto> CreateInvitationAsync(
        InviteUserRequest request, int? invitedByUserId, bool includePreviewLink,
        CancellationToken cancellationToken)
    {
        var email = NormalizeEmail(request.Email);
        if (!AllowedInviteRoles.Contains(request.Role))
        {
            throw new ArgumentException("Only broker roles can be invited.", nameof(request));
        }

        if (await dbContext.Users.AnyAsync(
                user => user.Email.ToLower() == email, cancellationToken))
        {
            throw new DuplicateUserEmailException();
        }

        var user = new PortalUser
        {
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            Email = email,
            JobTitle = string.IsNullOrWhiteSpace(request.JobTitle) ? null : request.JobTitle.Trim(),
            Role = request.Role,
            Status = "Invited",
            CreatedAt = DateTime.UtcNow
        };

        dbContext.Users.Add(user);
        var created = await AddInvitationAsync(user, invitedByUserId, includePreviewLink, cancellationToken);
        return created;
    }

    public async Task<InvitationCreatedDto> ResendInvitationAsync(
        int userId, int? invitedByUserId, bool includePreviewLink,
        CancellationToken cancellationToken)
    {
        var user = await dbContext.Users
            .SingleOrDefaultAsync(candidate => candidate.Id == userId, cancellationToken)
            ?? throw new KeyNotFoundException("User was not found.");

        if (user.Status != "Invited")
        {
            throw new InvalidOperationException("Only invited users can receive a new invitation.");
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

        return await AddInvitationAsync(user, invitedByUserId, includePreviewLink, cancellationToken);
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
        invitation.User.PasswordHash = passwordHasher.HashPassword(invitation.User, request.Password);
        invitation.User.Status = "Active";
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
        var user = await dbContext.Users.SingleOrDefaultAsync(
            candidate => candidate.Email.ToLower() == email, cancellationToken);

        if (user is null || user.Status != "Active" || string.IsNullOrEmpty(user.PasswordHash))
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

        return new AuthenticatedUserDto(user.Id, user.Email, user.FirstName, user.LastName, user.Role);
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

        var activationUrl = BuildActivationUrl(token);
        var delivery = await emailService.SendAsync(
            new InvitationEmail(user.Email, user.FirstName, activationUrl, expires),
            cancellationToken);

        return new InvitationCreatedDto(
            ToDto(user),
            delivery.Status,
            delivery.Message,
            includePreviewLink && delivery.Status == "Preview" ? activationUrl : null,
            includePreviewLink && delivery.Status == "Preview" ? delivery.PreviewHtml : null);
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
        if (invitation.User.Status != "Invited")
            return new InvitationValidationDto(false, null, "unavailable");

        return new InvitationValidationDto(true, invitation.User.Email, null);
    }

    private static UserListItemDto ToDto(PortalUser user) =>
        new(user.Id, user.FirstName, user.LastName, user.Email, user.JobTitle,
            user.Role, user.Status, user.CreatedAt);

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
