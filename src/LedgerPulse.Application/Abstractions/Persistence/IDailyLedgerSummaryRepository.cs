using LedgerPulse.Domain.DailyConsolidation.Entities;

namespace LedgerPulse.Application.Abstractions.Persistence;

public interface IDailyLedgerSummaryRepository
{
    Task<IReadOnlyCollection<DailyLedgerSummary>> ListAsync(CancellationToken cancellationToken);
}
