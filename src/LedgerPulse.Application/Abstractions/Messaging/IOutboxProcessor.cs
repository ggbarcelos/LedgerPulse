using LedgerPulse.Application.DailyConsolidation.Dtos;

namespace LedgerPulse.Application.Abstractions.Messaging;

public interface IOutboxProcessor
{
    Task<OutboxProcessingResult> ProcessPendingMessagesAsync(CancellationToken cancellationToken);
}
