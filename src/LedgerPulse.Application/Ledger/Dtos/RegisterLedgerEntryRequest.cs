namespace LedgerPulse.Application.Ledger.Dtos;

public sealed record RegisterLedgerEntryRequest(string Description, decimal Amount, string Currency, DateOnly? BusinessDate);
