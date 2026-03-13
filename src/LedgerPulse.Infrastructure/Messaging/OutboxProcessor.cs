using System.Data;
using System.Text.Json;
using LedgerPulse.Application.Abstractions.Messaging;
using LedgerPulse.Application.DailyConsolidation.Dtos;
using LedgerPulse.Application.IntegrationEvents;
using LedgerPulse.Domain.DailyConsolidation.Entities;
using LedgerPulse.Domain.Ledger;
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

                    var effectiveEntryType = integrationEvent.EntryType switch
                    {
                        LedgerEntryType.Credit => LedgerEntryType.Credit,
                        LedgerEntryType.Debit => LedgerEntryType.Debit,
                        _ when integrationEvent.Amount < 0 => LedgerEntryType.Debit,
                        _ => LedgerEntryType.Credit
                    };

                    summary.ApplyEntry(effectiveEntryType, Math.Abs(integrationEvent.Amount), DateTime.UtcNow);
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
            var connection = dbContext.Database.GetDbConnection();
            var shouldCloseConnection = connection.State != ConnectionState.Open;
            if (shouldCloseConnection)
            {
                await dbContext.Database.OpenConnectionAsync(cancellationToken);
            }

            await using var acquireCommand = connection.CreateCommand();
            acquireCommand.CommandText = "SELECT pg_try_advisory_lock(@lockKey)";

            var lockParameter = acquireCommand.CreateParameter();
            lockParameter.ParameterName = "@lockKey";
            lockParameter.Value = ProcessingLockKey;
            acquireCommand.Parameters.Add(lockParameter);

            var acquireResult = await acquireCommand.ExecuteScalarAsync(cancellationToken);
            var acquired = acquireResult is true;

            if (!acquired)
            {
                if (shouldCloseConnection)
                {
                    await dbContext.Database.CloseConnectionAsync();
                }

                return null;
            }

            return new AsyncDisposableAction(async () =>
            {
                await using var releaseCommand = connection.CreateCommand();
                releaseCommand.CommandText = "SELECT pg_advisory_unlock(@lockKey)";

                var releaseParameter = releaseCommand.CreateParameter();
                releaseParameter.ParameterName = "@lockKey";
                releaseParameter.Value = ProcessingLockKey;
                releaseCommand.Parameters.Add(releaseParameter);

                await releaseCommand.ExecuteScalarAsync();

                if (shouldCloseConnection)
                {
                    await dbContext.Database.CloseConnectionAsync();
                }
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
