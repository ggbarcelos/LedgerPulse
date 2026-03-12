using LedgerPulse.Application.Abstractions.Messaging;
using Microsoft.Extensions.Options;

namespace LedgerPulse.ConsolidationWorker;

public sealed class ConsolidationWorkerService(
    IServiceScopeFactory serviceScopeFactory,
    IOptionsMonitor<ConsolidationWorkerOptions> optionsMonitor,
    ILogger<ConsolidationWorkerService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            using var scope = serviceScopeFactory.CreateScope();
            var outboxProcessor = scope.ServiceProvider.GetRequiredService<IOutboxProcessor>();
            var result = await outboxProcessor.ProcessPendingMessagesAsync(stoppingToken);

            if (result.ProcessedMessages > 0 || result.IgnoredMessages > 0)
            {
                logger.LogInformation(
                    "Processed {ProcessedMessages} outbox messages and ignored {IgnoredMessages} unsupported messages.",
                    result.ProcessedMessages,
                    result.IgnoredMessages);
            }

            var delay = TimeSpan.FromSeconds(Math.Max(1, optionsMonitor.CurrentValue.PollingIntervalSeconds));
            await Task.Delay(delay, stoppingToken);
        }
    }
}
