using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SITAG.Domain.Entities;
using SITAG.Domain.Enums;

namespace SITAG.Infrastructure.Persistence.Configurations;

public class TenantConfiguration : IEntityTypeConfiguration<Tenant>
{
    public void Configure(EntityTypeBuilder<Tenant> b)
    {
        b.ToTable("tenants");
        b.HasKey(t => t.Id);

        b.Property(t => t.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        b.Property(t => t.Notes).HasMaxLength(2000);
        b.Property(t => t.CreatedAt).IsRequired();
    }
}

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> b)
    {
        b.ToTable("users");
        b.HasKey(u => u.Id);

        b.HasIndex(u => new { u.TenantId, u.Email }).IsUnique();

        b.Property(u => u.Email).HasMaxLength(255).IsRequired();
        b.Property(u => u.PasswordHash).HasMaxLength(500).IsRequired();
        b.Property(u => u.Role).HasConversion<string>().HasMaxLength(30).IsRequired();
        b.Property(u => u.CreatedAt).IsRequired();

        b.HasOne(u => u.Tenant).WithMany(t => t.Users)
            .HasForeignKey(u => u.TenantId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class TenantAuditLogConfiguration : IEntityTypeConfiguration<TenantAuditLog>
{
    public void Configure(EntityTypeBuilder<TenantAuditLog> b)
    {
        b.ToTable("tenant_audit_logs");
        b.HasKey(l => l.Id);

        b.Property(l => l.ActorEmail).HasMaxLength(255).IsRequired();
        b.Property(l => l.Action).HasConversion<string>().HasMaxLength(30).IsRequired();
        b.Property(l => l.FromStatus).HasConversion<string>().HasMaxLength(20);
        b.Property(l => l.ToStatus).HasConversion<string>().HasMaxLength(20);
        b.Property(l => l.Note).HasMaxLength(2000);
        b.Property(l => l.CreatedAt).IsRequired();

        b.HasOne(l => l.Tenant).WithMany(t => t.AuditLogs)
            .HasForeignKey(l => l.TenantId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class ProducerConfiguration : IEntityTypeConfiguration<Producer>
{
    public void Configure(EntityTypeBuilder<Producer> b)
    {
        b.ToTable("producers");
        b.HasKey(p => p.Id);

        b.HasIndex(p => p.TenantId).IsUnique(); // 1:1 with Tenant in MVP

        b.Property(p => p.DisplayName).HasMaxLength(255).IsRequired();
        b.Property(p => p.CreatedAt).IsRequired();

        b.HasOne(p => p.Tenant).WithOne(t => t.Producer)
            .HasForeignKey<Producer>(p => p.TenantId).OnDelete(DeleteBehavior.Restrict);
    }
}
