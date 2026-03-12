using LedgerPulse.Domain.Ledger.Entities;

namespace LedgerPulse.Application.Abstractions.Persistence;

public interface ILedgerEntryRepository
{
    Task AddAsync(LedgerEntry ledgerEntry, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<LedgerEntry>> ListAsync(CancellationToken cancellationToken);
}
