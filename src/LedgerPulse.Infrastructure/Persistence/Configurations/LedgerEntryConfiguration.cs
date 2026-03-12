using LedgerPulse.Domain.Ledger.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LedgerPulse.Infrastructure.Persistence.Configurations;

public sealed class LedgerEntryConfiguration : IEntityTypeConfiguration<LedgerEntry>
{
    public void Configure(EntityTypeBuilder<LedgerEntry> builder)
    {
        builder.ToTable("ledger_entries");
        builder.HasKey(entry => entry.Id);
        builder.Property(entry => entry.Description).HasMaxLength(200).IsRequired();
        builder.Property(entry => entry.Currency).HasMaxLength(3).IsRequired();
        builder.Property(entry => entry.Amount).HasColumnType("numeric(18,2)");
        builder.Property(entry => entry.CreatedAtUtc).IsRequired();
        builder.Property(entry => entry.BusinessDate).IsRequired();
        builder.Ignore(entry => entry.DomainEvents);
    }
}
