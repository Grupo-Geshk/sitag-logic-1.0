using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SITAG.Domain.Entities;

namespace SITAG.Infrastructure.Persistence.Configurations;

public class ImportDraftConfiguration : IEntityTypeConfiguration<ImportDraft>
{
    public void Configure(EntityTypeBuilder<ImportDraft> b)
    {
        b.ToTable("import_drafts");
        b.HasKey(d => d.Id);

        b.Property(d => d.FileName).HasMaxLength(500).IsRequired();
        b.Property(d => d.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
        b.Property(d => d.RowsJson).HasColumnType("text").IsRequired();
        b.Property(d => d.ExpiresAt).IsRequired();
        b.Property(d => d.CreatedAt).IsRequired();

        // One pending draft per tenant at most (enforced at application layer, index for perf)
        b.HasIndex(d => new { d.TenantId, d.Status });

        b.HasOne(d => d.Tenant).WithMany()
            .HasForeignKey(d => d.TenantId).OnDelete(DeleteBehavior.Restrict);
    }
}
