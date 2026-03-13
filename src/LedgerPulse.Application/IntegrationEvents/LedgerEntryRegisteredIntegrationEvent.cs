using LedgerPulse.Domain.Ledger;

namespace LedgerPulse.Application.IntegrationEvents;

public sealed record LedgerEntryRegisteredIntegrationEvent(Guid LedgerEntryId, DateOnly BusinessDate, decimal Amount, LedgerEntryType EntryType, DateTime OccurredOnUtc);
