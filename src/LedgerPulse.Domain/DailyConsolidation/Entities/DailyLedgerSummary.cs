namespace LedgerPulse.Domain.DailyConsolidation.Entities;

public sealed class DailyLedgerSummary
{
    private DailyLedgerSummary()
    {
    }

    public Guid Id { get; private set; }

    public DateOnly BusinessDate { get; private set; }

    public decimal TotalAmount { get; private set; }

    public int EntryCount { get; private set; }

    public DateTime UpdatedAtUtc { get; private set; }

    public static DailyLedgerSummary Create(DateOnly businessDate, DateTime nowUtc)
    {
        return new DailyLedgerSummary
        {
            Id = Guid.NewGuid(),
            BusinessDate = businessDate,
            TotalAmount = 0,
            EntryCount = 0,
            UpdatedAtUtc = nowUtc
        };
    }

    public void ApplyEntry(decimal amount, DateTime nowUtc)
    {
        TotalAmount += amount;
        EntryCount += 1;
        UpdatedAtUtc = nowUtc;
    }
}
