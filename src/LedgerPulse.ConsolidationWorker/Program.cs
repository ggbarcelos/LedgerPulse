using LedgerPulse.Application;
using LedgerPulse.Infrastructure;
using LedgerPulse.Infrastructure.Extensions;

namespace LedgerPulse.ConsolidationWorker;

internal static class Program
{
    private static async Task Main(string[] args)
    {
        var builder = Host.CreateApplicationBuilder(args);
        builder.Services.AddApplication();
        builder.Services.AddInfrastructure(builder.Configuration);
        builder.Services.Configure<ConsolidationWorkerOptions>(builder.Configuration.GetSection(ConsolidationWorkerOptions.SectionName));
        builder.Services.AddHostedService<ConsolidationWorkerService>();

        var host = builder.Build();
        await host.Services.InitializeDatabaseAsync();
        await host.RunAsync();
    }
}
