using LedgerPulse.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LedgerPulse.Infrastructure.Persistence.Configurations;

public sealed class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        builder.ToTable("outbox_messages");
        builder.HasKey(message => message.Id);
        builder.HasIndex(message => message.ProcessedOnUtc);
        builder.Property(message => message.Type).HasMaxLength(200).IsRequired();
        builder.Property(message => message.Content).IsRequired();
        builder.Property(message => message.OccurredOnUtc).IsRequired();
        builder.Property(message => message.Error).HasMaxLength(1000);
    }
}
