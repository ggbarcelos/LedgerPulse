using LedgerPulse.Application.Ledger.Dtos;
using LedgerPulse.Application.Ledger.Services;
using LedgerPulse.Api.Security;

namespace LedgerPulse.Api.Endpoints;

public static class LedgerEndpoints
{
    public static IEndpointRouteBuilder MapLedgerEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/ledger").WithTags("Ledger");

        group.MapGet("/entries", async (ILedgerEntryService ledgerEntryService, CancellationToken cancellationToken) =>
        {
            var entries = await ledgerEntryService.ListAsync(cancellationToken);
            return Results.Ok(entries);
        });

        group.MapPost("/entries", async (RegisterLedgerEntryRequest request, ILedgerEntryService ledgerEntryService, CancellationToken cancellationToken) =>
            {
                var createdEntry = await ledgerEntryService.CreateAsync(request, cancellationToken);
                return Results.Created($"/api/ledger/entries/{createdEntry.Id}", createdEntry);
            })
            .RequireRateLimiting("LedgerWrite")
            .RequireApiKey("ApiSecurity:LedgerWriteApiKey", "LedgerWrite");

        return endpoints;
    }
}
