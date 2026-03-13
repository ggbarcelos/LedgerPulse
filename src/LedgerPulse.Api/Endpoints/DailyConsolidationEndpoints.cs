using LedgerPulse.Application.Abstractions.Messaging;
using LedgerPulse.Application.DailyConsolidation.Services;
using LedgerPulse.Api.Security;

namespace LedgerPulse.Api.Endpoints;

public static class DailyConsolidationEndpoints
{
    public static IEndpointRouteBuilder MapDailyConsolidationEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/daily-consolidation").WithTags("DailyConsolidation");

        group.MapGet("/summaries", async (IDailyConsolidationQueryService queryService, CancellationToken cancellationToken) =>
        {
            var summaries = await queryService.ListAsync(cancellationToken);
            return Results.Ok(summaries);
        });

        var internalGroup = endpoints.MapGroup("/internal/daily-consolidation").WithTags("DailyConsolidation.Internal");

        internalGroup.MapPost("/process", async (IOutboxProcessor outboxProcessor, CancellationToken cancellationToken) =>
            {
                var result = await outboxProcessor.ProcessPendingMessagesAsync(cancellationToken);
                return Results.Ok(result);
            })
            .RequireRateLimiting("ConsolidationProcess")
            .RequireApiKey("ApiSecurity:ConsolidationProcessApiKey", "ConsolidationProcess")
            .ExcludeFromDescription();

        return endpoints;
    }
}
