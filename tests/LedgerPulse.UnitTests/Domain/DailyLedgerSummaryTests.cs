using LedgerPulse.Domain.DailyConsolidation.Entities;

namespace LedgerPulse.UnitTests.Domain;

public sealed class DailyLedgerSummaryTests
{
    [Fact]
    public void ApplyEntry_ShouldAccumulateAmountAndCount()
    {
        var summary = DailyLedgerSummary.Create(new DateOnly(2026, 3, 12), DateTime.UtcNow);

        summary.ApplyEntry(100m, DateTime.UtcNow);
        summary.ApplyEntry(-40m, DateTime.UtcNow);

        Assert.Equal(60m, summary.TotalAmount);
        Assert.Equal(2, summary.EntryCount);
    }
}
