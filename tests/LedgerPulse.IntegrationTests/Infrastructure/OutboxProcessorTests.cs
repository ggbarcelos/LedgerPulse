using LedgerPulse.Domain.Ledger.Entities;
using LedgerPulse.Infrastructure.Messaging;
using LedgerPulse.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LedgerPulse.IntegrationTests.Infrastructure;

public sealed class OutboxProcessorTests
{
    [Fact]
    public async Task ProcessPendingMessagesAsync_ShouldCreateDailySummary()
    {
        var options = new DbContextOptionsBuilder<LedgerPulseDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var dbContext = new LedgerPulseDbContext(options);
        var ledgerEntry = LedgerEntry.Create(new DateOnly(2026, 3, 12), "Invoice payment", 150m, "BRL");

        dbContext.LedgerEntries.Add(ledgerEntry);
        await dbContext.SaveChangesAsync();

        var processor = new OutboxProcessor(dbContext);
        var result = await processor.ProcessPendingMessagesAsync(CancellationToken.None);

        var summary = await dbContext.DailyLedgerSummaries.SingleAsync();
        var outboxMessage = await dbContext.OutboxMessages.SingleAsync();

        Assert.Equal(1, result.ProcessedMessages);
        Assert.Equal(0, result.IgnoredMessages);
        Assert.Equal(150m, summary.TotalAmount);
        Assert.Equal(1, summary.EntryCount);
        Assert.NotNull(outboxMessage.ProcessedOnUtc);
    }
}
