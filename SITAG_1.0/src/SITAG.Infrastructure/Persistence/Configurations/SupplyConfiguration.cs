using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SITAG.Domain.Entities;

namespace SITAG.Infrastructure.Persistence.Configurations;

public class SupplyConfiguration : IEntityTypeConfiguration<Supply>
{
    public void Configure(EntityTypeBuilder<Supply> b)
    {
        b.ToTable("supplies");
        b.HasKey(s => s.Id);

        b.Property(s => s.Name).HasMaxLength(255).IsRequired();
        b.Property(s => s.Category).HasMaxLength(100);
        b.Property(s => s.Unit).HasMaxLength(50).IsRequired();
        b.Property(s => s.CurrentQuantity).HasColumnType("numeric(12,3)").IsRequired();
        b.Property(s => s.MinStockLevel).HasColumnType("numeric(12,3)").IsRequired();
        b.Property(s => s.CreatedAt).IsRequired();

        b.HasOne(s => s.Tenant).WithMany()
            .HasForeignKey(s => s.TenantId).OnDelete(DeleteBehavior.Restrict);

        b.HasOne(s => s.Farm).WithMany()
            .HasForeignKey(s => s.FarmId).OnDelete(DeleteBehavior.Restrict)
            .IsRequired(false);
    }
}

public class SupplyMovementConfiguration : IEntityTypeConfiguration<SupplyMovement>
{
    public void Configure(EntityTypeBuilder<SupplyMovement> b)
    {
        b.ToTable("supply_movements");
        b.HasKey(m => m.Id);

        b.Property(m => m.MovementType).HasConversion<string>().HasMaxLength(20).IsRequired();
        b.Property(m => m.Quantity).HasColumnType("numeric(12,3)").IsRequired();
        b.Property(m => m.PreviousQuantity).HasColumnType("numeric(12,3)").IsRequired();
        b.Property(m => m.NewQuantity).HasColumnType("numeric(12,3)").IsRequired();
        b.Property(m => m.Reason).HasMaxLength(500);
        b.Property(m => m.CreatedAt).IsRequired();

        b.HasOne(m => m.Supply).WithMany(s => s.Movements)
            .HasForeignKey(m => m.SupplyId).OnDelete(DeleteBehavior.Restrict);
    }
}
