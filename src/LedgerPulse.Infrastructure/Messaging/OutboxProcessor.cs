using System.Text.Json;
using LedgerPulse.Application.Abstractions.Messaging;
using LedgerPulse.Application.DailyConsolidation.Dtos;
using LedgerPulse.Application.IntegrationEvents;
using LedgerPulse.Domain.DailyConsolidation.Entities;
using LedgerPulse.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LedgerPulse.Infrastructure.Messaging;

public sealed class OutboxProcessor(LedgerPulseDbContext dbContext) : IOutboxProcessor
{
    private static readonly JsonSerializerOptions JsonSerializerOptions = new(JsonSerializerDefaults.Web);
    private static readonly SemaphoreSlim InMemoryExecutionLock = new(1, 1);
    private const long ProcessingLockKey = 4301202601;
    private const int MaxErrorLength = 1000;

    public async Task<OutboxProcessingResult> ProcessPendingMessagesAsync(CancellationToken cancellationToken)
    {
        await using var processingLock = await TryAcquireProcessingLockAsync(cancellationToken);
        if (processingLock is null)
        {
            return new OutboxProcessingResult(0, 0);
        }

        var pendingMessages = await dbContext.OutboxMessages
            .Where(message => message.ProcessedOnUtc == null)
            .OrderBy(message => message.OccurredOnUtc)
            .Take(50)
            .ToListAsync(cancellationToken);

        var processedMessages = 0;
        var ignoredMessages = 0;

        foreach (var message in pendingMessages)
        {
            try
            {
                if (message.Type == nameof(LedgerEntryRegisteredIntegrationEvent))
                {
                    var integrationEvent = JsonSerializer.Deserialize<LedgerEntryRegisteredIntegrationEvent>(message.Content, JsonSerializerOptions)
                        ?? throw new InvalidOperationException($"Outbox message '{message.Id}' could not be deserialized.");

                    var summary = await dbContext.DailyLedgerSummaries
                        .SingleOrDefaultAsync(item => item.BusinessDate == integrationEvent.BusinessDate, cancellationToken);

                    if (summary is null)
                    {
                        summary = DailyLedgerSummary.Create(integrationEvent.BusinessDate, DateTime.UtcNow);
                        dbContext.DailyLedgerSummaries.Add(summary);
                    }

                    summary.ApplyEntry(integrationEvent.Amount, DateTime.UtcNow);
                    message.MarkProcessed(DateTime.UtcNow);
                    processedMessages += 1;
                    continue;
                }

                message.MarkProcessed(DateTime.UtcNow, $"Unsupported message type: {message.Type}");
                ignoredMessages += 1;
            }
            catch (Exception ex)
            {
                message.MarkProcessed(DateTime.UtcNow, TruncateError($"Processing failed: {ex.Message}"));
                ignoredMessages += 1;
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return new OutboxProcessingResult(processedMessages, ignoredMessages);
    }

    private async Task<IAsyncDisposable?> TryAcquireProcessingLockAsync(CancellationToken cancellationToken)
    {
        if (dbContext.Database.IsNpgsql())
        {
            var acquired = await dbContext.Database
                .SqlQueryRaw<bool>("SELECT pg_try_advisory_lock({0})", ProcessingLockKey)
                .SingleAsync(cancellationToken);

            if (!acquired)
            {
                return null;
            }

            return new AsyncDisposableAction(async () =>
            {
                await dbContext.Database.ExecuteSqlRawAsync("SELECT pg_advisory_unlock({0})", [ProcessingLockKey]);
            });
        }

        await InMemoryExecutionLock.WaitAsync(cancellationToken);
        return new AsyncDisposableAction(() =>
        {
            InMemoryExecutionLock.Release();
            return Task.CompletedTask;
        });
    }

    private static string TruncateError(string error)
        => error.Length <= MaxErrorLength ? error : error[..MaxErrorLength];

    private sealed class AsyncDisposableAction(Func<Task> disposeAction) : IAsyncDisposable
    {
        public async ValueTask DisposeAsync()
        {
            await disposeAction();
        }
    }
}
