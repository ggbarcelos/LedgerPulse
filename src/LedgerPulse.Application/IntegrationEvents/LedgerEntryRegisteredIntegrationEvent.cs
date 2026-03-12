namespace LedgerPulse.Application.IntegrationEvents;

public sealed record LedgerEntryRegisteredIntegrationEvent(Guid LedgerEntryId, DateOnly BusinessDate, decimal Amount, string Currency, DateTime OccurredOnUtc);
