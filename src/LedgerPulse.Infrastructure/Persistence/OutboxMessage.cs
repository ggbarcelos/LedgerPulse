namespace LedgerPulse.Infrastructure.Persistence;

public sealed class OutboxMessage
{
    private OutboxMessage()
    {
    }

    public Guid Id { get; private set; }

    public string Type { get; private set; } = string.Empty;

    public string Content { get; private set; } = string.Empty;

    public DateTime OccurredOnUtc { get; private set; }

    public DateTime? ProcessedOnUtc { get; private set; }

    public string? Error { get; private set; }

    public static OutboxMessage Create(Guid id, string type, string content, DateTime occurredOnUtc)
    {
        return new OutboxMessage
        {
            Id = id,
            Type = type,
            Content = content,
            OccurredOnUtc = occurredOnUtc
        };
    }

    public void MarkProcessed(DateTime processedOnUtc, string? error = null)
    {
        ProcessedOnUtc = processedOnUtc;
        Error = error;
    }
}
