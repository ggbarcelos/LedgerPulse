namespace LedgerPulse.Application.DailyConsolidation.Dtos;

public sealed record OutboxProcessingResult(int ProcessedMessages, int IgnoredMessages);
