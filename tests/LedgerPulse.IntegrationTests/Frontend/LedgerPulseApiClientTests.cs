using System.Net;
using System.Net.Http.Json;
using LedgerPulse.Domain.Ledger;
using LedgerPulse.Frontend.Models;
using LedgerPulse.Frontend.Services;

namespace LedgerPulse.IntegrationTests.Frontend;

public sealed class LedgerPulseApiClientTests
{
    [Fact]
    public async Task CreateLedgerEntryAsync_ShouldSendConfiguredApiKeyHeader()
    {
        var handler = new RecordingHttpMessageHandler();
        using var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("http://localhost:5021/", UriKind.Absolute)
        };

        var apiClient = new LedgerPulseApiClient(httpClient, "ledgerpulse-dev-key");

        _ = await apiClient.CreateLedgerEntryAsync(new RegisterLedgerEntryRequestModel("Header test", 10m, LedgerEntryType.Credit, new DateOnly(2026, 3, 13)));

        Assert.NotNull(handler.LastRequest);
        Assert.True(handler.LastRequest!.Headers.TryGetValues("X-Api-Key", out var values));
        Assert.Equal("ledgerpulse-dev-key", Assert.Single(values));
    }

    [Fact]
    public async Task CreateLedgerEntryAsync_ShouldNotSendApiKeyHeaderWhenNotConfigured()
    {
        var handler = new RecordingHttpMessageHandler();
        using var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("http://localhost:5021/", UriKind.Absolute)
        };

        var apiClient = new LedgerPulseApiClient(httpClient);

        _ = await apiClient.CreateLedgerEntryAsync(new RegisterLedgerEntryRequestModel("No header test", 10m, LedgerEntryType.Debit, new DateOnly(2026, 3, 13)));

        Assert.NotNull(handler.LastRequest);
        Assert.False(handler.LastRequest!.Headers.Contains("X-Api-Key"));
    }

    private sealed class RecordingHttpMessageHandler : HttpMessageHandler
    {
        public HttpRequestMessage? LastRequest { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastRequest = CloneRequest(request);

            var response = new HttpResponseMessage(HttpStatusCode.Created)
            {
                Content = JsonContent.Create(new LedgerEntryViewModel(
                    Guid.NewGuid(),
                    new DateOnly(2026, 3, 13),
                    "Recorded",
                    10m,
                    LedgerEntryType.Credit,
                    DateTime.UtcNow))
            };

            return Task.FromResult(response);
        }

        private static HttpRequestMessage CloneRequest(HttpRequestMessage request)
        {
            var clone = new HttpRequestMessage(request.Method, request.RequestUri);

            foreach (var header in request.Headers)
            {
                clone.Headers.TryAddWithoutValidation(header.Key, header.Value);
            }

            return clone;
        }
    }
}
