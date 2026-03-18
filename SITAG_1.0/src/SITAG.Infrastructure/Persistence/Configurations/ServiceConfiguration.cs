using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SITAG.Domain.Entities;

namespace SITAG.Infrastructure.Persistence.Configurations;

public class VetServiceConfiguration : IEntityTypeConfiguration<VetService>
{
    public void Configure(EntityTypeBuilder<VetService> b)
    {
        b.ToTable("vet_services");
        b.HasKey(s => s.Id);

        b.Property(s => s.ServiceType).HasMaxLength(100).IsRequired();
        b.Property(s => s.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
        b.Property(s => s.Cost).HasColumnType("numeric(12,2)");
        b.Property(s => s.Notes).HasMaxLength(2000);
        b.Property(s => s.CreatedAt).IsRequired();

        b.HasOne(s => s.Tenant).WithMany()
            .HasForeignKey(s => s.TenantId).OnDelete(DeleteBehavior.Restrict);

        b.HasOne(s => s.Farm).WithMany()
            .HasForeignKey(s => s.FarmId).OnDelete(DeleteBehavior.Restrict);

        b.HasOne(s => s.Worker).WithMany()
            .HasForeignKey(s => s.WorkerId).OnDelete(DeleteBehavior.SetNull);
    }
}

public class ServiceAnimalConfiguration : IEntityTypeConfiguration<ServiceAnimal>
{
    public void Configure(EntityTypeBuilder<ServiceAnimal> b)
    {
        b.ToTable("service_animals");
        b.HasKey(sa => sa.Id);

        // (ServiceId, AnimalId) must be unique
        b.HasIndex(sa => new { sa.ServiceId, sa.AnimalId }).IsUnique();

        b.HasOne(sa => sa.Service).WithMany(s => s.ServiceAnimals)
            .HasForeignKey(sa => sa.ServiceId).OnDelete(DeleteBehavior.Cascade);

        b.HasOne(sa => sa.Animal).WithMany(a => a.ServiceAnimals)
            .HasForeignKey(sa => sa.AnimalId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class ServiceSupplyConsumptionConfiguration : IEntityTypeConfiguration<ServiceSupplyConsumption>
{
    public void Configure(EntityTypeBuilder<ServiceSupplyConsumption> b)
    {
        b.ToTable("service_supply_consumptions");
        b.HasKey(c => c.Id);

        b.Property(c => c.Quantity).HasColumnType("numeric(12,3)").IsRequired();

        b.HasOne(c => c.Service).WithMany(s => s.SupplyConsumptions)
            .HasForeignKey(c => c.ServiceId).OnDelete(DeleteBehavior.Cascade);

        b.HasOne(c => c.Supply).WithMany()
            .HasForeignKey(c => c.SupplyId).OnDelete(DeleteBehavior.Restrict);
    }
}
