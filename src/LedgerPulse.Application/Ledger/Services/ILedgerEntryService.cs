using LedgerPulse.Application.Ledger.Dtos;

namespace LedgerPulse.Application.Ledger.Services;

public interface ILedgerEntryService
{
    Task<LedgerEntryResponse> CreateAsync(RegisterLedgerEntryRequest request, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<LedgerEntryResponse>> ListAsync(CancellationToken cancellationToken);
}
