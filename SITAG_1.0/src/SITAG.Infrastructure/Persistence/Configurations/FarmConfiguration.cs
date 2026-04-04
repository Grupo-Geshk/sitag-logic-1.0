using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SITAG.Domain.Entities;

namespace SITAG.Infrastructure.Persistence.Configurations;

public class FarmConfiguration : IEntityTypeConfiguration<Farm>
{
    public void Configure(EntityTypeBuilder<Farm> b)
    {
        b.ToTable("farms");
        b.HasKey(f => f.Id);

        b.Property(f => f.Name).HasMaxLength(255).IsRequired();
        b.Property(f => f.Location).HasMaxLength(500);
        b.Property(f => f.Hectares).HasColumnType("numeric(10,2)");
        b.Property(f => f.CreatedAt).IsRequired();

        b.HasOne(f => f.Tenant).WithMany()
            .HasForeignKey(f => f.TenantId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class FarmBrandConfiguration : IEntityTypeConfiguration<FarmBrand>
{
    public void Configure(EntityTypeBuilder<FarmBrand> b)
    {
        b.ToTable("farm_brands");
        b.HasKey(fb => fb.Id);

        b.Property(fb => fb.Name).HasMaxLength(255).IsRequired();
        b.Property(fb => fb.PhotoUrl).HasMaxLength(1000);
        b.Property(fb => fb.CreatedAt).IsRequired();

        b.HasOne(fb => fb.Farm).WithMany(f => f.Brands)
            .HasForeignKey(fb => fb.FarmId).OnDelete(DeleteBehavior.Restrict);

        b.HasOne(fb => fb.Tenant).WithMany()
            .HasForeignKey(fb => fb.TenantId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class DivisionConfiguration : IEntityTypeConfiguration<Division>
{
    public void Configure(EntityTypeBuilder<Division> b)
    {
        b.ToTable("divisions");
        b.HasKey(d => d.Id);

        b.Property(d => d.Name).HasMaxLength(255).IsRequired();
        b.Property(d => d.CreatedAt).IsRequired();

        b.HasOne(d => d.Farm).WithMany(f => f.Divisions)
            .HasForeignKey(d => d.FarmId).OnDelete(DeleteBehavior.Restrict);

        b.HasOne(d => d.Tenant).WithMany()
            .HasForeignKey(d => d.TenantId).OnDelete(DeleteBehavior.Restrict);
    }
}
