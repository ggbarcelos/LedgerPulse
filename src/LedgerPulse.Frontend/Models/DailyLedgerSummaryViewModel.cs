namespace LedgerPulse.Frontend.Models;

public sealed record DailyLedgerSummaryViewModel(DateOnly BusinessDate, decimal TotalAmount, int EntryCount, DateTime UpdatedAtUtc);
