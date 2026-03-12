namespace LedgerPulse.Frontend.Models;

public sealed record LedgerEntryViewModel(Guid Id, DateOnly BusinessDate, string Description, decimal Amount, string Currency, DateTime CreatedAtUtc);
