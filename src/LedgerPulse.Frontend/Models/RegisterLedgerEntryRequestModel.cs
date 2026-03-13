using LedgerPulse.Domain.Ledger;

namespace LedgerPulse.Frontend.Models;

public sealed record RegisterLedgerEntryRequestModel(string Description, decimal Amount, LedgerEntryType EntryType, DateOnly BusinessDate);
