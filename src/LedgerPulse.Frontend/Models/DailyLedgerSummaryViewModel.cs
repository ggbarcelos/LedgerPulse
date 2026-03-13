namespace LedgerPulse.Frontend.Models;

public sealed record DailyLedgerSummaryViewModel(DateOnly BusinessDate, decimal TotalCredits, decimal TotalDebits, decimal Balance, int EntryCount, DateTime UpdatedAtUtc);
