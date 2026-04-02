using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SITAG.Domain.Entities;
using SITAG.Domain.Enums;

namespace SITAG.Infrastructure.Persistence.Configurations;

public class WorkerConfiguration : IEntityTypeConfiguration<Worker>
{
    public void Configure(EntityTypeBuilder<Worker> b)
    {
        b.ToTable("workers");
        b.HasKey(w => w.Id);

        b.Property(w => w.Name).HasMaxLength(255).IsRequired();
        b.Property(w => w.RoleLabel).HasMaxLength(100);
        b.Property(w => w.Contact).HasMaxLength(255);
        b.Property(w => w.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
        b.Property(w => w.CreatedAt).IsRequired();

        b.HasOne(w => w.Tenant).WithMany()
            .HasForeignKey(w => w.TenantId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class WorkerFarmAssignmentConfiguration : IEntityTypeConfiguration<WorkerFarmAssignment>
{
    public void Configure(EntityTypeBuilder<WorkerFarmAssignment> b)
    {
        b.ToTable("worker_farm_assignments");
        b.HasKey(a => a.Id);

        // Only one active assignment per (worker, farm) — enforced by application rule
        b.HasIndex(a => new { a.WorkerId, a.FarmId });
        b.Property(a => a.CreatedAt).IsRequired();

        b.HasOne(a => a.Worker).WithMany(w => w.FarmAssignments)
            .HasForeignKey(a => a.WorkerId).OnDelete(DeleteBehavior.Restrict);

        b.HasOne(a => a.Farm).WithMany()
            .HasForeignKey(a => a.FarmId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class WorkerLoanConfiguration : IEntityTypeConfiguration<WorkerLoan>
{
    public void Configure(EntityTypeBuilder<WorkerLoan> b)
    {
        b.ToTable("worker_loans");
        b.HasKey(l => l.Id);

        b.Property(l => l.Amount).HasPrecision(18, 2).IsRequired();
        b.Property(l => l.RemainingAmount).HasPrecision(18, 2).IsRequired();
        b.Property(l => l.Description).HasMaxLength(500);
        b.Property(l => l.CreatedAt).IsRequired();

        b.HasIndex(l => new { l.WorkerId, l.LoanDate });

        b.HasOne(l => l.Worker).WithMany()
            .HasForeignKey(l => l.WorkerId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class WorkerPaymentConfiguration : IEntityTypeConfiguration<WorkerPayment>
{
    public void Configure(EntityTypeBuilder<WorkerPayment> b)
    {
        b.ToTable("worker_payments");
        b.HasKey(p => p.Id);

        b.Property(p => p.Mode).HasConversion<string>().HasMaxLength(20).IsRequired();
        b.Property(p => p.Rate).HasPrecision(18, 4).IsRequired();
        b.Property(p => p.Quantity).HasPrecision(18, 4).IsRequired();
        b.Property(p => p.TotalAmount).HasPrecision(18, 2).IsRequired();
        b.Property(p => p.Notes).HasMaxLength(500);
        b.Property(p => p.CreatedAt).IsRequired();

        b.HasIndex(p => new { p.WorkerId, p.PaymentDate });

        b.HasOne(p => p.Worker).WithMany()
            .HasForeignKey(p => p.WorkerId).OnDelete(DeleteBehavior.Restrict);
    }
}
