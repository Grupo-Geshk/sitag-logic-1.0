using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SITAG.Domain.Entities;

namespace SITAG.Infrastructure.Persistence.Configurations;

public class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> b)
    {
        b.ToTable("refresh_tokens");
        b.HasKey(t => t.Id);

        // Lookup by hash must be fast and guaranteed unique
        b.HasIndex(t => t.TokenHash).IsUnique();

        b.Property(t => t.TokenHash).HasMaxLength(512).IsRequired();
        b.Property(t => t.CreatedByIp).HasMaxLength(50);
        b.Property(t => t.UserAgent).HasMaxLength(512);
        b.Property(t => t.CreatedAt).IsRequired();

        // Ignore computed helpers — they are derived, not stored
        b.Ignore(t => t.IsExpired);
        b.Ignore(t => t.IsRevoked);
        b.Ignore(t => t.IsActive);

        b.HasOne(t => t.User).WithMany()
            .HasForeignKey(t => t.UserId).OnDelete(DeleteBehavior.Cascade);
    }
}
