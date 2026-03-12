using LedgerPulse.Domain.Common;

namespace LedgerPulse.Domain.Ledger.Events;

public sealed record LedgerEntryRegisteredDomainEvent(Guid LedgerEntryId, DateOnly BusinessDate, decimal Amount, string Currency) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();

    public DateTime OccurredOnUtc { get; } = DateTime.UtcNow;
}
