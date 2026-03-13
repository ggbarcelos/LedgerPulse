using LedgerPulse.Domain.Common;
using LedgerPulse.Domain.Ledger;

namespace LedgerPulse.Domain.Ledger.Events;

public sealed record LedgerEntryRegisteredDomainEvent(Guid LedgerEntryId, DateOnly BusinessDate, decimal Amount, LedgerEntryType EntryType) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();

    public DateTime OccurredOnUtc { get; } = DateTime.UtcNow;
}
