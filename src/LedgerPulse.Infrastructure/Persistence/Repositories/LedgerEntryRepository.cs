using LedgerPulse.Application.Abstractions.Persistence;
using LedgerPulse.Domain.Ledger.Entities;
using Microsoft.EntityFrameworkCore;

namespace LedgerPulse.Infrastructure.Persistence.Repositories;

public sealed class LedgerEntryRepository(LedgerPulseDbContext dbContext) : ILedgerEntryRepository
{
    public Task AddAsync(LedgerEntry ledgerEntry, CancellationToken cancellationToken)
    {
        return dbContext.LedgerEntries.AddAsync(ledgerEntry, cancellationToken).AsTask();
    }

    public async Task<IReadOnlyCollection<LedgerEntry>> ListAsync(CancellationToken cancellationToken)
    {
        return await dbContext.LedgerEntries
            .OrderByDescending(entry => entry.BusinessDate)
            .ThenByDescending(entry => entry.CreatedAtUtc)
            .ToListAsync(cancellationToken);
    }
}
