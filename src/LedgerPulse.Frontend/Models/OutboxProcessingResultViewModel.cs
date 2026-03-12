namespace LedgerPulse.Frontend.Models;

public sealed record OutboxProcessingResultViewModel(int ProcessedMessages, int IgnoredMessages);
