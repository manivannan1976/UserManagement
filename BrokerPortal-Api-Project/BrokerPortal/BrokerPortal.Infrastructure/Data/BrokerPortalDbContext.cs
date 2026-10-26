using BrokerPortal.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BrokerPortal.Infrastructure.Data;

public sealed class BrokerPortalDbContext(
    DbContextOptions<BrokerPortalDbContext> options) : DbContext(options)
{
    public DbSet<PortalUser> Users => Set<PortalUser>();
    public DbSet<UserInvitation> UserInvitations => Set<UserInvitation>();
    public DbSet<Agency> Agencies => Set<Agency>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        var user = modelBuilder.Entity<PortalUser>();
        user.ToTable("PortalUsers");
        user.HasKey(x => x.Id);
        user.Property(x => x.Id).HasColumnName("UserId").UseIdentityColumn();
        user.Property(x => x.FirstName).HasMaxLength(100).IsRequired();
        user.Property(x => x.LastName).HasMaxLength(100).IsRequired();
        user.Property(x => x.Email).HasMaxLength(256).IsRequired();
        user.HasIndex(x => x.Email).IsUnique();
        user.Property(x => x.JobTitle).HasMaxLength(150);
        user.Property(x => x.Role).HasMaxLength(50).IsRequired();
        user.Property(x => x.Status).HasMaxLength(30).IsRequired();
        user.Property(x => x.CreatedAt).IsRequired();
        user.Property(x => x.RowVersion).IsRowVersion();
        user.Property(x => x.PasswordHash).HasMaxLength(512);
        user.Property(x => x.EmailVerifiedAtUtc).HasColumnName("EmailVerifiedAt");
        user.Property(x => x.ActivatedAtUtc).HasColumnName("ActivatedAt");
        user.Property(x => x.DeactivatedAtUtc).HasColumnName("DeactivatedAt");
        user.HasOne(x => x.Agency).WithMany().HasForeignKey(x => x.AgencyId)
            .OnDelete(DeleteBehavior.Restrict);

        var agency = modelBuilder.Entity<Agency>();
        agency.ToTable("Agencies");
        agency.HasKey(x => x.Id);
        agency.Property(x => x.Id).HasColumnName("AgencyId").UseIdentityColumn();
        agency.Property(x => x.Name).HasMaxLength(200).IsRequired();
        agency.HasIndex(x => x.Name).IsUnique();
        agency.Property(x => x.DuckCreekAgencyId).HasMaxLength(100);
        agency.Property(x => x.AcceptedDomains).HasMaxLength(1000).IsRequired();
        agency.Property(x => x.Products).HasMaxLength(1000).IsRequired();

        var invitation = modelBuilder.Entity<UserInvitation>();
        invitation.ToTable("UserInvitations");
        invitation.HasKey(x => x.Id).HasName("PK_UserInvitations");
        invitation.Property(x => x.Id).HasColumnName("InvitationId")
            .HasDefaultValueSql("NEWSEQUENTIALID()");
        invitation.Property(x => x.TokenHash).HasMaxLength(64).IsUnicode(false).IsRequired();
        invitation.HasIndex(x => x.TokenHash).IsUnique();
        invitation.HasIndex(x => new { x.UserId, x.CreatedAtUtc });
        invitation.Property(x => x.RowVersion).IsRowVersion();
        invitation.HasOne<PortalUser>()
            .WithMany()
            .HasForeignKey(x => x.InvitedByUserId)
            .OnDelete(DeleteBehavior.NoAction)
            .HasConstraintName("FK_UserInvitations_PortalUsers_InvitedByUserId");
        invitation.HasOne(x => x.User)
            .WithMany(x => x.Invitations)
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_UserInvitations_PortalUsers_UserId");
    }
}
