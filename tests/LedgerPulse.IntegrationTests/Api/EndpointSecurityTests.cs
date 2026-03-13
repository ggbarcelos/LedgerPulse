using System.Net;
using System.Net.Http.Json;
using LedgerPulse.Api.Endpoints;
using LedgerPulse.Api.Security;
using LedgerPulse.Application.Abstractions.Messaging;
using LedgerPulse.Application.DailyConsolidation.Dtos;
using LedgerPulse.Application.DailyConsolidation.Services;
using LedgerPulse.Application.Ledger.Dtos;
using LedgerPulse.Application.Ledger.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace LedgerPulse.IntegrationTests.Api;

public sealed class EndpointSecurityTests
{
    [Fact]
    public async Task PostLedgerEntries_ShouldReturnUnauthorized_WhenApiKeyIsMissing()
    {
        await using var startedApp = await StartTestAppAsync();

        var response = await startedApp.Client.PostAsJsonAsync(
            "/api/ledger/entries",
            new RegisterLedgerEntryRequest("Payment", 10m, "BRL", new DateOnly(2026, 3, 12)));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task PostLedgerEntries_ShouldReturnUnauthorized_WhenApiKeyIsInvalid()
    {
        await using var startedApp = await StartTestAppAsync();
        startedApp.Client.DefaultRequestHeaders.Add("X-Api-Key", "invalid-key");

        var response = await startedApp.Client.PostAsJsonAsync(
            "/api/ledger/entries",
            new RegisterLedgerEntryRequest("Payment", 10m, "BRL", new DateOnly(2026, 3, 12)));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task PostLedgerEntries_ShouldReturnCreated_WhenApiKeyIsValid()
    {
        await using var startedApp = await StartTestAppAsync();
        startedApp.Client.DefaultRequestHeaders.Add("X-Api-Key", "ledger-write-key");

        var response = await startedApp.Client.PostAsJsonAsync(
            "/api/ledger/entries",
            new RegisterLedgerEntryRequest("Payment", 10m, "BRL", new DateOnly(2026, 3, 12)));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task PostPublicConsolidationProcess_ShouldReturnNotFound()
    {
        await using var startedApp = await StartTestAppAsync();

        var response = await startedApp.Client.PostAsync("/api/daily-consolidation/process", null);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task PostInternalConsolidationProcess_ShouldReturnUnauthorized_WhenUsingLedgerWriteApiKey()
    {
        await using var startedApp = await StartTestAppAsync();
        startedApp.Client.DefaultRequestHeaders.Add("X-Api-Key", "ledger-write-key");

        var response = await startedApp.Client.PostAsync("/internal/daily-consolidation/process", null);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task PostInternalConsolidationProcess_ShouldReturnOk_WhenApiKeyIsValid()
    {
        await using var startedApp = await StartTestAppAsync();
        startedApp.Client.DefaultRequestHeaders.Add("X-Api-Key", "consolidation-process-key");

        var response = await startedApp.Client.PostAsync("/internal/daily-consolidation/process", null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    private static async Task<StartedApp> StartTestAppAsync()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ApiSecurity:LedgerWriteApiKey"] = "ledger-write-key",
            ["ApiSecurity:ConsolidationProcessApiKey"] = "consolidation-process-key"
        });

        builder.Services.AddScoped<ApiKeyEndpointFilter>();
        builder.Services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.AddFixedWindowLimiter("LedgerWrite", limiterOptions =>
            {
                limiterOptions.PermitLimit = 100;
                limiterOptions.Window = TimeSpan.FromSeconds(1);
                limiterOptions.QueueLimit = 0;
                limiterOptions.AutoReplenishment = true;
            });
            options.AddFixedWindowLimiter("ConsolidationProcess", limiterOptions =>
            {
                limiterOptions.PermitLimit = 100;
                limiterOptions.Window = TimeSpan.FromSeconds(1);
                limiterOptions.QueueLimit = 0;
                limiterOptions.AutoReplenishment = true;
            });
        });

        builder.Services.AddSingleton<ILedgerEntryService, StubLedgerEntryService>();
        builder.Services.AddSingleton<IDailyConsolidationQueryService, StubDailyConsolidationQueryService>();
        builder.Services.AddSingleton<IOutboxProcessor, StubOutboxProcessor>();

        var app = builder.Build();
        app.UseRateLimiter();
        app.MapLedgerEndpoints();
        app.MapDailyConsolidationEndpoints();

        await app.StartAsync();
        return new StartedApp(app, app.GetTestClient());
    }

    private sealed class StubLedgerEntryService : ILedgerEntryService
    {
        public Task<LedgerEntryResponse> CreateAsync(RegisterLedgerEntryRequest request, CancellationToken cancellationToken)
        {
            return Task.FromResult(new LedgerEntryResponse(
                Guid.NewGuid(),
                request.BusinessDate ?? DateOnly.FromDateTime(DateTime.UtcNow),
                request.Description,
                request.Amount,
                request.Currency,
                DateTime.UtcNow));
        }

        public Task<IReadOnlyCollection<LedgerEntryResponse>> ListAsync(CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyCollection<LedgerEntryResponse>>([]);
    }

    private sealed class StubDailyConsolidationQueryService : IDailyConsolidationQueryService
    {
        public Task<IReadOnlyCollection<DailyLedgerSummaryResponse>> ListAsync(CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyCollection<DailyLedgerSummaryResponse>>([]);
    }

    private sealed class StubOutboxProcessor : IOutboxProcessor
    {
        public Task<OutboxProcessingResult> ProcessPendingMessagesAsync(CancellationToken cancellationToken)
            => Task.FromResult(new OutboxProcessingResult(0, 0));
    }

    private sealed class StartedApp(WebApplication app, HttpClient client) : IAsyncDisposable
    {
        public HttpClient Client { get; } = client;

        public async ValueTask DisposeAsync()
        {
            Client.Dispose();
            await app.StopAsync();
            await app.DisposeAsync();
        }
    }
}
