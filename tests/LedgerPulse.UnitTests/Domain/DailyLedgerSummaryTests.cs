using LedgerPulse.Domain.DailyConsolidation.Entities;
using LedgerPulse.Domain.Ledger;

namespace LedgerPulse.UnitTests.Domain;

public sealed class DailyLedgerSummaryTests
{
    [Fact]
    public void ApplyEntry_ShouldAccumulateAmountAndCount()
    {
        var summary = DailyLedgerSummary.Create(new DateOnly(2026, 3, 12), DateTime.UtcNow);

        summary.ApplyEntry(LedgerEntryType.Credit, 100m, DateTime.UtcNow);
        summary.ApplyEntry(LedgerEntryType.Debit, 40m, DateTime.UtcNow);

        Assert.Equal(100m, summary.TotalCredits);
        Assert.Equal(40m, summary.TotalDebits);
        Assert.Equal(60m, summary.Balance);
        Assert.Equal(2, summary.EntryCount);
    }
}
