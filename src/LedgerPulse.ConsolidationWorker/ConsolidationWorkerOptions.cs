namespace LedgerPulse.ConsolidationWorker;

public sealed class ConsolidationWorkerOptions
{
    public const string SectionName = "ConsolidationWorker";

    public int PollingIntervalSeconds { get; set; } = 10;
}
