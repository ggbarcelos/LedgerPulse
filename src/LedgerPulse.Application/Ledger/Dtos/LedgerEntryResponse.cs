namespace LedgerPulse.Application.Ledger.Dtos;

public sealed record LedgerEntryResponse(Guid Id, DateOnly BusinessDate, string Description, decimal Amount, string Currency, DateTime CreatedAtUtc);
