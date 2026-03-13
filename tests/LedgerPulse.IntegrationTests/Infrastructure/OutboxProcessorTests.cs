using LedgerPulse.Domain.Ledger.Entities;
using LedgerPulse.Domain.Ledger;
using LedgerPulse.Application.IntegrationEvents;
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
        var ledgerEntry = LedgerEntry.Create(new DateOnly(2026, 3, 12), "Invoice payment", 150m, LedgerEntryType.Credit);

        dbContext.LedgerEntries.Add(ledgerEntry);
        await dbContext.SaveChangesAsync();

        var processor = new OutboxProcessor(dbContext);
        var result = await processor.ProcessPendingMessagesAsync(CancellationToken.None);

        var summary = await dbContext.DailyLedgerSummaries.SingleAsync();
        var outboxMessage = await dbContext.OutboxMessages.SingleAsync();

        Assert.Equal(1, result.ProcessedMessages);
        Assert.Equal(0, result.IgnoredMessages);
        Assert.Equal(150m, summary.TotalCredits);
        Assert.Equal(0m, summary.TotalDebits);
        Assert.Equal(150m, summary.Balance);
        Assert.Equal(1, summary.EntryCount);
        Assert.NotNull(outboxMessage.ProcessedOnUtc);
    }

    [Fact]
    public async Task ProcessPendingMessagesAsync_ShouldMarkInvalidPayloadAsProcessedWithError()
    {
        var options = new DbContextOptionsBuilder<LedgerPulseDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var dbContext = new LedgerPulseDbContext(options);
        dbContext.OutboxMessages.Add(OutboxMessage.Create(
            Guid.NewGuid(),
            nameof(LedgerEntryRegisteredIntegrationEvent),
            "{invalid-json",
            DateTime.UtcNow));
        await dbContext.SaveChangesAsync();

        var processor = new OutboxProcessor(dbContext);
        var result = await processor.ProcessPendingMessagesAsync(CancellationToken.None);

        var outboxMessage = await dbContext.OutboxMessages.SingleAsync();
        Assert.Equal(0, result.ProcessedMessages);
        Assert.Equal(1, result.IgnoredMessages);
        Assert.NotNull(outboxMessage.ProcessedOnUtc);
        Assert.False(string.IsNullOrWhiteSpace(outboxMessage.Error));
    }

    [Fact]
    public async Task ProcessPendingMessagesAsync_ShouldNotDuplicateConsolidationWhenCalledConcurrently()
    {
        var databaseName = Guid.NewGuid().ToString();
        var options = new DbContextOptionsBuilder<LedgerPulseDbContext>()
            .UseInMemoryDatabase(databaseName)
            .Options;

        await using (var seedContext = new LedgerPulseDbContext(options))
        {
            seedContext.LedgerEntries.Add(LedgerEntry.Create(new DateOnly(2026, 3, 12), "Concurrent payment", 200m, LedgerEntryType.Debit));
            await seedContext.SaveChangesAsync();
        }

        await using var dbContextA = new LedgerPulseDbContext(options);
        await using var dbContextB = new LedgerPulseDbContext(options);

        var processorA = new OutboxProcessor(dbContextA);
        var processorB = new OutboxProcessor(dbContextB);

        await Task.WhenAll(
            processorA.ProcessPendingMessagesAsync(CancellationToken.None),
            processorB.ProcessPendingMessagesAsync(CancellationToken.None));

        await using var assertContext = new LedgerPulseDbContext(options);
        var summary = await assertContext.DailyLedgerSummaries.SingleAsync();

        Assert.Equal(0m, summary.TotalCredits);
        Assert.Equal(200m, summary.TotalDebits);
        Assert.Equal(-200m, summary.Balance);
        Assert.Equal(1, summary.EntryCount);
    }
}
