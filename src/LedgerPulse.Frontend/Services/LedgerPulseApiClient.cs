using System.Net.Http.Json;
using LedgerPulse.Frontend.Models;

namespace LedgerPulse.Frontend.Services;

public sealed class LedgerPulseApiClient(HttpClient httpClient)
{
    public async Task<IReadOnlyCollection<LedgerEntryViewModel>> GetLedgerEntriesAsync(CancellationToken cancellationToken = default)
    {
        var response = await httpClient.GetFromJsonAsync<List<LedgerEntryViewModel>>("api/ledger/entries", cancellationToken);
        return response ?? [];
    }

    public async Task<LedgerEntryViewModel> CreateLedgerEntryAsync(RegisterLedgerEntryRequestModel request, CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PostAsJsonAsync("api/ledger/entries", request, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<LedgerEntryViewModel>(cancellationToken)
            ?? throw new InvalidOperationException("The API response did not contain the created ledger entry.");
    }

    public async Task<IReadOnlyCollection<DailyLedgerSummaryViewModel>> GetDailySummariesAsync(CancellationToken cancellationToken = default)
    {
        var response = await httpClient.GetFromJsonAsync<List<DailyLedgerSummaryViewModel>>("api/daily-consolidation/summaries", cancellationToken);
        return response ?? [];
    }

    public async Task<OutboxProcessingResultViewModel> ProcessOutboxAsync(CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PostAsync("api/daily-consolidation/process", content: null, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<OutboxProcessingResultViewModel>(cancellationToken)
            ?? throw new InvalidOperationException("The API response did not contain the processing result.");
    }
}
