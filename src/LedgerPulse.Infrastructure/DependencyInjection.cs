using LedgerPulse.Application.Abstractions.Messaging;
using LedgerPulse.Application.Abstractions.Persistence;
using LedgerPulse.Infrastructure.Messaging;
using LedgerPulse.Infrastructure.Persistence;
using LedgerPulse.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace LedgerPulse.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Postgres")
            ?? throw new InvalidOperationException("Connection string 'Postgres' was not configured.");

        services.AddDbContext<LedgerPulseDbContext>(options => options.UseNpgsql(connectionString));
        services.AddScoped<ILedgerEntryRepository, LedgerEntryRepository>();
        services.AddScoped<IDailyLedgerSummaryRepository, DailyLedgerSummaryRepository>();
        services.AddScoped<IUnitOfWork>(serviceProvider => serviceProvider.GetRequiredService<LedgerPulseDbContext>());
        services.AddScoped<IOutboxProcessor, OutboxProcessor>();
        return services;
    }
}
