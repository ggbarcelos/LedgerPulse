using LedgerPulse.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace LedgerPulse.Infrastructure.Extensions;

public static class HostExtensions
{
    public static async Task InitializeDatabaseAsync(this IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<LedgerPulseDbContext>();
        await dbContext.Database.EnsureCreatedAsync();
    }
}
