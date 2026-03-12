using LedgerPulse.Application.Abstractions.Persistence;
using LedgerPulse.Application.DailyConsolidation.Dtos;

namespace LedgerPulse.Application.DailyConsolidation.Services;

public sealed class DailyConsolidationQueryService(IDailyLedgerSummaryRepository summaryRepository) : IDailyConsolidationQueryService
{
    public async Task<IReadOnlyCollection<DailyLedgerSummaryResponse>> ListAsync(CancellationToken cancellationToken)
    {
        var summaries = await summaryRepository.ListAsync(cancellationToken);
        return summaries
            .Select(summary => new DailyLedgerSummaryResponse(summary.BusinessDate, summary.TotalAmount, summary.EntryCount, summary.UpdatedAtUtc))
            .ToArray();
    }
}
