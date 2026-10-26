using BrokerPortal.Domain.Entities;
using BrokerPortal.Infrastructure.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace BrokerPortal.Api;

public static class DevelopmentAdminBootstrapper
{
    public static async Task EnsureAdminAsync(
        IServiceProvider services,
        IConfiguration configuration,
        IHostEnvironment environment,
        CancellationToken cancellationToken = default)
    {
        if (!environment.IsDevelopment())
        {
            return;
        }

        var email = configuration["DevelopmentAdmin:Email"]?.Trim().ToLowerInvariant();
        var password = configuration["DevelopmentAdmin:Password"];
        var resetPassword = configuration.GetValue<bool>("DevelopmentAdmin:ResetPassword");
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            return;
        }

        await using var scope = services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<BrokerPortalDbContext>();
        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher<PortalUser>>();

        var admin = await dbContext.Users.SingleOrDefaultAsync(
            user => user.Email.ToLower() == email, cancellationToken);
        if (admin is null)
        {
            var now = DateTime.UtcNow;
            admin = new PortalUser
            {
                FirstName = "Portal",
                LastName = "Administrator",
                Email = email,
                Role = "PORTAL_SUPER_ADMIN",
                Status = "Active",
                CreatedAt = now,
                ActivatedAtUtc = now,
                EmailVerifiedAtUtc = now
            };
            admin.PasswordHash = hasher.HashPassword(admin, password);
            dbContext.Users.Add(admin);
        }
        else if (admin.Role != "PORTAL_SUPER_ADMIN")
        {
            throw new InvalidOperationException(
                "Configured development administrator exists without the required administrator role.");
        }
        else if (!resetPassword)
        {
            return;
        }
        else
        {
            admin.Status = "Active";
            admin.PasswordHash = hasher.HashPassword(admin, password);
            admin.ActivatedAtUtc ??= DateTime.UtcNow;
            admin.EmailVerifiedAtUtc ??= DateTime.UtcNow;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
