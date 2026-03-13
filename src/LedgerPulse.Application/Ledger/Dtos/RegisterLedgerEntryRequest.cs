using LedgerPulse.Domain.Ledger;

namespace LedgerPulse.Application.Ledger.Dtos;

public sealed record RegisterLedgerEntryRequest(string Description, decimal Amount, LedgerEntryType EntryType, DateOnly? BusinessDate);
