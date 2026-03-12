using LedgerPulse.Application.Abstractions.Persistence;
using LedgerPulse.Application.Ledger.Dtos;
using LedgerPulse.Domain.Ledger.Entities;

namespace LedgerPulse.Application.Ledger.Services;

public sealed class LedgerEntryService(ILedgerEntryRepository ledgerEntryRepository, IUnitOfWork unitOfWork) : ILedgerEntryService
{
    public async Task<LedgerEntryResponse> CreateAsync(RegisterLedgerEntryRequest request, CancellationToken cancellationToken)
    {
        var businessDate = request.BusinessDate ?? DateOnly.FromDateTime(DateTime.UtcNow);
        var ledgerEntry = LedgerEntry.Create(businessDate, request.Description, request.Amount, request.Currency);

        await ledgerEntryRepository.AddAsync(ledgerEntry, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new LedgerEntryResponse(
            ledgerEntry.Id,
            ledgerEntry.BusinessDate,
            ledgerEntry.Description,
            ledgerEntry.Amount,
            ledgerEntry.Currency,
            ledgerEntry.CreatedAtUtc);
    }

    public async Task<IReadOnlyCollection<LedgerEntryResponse>> ListAsync(CancellationToken cancellationToken)
    {
        var ledgerEntries = await ledgerEntryRepository.ListAsync(cancellationToken);
        return ledgerEntries
            .Select(entry => new LedgerEntryResponse(entry.Id, entry.BusinessDate, entry.Description, entry.Amount, entry.Currency, entry.CreatedAtUtc))
            .ToArray();
    }
}
