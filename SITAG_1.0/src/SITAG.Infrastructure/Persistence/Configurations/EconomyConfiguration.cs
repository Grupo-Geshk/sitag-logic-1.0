using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SITAG.Domain.Entities;

namespace SITAG.Infrastructure.Persistence.Configurations;

public class TransactionCategoryConfiguration : IEntityTypeConfiguration<TransactionCategory>
{
    public void Configure(EntityTypeBuilder<TransactionCategory> b)
    {
        b.ToTable("transaction_categories");
        b.HasKey(c => c.Id);

        // Unique per (tenant, name, type)
        b.HasIndex(c => new { c.TenantId, c.Name, c.Type }).IsUnique();

        b.Property(c => c.Name).HasMaxLength(150).IsRequired();
        b.Property(c => c.Type).HasConversion<string>().HasMaxLength(10).IsRequired();
        b.Property(c => c.CreatedAt).IsRequired();

        b.HasOne(c => c.Tenant).WithMany()
            .HasForeignKey(c => c.TenantId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class EconomyTransactionConfiguration : IEntityTypeConfiguration<EconomyTransaction>
{
    public void Configure(EntityTypeBuilder<EconomyTransaction> b)
    {
        b.ToTable("economy_transactions");
        b.HasKey(t => t.Id);

        b.Property(t => t.Type).HasConversion<string>().HasMaxLength(10).IsRequired();
        b.Property(t => t.CategoryName).HasMaxLength(150);
        b.Property(t => t.Amount).HasColumnType("numeric(12,2)").IsRequired();
        b.Property(t => t.CreatedAt).IsRequired();

        b.HasOne(t => t.Tenant).WithMany()
            .HasForeignKey(t => t.TenantId).OnDelete(DeleteBehavior.Restrict);

        b.HasOne(t => t.Category).WithMany(c => c.Transactions)
            .HasForeignKey(t => t.CategoryId).OnDelete(DeleteBehavior.SetNull);
    }
}
