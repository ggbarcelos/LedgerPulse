using LedgerPulse.Domain.Ledger;

namespace LedgerPulse.Domain.DailyConsolidation.Entities;

public sealed class DailyLedgerSummary
{
    private DailyLedgerSummary()
    {
    }

    public Guid Id { get; private set; }

    public DateOnly BusinessDate { get; private set; }

    public decimal TotalCredits { get; private set; }

    public decimal TotalDebits { get; private set; }

    public decimal Balance => TotalCredits - TotalDebits;

    public int EntryCount { get; private set; }

    public DateTime UpdatedAtUtc { get; private set; }

    public static DailyLedgerSummary Create(DateOnly businessDate, DateTime nowUtc)
    {
        return new DailyLedgerSummary
        {
            Id = Guid.NewGuid(),
            BusinessDate = businessDate,
            TotalCredits = 0,
            TotalDebits = 0,
            EntryCount = 0,
            UpdatedAtUtc = nowUtc
        };
    }

    public void ApplyEntry(LedgerEntryType entryType, decimal amount, DateTime nowUtc)
    {
        if (amount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(amount), "Amount must be greater than zero.");
        }

        switch (entryType)
        {
            case LedgerEntryType.Credit:
                TotalCredits += amount;
                break;
            case LedgerEntryType.Debit:
                TotalDebits += amount;
                break;
            default:
                throw new ArgumentException("Entry type is required.", nameof(entryType));
        }

        EntryCount += 1;
        UpdatedAtUtc = nowUtc;
    }
}
