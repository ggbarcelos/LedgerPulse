using LedgerPulse.Domain.Ledger;

namespace LedgerPulse.Frontend.Models;

public sealed record LedgerEntryViewModel(Guid Id, DateOnly BusinessDate, string Description, decimal Amount, LedgerEntryType EntryType, DateTime CreatedAtUtc);
