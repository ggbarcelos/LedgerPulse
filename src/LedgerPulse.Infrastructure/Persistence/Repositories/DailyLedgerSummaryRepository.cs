using LedgerPulse.Application.Abstractions.Persistence;
using LedgerPulse.Domain.DailyConsolidation.Entities;
using Microsoft.EntityFrameworkCore;

namespace LedgerPulse.Infrastructure.Persistence.Repositories;

public sealed class DailyLedgerSummaryRepository(LedgerPulseDbContext dbContext) : IDailyLedgerSummaryRepository
{
    public async Task<IReadOnlyCollection<DailyLedgerSummary>> ListAsync(CancellationToken cancellationToken)
    {
        return await dbContext.DailyLedgerSummaries
            .OrderByDescending(summary => summary.BusinessDate)
            .ToListAsync(cancellationToken);
    }
}
