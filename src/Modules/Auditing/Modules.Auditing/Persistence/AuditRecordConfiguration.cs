using Finbuckle.MultiTenant.EntityFrameworkCore.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FSH.Modules.Auditing.Persistence;

public class AuditRecordConfiguration : IEntityTypeConfiguration<AuditRecord>
{
    public void Configure(EntityTypeBuilder<AuditRecord> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("AuditRecords", "audit");
        builder.IsMultiTenant();
        builder.HasKey(x => x.Id);
        builder.Property(x => x.EventType).HasConversion<int>();
        builder.Property(x => x.Severity).HasConversion<byte>();
        builder.Property(x => x.Tags).HasConversion<long>();
        builder.Property(x => x.PayloadJson).HasColumnType("jsonb");

        // Impersonation columns for optimized queries
        builder.Property(x => x.IsImpersonating).IsRequired();
        builder.Property(x => x.RealUserId).HasMaxLength(256);
        builder.Property(x => x.RealUserName).HasMaxLength(256);

        // Indexes
        builder.HasIndex(x => x.TenantId);
        builder.HasIndex(x => x.EventType);
        builder.HasIndex(x => x.OccurredAtUtc);
        builder.HasIndex(x => x.IsImpersonating); // Fast impersonation filtering
        builder.HasIndex(x => new { x.IsImpersonating, x.EventType }); // Combined queries
    }
}
