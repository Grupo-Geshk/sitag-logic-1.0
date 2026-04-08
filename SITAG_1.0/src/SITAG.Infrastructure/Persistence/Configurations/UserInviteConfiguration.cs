using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SITAG.Domain.Entities;

namespace SITAG.Infrastructure.Persistence.Configurations;

public class UserInviteConfiguration : IEntityTypeConfiguration<UserInvite>
{
    public void Configure(EntityTypeBuilder<UserInvite> b)
    {
        b.ToTable("user_invites");
        b.HasKey(i => i.Id);

        b.Property(i => i.Email).HasMaxLength(254).IsRequired();
        b.Property(i => i.TokenHash).HasMaxLength(512).IsRequired();
        b.Property(i => i.ExpiresAt).IsRequired();

        // Token lookup must be fast and unique
        b.HasIndex(i => i.TokenHash).IsUnique();

        // One pending invite per email per tenant (prevent spam)
        b.HasIndex(i => new { i.TenantId, i.Email });

        // Ignore computed helpers
        b.Ignore(i => i.IsExpired);
        b.Ignore(i => i.IsAccepted);
        b.Ignore(i => i.IsValid);

        b.HasOne(i => i.Tenant).WithMany()
            .HasForeignKey(i => i.TenantId).OnDelete(DeleteBehavior.Cascade);
    }
}
