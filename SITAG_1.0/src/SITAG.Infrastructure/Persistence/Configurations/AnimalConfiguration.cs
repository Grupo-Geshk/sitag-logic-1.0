using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SITAG.Domain.Entities;

namespace SITAG.Infrastructure.Persistence.Configurations;

public class AnimalConfiguration : IEntityTypeConfiguration<Animal>
{
    public void Configure(EntityTypeBuilder<Animal> b)
    {
        b.ToTable("animals");
        b.HasKey(a => a.Id);

        // TagNumber unique per tenant
        b.HasIndex(a => new { a.TenantId, a.TagNumber }).IsUnique();

        b.Property(a => a.TagNumber).HasMaxLength(100).IsRequired();
        b.Property(a => a.Sex).HasMaxLength(20).IsRequired();
        b.Property(a => a.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
        b.Property(a => a.HealthStatus).HasConversion<string>().HasMaxLength(30).IsRequired();
        b.Property(a => a.CloseReason).HasMaxLength(500);
        b.Property(a => a.MotherRef).HasMaxLength(200);
        b.Property(a => a.FatherRef).HasMaxLength(200);
        b.Property(a => a.PhotoUrl).HasMaxLength(1000);
        b.Property(a => a.CreatedAt).IsRequired();

        b.HasOne(a => a.Tenant).WithMany()
            .HasForeignKey(a => a.TenantId).OnDelete(DeleteBehavior.Restrict);

        b.HasOne(a => a.Farm).WithMany(f => f.Animals)
            .HasForeignKey(a => a.FarmId).OnDelete(DeleteBehavior.Restrict);

        b.HasOne(a => a.Division).WithMany()
            .HasForeignKey(a => a.DivisionId).OnDelete(DeleteBehavior.SetNull);

        // ── Genealogy: self-referencing FKs ───────────────────────────────────
        // No bidirectional collection navigations — offspring are queried directly
        // via MotherId / FatherId filters when needed (avoids ambiguous FK mapping).
        b.HasOne(a => a.Mother).WithMany()
            .HasForeignKey(a => a.MotherId)
            .OnDelete(DeleteBehavior.SetNull)
            .IsRequired(false);

        b.HasOne(a => a.Father).WithMany()
            .HasForeignKey(a => a.FatherId)
            .OnDelete(DeleteBehavior.SetNull)
            .IsRequired(false);

        // Explicit indexes to support efficient offspring queries
        b.HasIndex(a => a.MotherId);
        b.HasIndex(a => a.FatherId);
    }
}

public class AnimalMovementConfiguration : IEntityTypeConfiguration<AnimalMovement>
{
    public void Configure(EntityTypeBuilder<AnimalMovement> b)
    {
        b.ToTable("animal_movements");
        b.HasKey(m => m.Id);

        b.Property(m => m.Scope).HasConversion<string>().HasMaxLength(20).IsRequired();
        b.Property(m => m.Reason).HasMaxLength(500);
        b.Property(m => m.CreatedAt).IsRequired();

        b.HasOne(m => m.Animal).WithMany(a => a.Movements)
            .HasForeignKey(m => m.AnimalId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class AnimalEventConfiguration : IEntityTypeConfiguration<AnimalEvent>
{
    public void Configure(EntityTypeBuilder<AnimalEvent> b)
    {
        b.ToTable("animal_events");
        b.HasKey(e => e.Id);

        b.Property(e => e.EventType).HasConversion<string>().HasMaxLength(30).IsRequired();
        b.Property(e => e.Cost).HasColumnType("numeric(12,2)");
        b.Property(e => e.Description).HasMaxLength(2000);
        b.Property(e => e.CreatedAt).IsRequired();

        b.HasOne(e => e.Animal).WithMany(a => a.Events)
            .HasForeignKey(e => e.AnimalId).OnDelete(DeleteBehavior.Restrict);

        b.HasOne(e => e.Worker).WithMany()
            .HasForeignKey(e => e.WorkerId).OnDelete(DeleteBehavior.SetNull);
    }
}
