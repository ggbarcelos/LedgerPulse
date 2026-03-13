using LedgerPulse.Domain.DailyConsolidation.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LedgerPulse.Infrastructure.Persistence.Configurations;

public sealed class DailyLedgerSummaryConfiguration : IEntityTypeConfiguration<DailyLedgerSummary>
{
    public void Configure(EntityTypeBuilder<DailyLedgerSummary> builder)
    {
        builder.ToTable("daily_ledger_summaries");
        builder.HasKey(summary => summary.Id);
        builder.HasIndex(summary => summary.BusinessDate).IsUnique();
        builder.Property(summary => summary.BusinessDate).IsRequired();
        builder.Property(summary => summary.TotalCredits).HasColumnType("numeric(18,2)");
        builder.Property(summary => summary.TotalDebits).HasColumnType("numeric(18,2)");
        builder.Property(summary => summary.EntryCount).IsRequired();
        builder.Property(summary => summary.UpdatedAtUtc).IsRequired();
    }
}
