using LedgerPulse.Application.DailyConsolidation.Dtos;

namespace LedgerPulse.Application.DailyConsolidation.Services;

public interface IDailyConsolidationQueryService
{
    Task<IReadOnlyCollection<DailyLedgerSummaryResponse>> ListAsync(CancellationToken cancellationToken);
}
