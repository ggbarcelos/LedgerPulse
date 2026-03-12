using LedgerPulse.Application.DailyConsolidation.Services;
using LedgerPulse.Application.Ledger.Services;
using Microsoft.Extensions.DependencyInjection;

namespace LedgerPulse.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<ILedgerEntryService, LedgerEntryService>();
        services.AddScoped<IDailyConsolidationQueryService, DailyConsolidationQueryService>();
        return services;
    }
}
