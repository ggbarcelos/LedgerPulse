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

    public async Task<OutboxProcessingResult> ProcessPendingMessagesAsync(CancellationToken cancellationToken)
    {
        var pendingMessages = await dbContext.OutboxMessages
            .Where(message => message.ProcessedOnUtc == null)
            .OrderBy(message => message.OccurredOnUtc)
            .Take(50)
            .ToListAsync(cancellationToken);

        var processedMessages = 0;
        var ignoredMessages = 0;

        foreach (var message in pendingMessages)
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

        await dbContext.SaveChangesAsync(cancellationToken);
        return new OutboxProcessingResult(processedMessages, ignoredMessages);
    }
}
