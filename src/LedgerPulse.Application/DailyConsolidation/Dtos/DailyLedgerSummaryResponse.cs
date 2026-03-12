namespace LedgerPulse.Application.DailyConsolidation.Dtos;

public sealed record DailyLedgerSummaryResponse(DateOnly BusinessDate, decimal TotalAmount, int EntryCount, DateTime UpdatedAtUtc);
