using System.Text.Json;
using LedgerPulse.Application.Abstractions.Persistence;
using LedgerPulse.Application.IntegrationEvents;
using LedgerPulse.Domain.Common;
using LedgerPulse.Domain.DailyConsolidation.Entities;
using LedgerPulse.Domain.Ledger.Entities;
using LedgerPulse.Domain.Ledger.Events;
using LedgerPulse.Infrastructure.Persistence.Configurations;
using Microsoft.EntityFrameworkCore;

namespace LedgerPulse.Infrastructure.Persistence;

public sealed class LedgerPulseDbContext(DbContextOptions<LedgerPulseDbContext> options) : DbContext(options), IUnitOfWork
{
    private static readonly JsonSerializerOptions JsonSerializerOptions = new(JsonSerializerDefaults.Web);

    public DbSet<LedgerEntry> LedgerEntries => Set<LedgerEntry>();

    public DbSet<DailyLedgerSummary> DailyLedgerSummaries => Set<DailyLedgerSummary>();

    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new LedgerEntryConfiguration());
        modelBuilder.ApplyConfiguration(new DailyLedgerSummaryConfiguration());
        modelBuilder.ApplyConfiguration(new OutboxMessageConfiguration());
        base.OnModelCreating(modelBuilder);
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        AddOutboxMessages();
        return await base.SaveChangesAsync(cancellationToken);
    }

    private void AddOutboxMessages()
    {
        var outboxMessages = ChangeTracker
            .Entries<Entity>()
            .Select(entry => entry.Entity)
            .Where(entity => entity.DomainEvents.Count > 0)
            .SelectMany(entity =>
            {
                var events = entity.DomainEvents.ToArray();
                entity.ClearDomainEvents();
                return events;
            })
            .Select(MapDomainEventToOutboxMessage)
            .ToArray();

        if (outboxMessages.Length > 0)
        {
            OutboxMessages.AddRange(outboxMessages);
        }
    }

    private static OutboxMessage MapDomainEventToOutboxMessage(IDomainEvent domainEvent)
    {
        return domainEvent switch
        {
            LedgerEntryRegisteredDomainEvent ledgerEntryRegistered => MapLedgerEntryRegistered(ledgerEntryRegistered),
            _ => throw new InvalidOperationException($"Domain event '{domainEvent.GetType().Name}' is not mapped to the outbox.")
        };
    }

    private static OutboxMessage MapLedgerEntryRegistered(LedgerEntryRegisteredDomainEvent domainEvent)
    {
        var integrationEvent = new LedgerEntryRegisteredIntegrationEvent(
            domainEvent.LedgerEntryId,
            domainEvent.BusinessDate,
            domainEvent.Amount,
            domainEvent.EntryType,
            domainEvent.OccurredOnUtc);

        return OutboxMessage.Create(
            domainEvent.EventId,
            nameof(LedgerEntryRegisteredIntegrationEvent),
            JsonSerializer.Serialize(integrationEvent, JsonSerializerOptions),
            domainEvent.OccurredOnUtc);
    }
}
