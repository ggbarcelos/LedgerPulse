namespace LedgerPulse.Application.DailyConsolidation.Dtos;

public sealed record DailyLedgerSummaryResponse(DateOnly BusinessDate, decimal TotalCredits, decimal TotalDebits, decimal Balance, int EntryCount, DateTime UpdatedAtUtc);
