using System.Net.Http.Json;
using LedgerPulse.Frontend.Models;

namespace LedgerPulse.Frontend.Services;

public sealed class LedgerPulseApiClient(HttpClient httpClient, string? apiKey = null)
{
    private const string ApiKeyHeaderName = "X-Api-Key";
    private readonly string? _apiKey = string.IsNullOrWhiteSpace(apiKey) ? null : apiKey;

    public async Task<IReadOnlyCollection<LedgerEntryViewModel>> GetLedgerEntriesAsync(CancellationToken cancellationToken = default)
    {
        var response = await httpClient.GetFromJsonAsync<List<LedgerEntryViewModel>>("api/ledger/entries", cancellationToken);
        return response ?? [];
    }

    public async Task<LedgerEntryViewModel> CreateLedgerEntryAsync(RegisterLedgerEntryRequestModel request, CancellationToken cancellationToken = default)
    {
        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, "api/ledger/entries")
        {
            Content = JsonContent.Create(request)
        };

        if (_apiKey is not null)
        {
            httpRequest.Headers.TryAddWithoutValidation(ApiKeyHeaderName, _apiKey);
        }

        var response = await httpClient.SendAsync(httpRequest, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<LedgerEntryViewModel>(cancellationToken)
            ?? throw new InvalidOperationException("A resposta da API nao retornou o lancamento criado.");
    }

    public async Task<IReadOnlyCollection<DailyLedgerSummaryViewModel>> GetDailySummariesAsync(CancellationToken cancellationToken = default)
    {
        var response = await httpClient.GetFromJsonAsync<List<DailyLedgerSummaryViewModel>>("api/daily-consolidation/summaries", cancellationToken);
        return response ?? [];
    }
}
